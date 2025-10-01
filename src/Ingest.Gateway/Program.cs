using Innovia.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<IngestDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddScoped<IngestService>();
builder.Services.AddScoped<IValidator<MeasurementBatch>, MeasurementBatchValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

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

app.MapPost("/ingest/http/{tenant}", async (string tenant, MeasurementBatch payload, IValidator<MeasurementBatch> validator, IngestService ingest) =>
{
    var result = await validator.ValidateAsync(payload);
    if (!result.IsValid) return Results.BadRequest(result.Errors);
    await ingest.ProcessAsync(tenant, payload);
    return Results.Accepted();
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
    public IngestService(IngestDbContext db) { _db = db; }
    public async Task ProcessAsync(string tenant, MeasurementBatch payload)
    {
        // TODO: validate api key/HMAC and resolve tenant/device ids
        foreach (var m in payload.Metrics)
        {
            _db.Measurements.Add(new MeasurementRow {
                Time = payload.Timestamp,
                TenantId = Guid.NewGuid(), // TODO: lookup
                DeviceId = Guid.NewGuid(), // TODO: lookup
                Type = m.Type,
                Value = m.Value
            });
        }
        await _db.SaveChangesAsync();
    }
}
