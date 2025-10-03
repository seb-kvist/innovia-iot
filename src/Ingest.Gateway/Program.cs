using Innovia.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<IngestDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddScoped<IngestService>();
builder.Services.AddScoped<IValidator<MeasurementBatch>, MeasurementBatchValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Registry HTTP client + config
builder.Services.AddHttpClient<DeviceRegistryClient>();
builder.Services.AddSingleton<DeviceRegistryConfig>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("DeviceRegistry");
    return new DeviceRegistryConfig { BaseUrl = cfg?["BaseUrl"] ?? "http://localhost:5101" };
});
// Realtime publisher (SignalR client)
builder.Services.AddSingleton(new RealtimeConfig
{
    HubUrl = "http://localhost:5103/hub/telemetry"
});
builder.Services.AddSingleton<HubConnection>(sp =>
{
    var cfg = sp.GetRequiredService<RealtimeConfig>();
    return new HubConnectionBuilder()
        .WithUrl(cfg.HubUrl)
        .WithAutomaticReconnect()
        .Build();
});
builder.Services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();

var app = builder.Build();

// Start SignalR hub connection
using (var scope = app.Services.CreateScope())
{
    var hub = scope.ServiceProvider.GetRequiredService<HubConnection>();
    await hub.StartAsync();
}

// Ensure database and tables exist (quick-start dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IngestDbContext>();
    db.Database.EnsureCreated();
}

// Enable Swagger always (not only in Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ingest.Gateway v1");
    c.RoutePrefix = "swagger";
});
// Redirect root to Swagger UI for convenience
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/ingest/http/{tenant}", async (string tenant, MeasurementBatch payload, IValidator<MeasurementBatch> validator, IngestService ingest, ILogger<Program> log) =>
{
    var result = await validator.ValidateAsync(payload);
    if (!result.IsValid)
    {
        log.LogWarning("Validation failed for ingest payload (tenant: {Tenant}, serial: {Serial}): {Errors}", tenant, payload?.DeviceId, result.Errors);
        return Results.BadRequest(result.Errors);
    }

    await ingest.ProcessAsync(tenant, payload);
    log.LogInformation("Ingested {Count} metrics for serial {Serial} in tenant {Tenant} at {Time}", payload.Metrics.Count, payload.DeviceId, tenant, payload.Timestamp);
    return Results.Accepted();
});

app.MapGet("/ingest/debug/device/{deviceId:guid}", async (Guid deviceId, IngestDbContext db) =>
{
    var count = await db.Measurements.Where(m => m.DeviceId == deviceId).CountAsync();
    var latest = await db.Measurements.Where(m => m.DeviceId == deviceId).OrderByDescending(m => m.Time).Take(5).ToListAsync();
    return Results.Ok(new { deviceId, count, latest });
});

app.Run();

public class IngestDbContext : DbContext
{
    public IngestDbContext(DbContextOptions<IngestDbContext> o) : base(o) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeasurementRow>().ToTable("Measurements");
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<MeasurementRow> Measurements => Set<MeasurementRow>();
}
public class MeasurementRow
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string Type { get; set; } = "";
    public double Value { get; set; }
}
public class MeasurementBatchValidator : AbstractValidator<MeasurementBatch>
{
    public MeasurementBatchValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.ApiKey).NotEmpty();
        RuleFor(x => x.Metrics).NotEmpty();
    }
}
public class IngestService
{
    private readonly IngestDbContext _db;
    private readonly DeviceRegistryClient _registry;
    private readonly IRealtimePublisher _rt;
    public IngestService(IngestDbContext db, DeviceRegistryClient registry, IRealtimePublisher rt) { _db = db; _registry = registry; _rt = rt; }
    public async Task ProcessAsync(string tenant, MeasurementBatch payload)
    {
        // Resolve tenant and device using DeviceRegistry (tenant slug + device serial)
        var ids = await _registry.ResolveAsync(tenant, payload.DeviceId);

        foreach (var m in payload.Metrics)
        {
            _db.Measurements.Add(new MeasurementRow {
                Time = payload.Timestamp,
                TenantId = ids.TenantId,
                DeviceId = ids.DeviceId,
                Type = m.Type,
                Value = m.Value
            });
        }
        await _db.SaveChangesAsync();

        // Publish in realtime
        foreach (var m in payload.Metrics)
        {
            await _rt.PublishAsync(tenant, ids.DeviceId, m.Type, m.Value, payload.Timestamp);
        }
    }
}

public record DeviceRegistryConfig
{
    public string BaseUrl { get; set; } = "http://localhost:5101";
}

public class DeviceRegistryClient
{
    private readonly HttpClient _http;
    private readonly DeviceRegistryConfig _cfg;
    private readonly Dictionary<string, (Guid TenantId, Guid DeviceId)> _cache = new();

    public DeviceRegistryClient(HttpClient http, DeviceRegistryConfig cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public async Task<(Guid TenantId, Guid DeviceId)> ResolveAsync(string tenantSlug, string deviceSerial)
    {
        var cacheKey = $"{tenantSlug}:{deviceSerial}";
        if (_cache.TryGetValue(cacheKey, out var hit)) return hit;

        var tenant = await _http.GetFromJsonAsync<TenantDto>($"{_cfg.BaseUrl}/api/tenants/by-slug/{tenantSlug}")
                    ?? throw new InvalidOperationException($"Tenant slug '{tenantSlug}' not found");

        var device = await _http.GetFromJsonAsync<DeviceDto>($"{_cfg.BaseUrl}/api/tenants/{tenant.Id}/devices/by-serial/{deviceSerial}")
                    ?? throw new InvalidOperationException($"Device serial '{deviceSerial}' not found in tenant '{tenantSlug}'");

        var ids = (tenant.Id, device.Id);
        _cache[cacheKey] = ids;
        return ids;
    }

    private record TenantDto(Guid Id, string Name, string Slug);
    private record DeviceDto(Guid Id, Guid TenantId, Guid? RoomId, string Model, string Serial, string Status);
}

public class RealtimeConfig
{
    public string HubUrl { get; set; } = "http://localhost:5103/hub/telemetry";
}

public interface IRealtimePublisher
{
    Task PublishAsync(string tenantSlug, Guid deviceId, string type, double value, DateTimeOffset time);
}

public class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly HubConnection _conn;
    public SignalRRealtimePublisher(HubConnection conn) => _conn = conn;

    public async Task PublishAsync(string tenantSlug, Guid deviceId, string type, double value, DateTimeOffset time)
    {
        var payload = new
        {
            TenantSlug = tenantSlug,
            DeviceId = deviceId,
            Type = type,
            Value = value,
            Time = time
        };
        await _conn.InvokeAsync("PublishMeasurement", payload);
    }
}
