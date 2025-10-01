using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PortalDbContext>(o =>
    o.UseNpgsql(
        builder.Configuration.GetConnectionString("Db")
        ?? "Host=localhost;Username=postgres;Password=password;Database=innovia_ingest"
    )
);
var app = builder.Build();
// Ensure database and tables exist (quick-start dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
    db.Database.EnsureCreated();
}
// Enable Swagger always (not only in Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Portal.Adapter v1");
    c.RoutePrefix = "swagger";
});
// Redirect root to Swagger UI for convenience
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/portal/{tenant}/rooms/{roomId}/latest", (string tenant, Guid roomId) => Results.Ok(new {
    roomId,
    metrics = new [] { new { type = "co2", value = 950.0, unit = "ppm" }, new { type = "temperature", value = 22.4, unit = "C" } }
}));

app.MapGet("/portal/{tenant}/devices/{deviceId}/series",
    async (string tenant, Guid deviceId, string type, DateTimeOffset from, DateTimeOffset to, PortalDbContext db) =>
{
    var points = await db.Measurements
        .Where(m => m.DeviceId == deviceId && m.Type == type && m.Time >= from && m.Time <= to)
        .OrderBy(m => m.Time)
        .Select(m => new { t = m.Time, v = m.Value })
        .ToListAsync();

    return Results.Ok(new { deviceId, type, from, to, points });
});

app.Run();

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options) { }
    public DbSet<MeasurementRow> Measurements => Set<MeasurementRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeasurementRow>().ToTable("Measurements");
        base.OnModelCreating(modelBuilder);
    }
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
