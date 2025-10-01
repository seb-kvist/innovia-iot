var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
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

app.MapGet("/portal/{tenant}/devices/{deviceId}/series", (string tenant, Guid deviceId, string type, DateTimeOffset from, DateTimeOffset to) =>
    Results.Ok(new { deviceId, type, from, to, points = new [] { new { t = from, v = 1.0 }, new { t = to, v = 2.0 } } })
);

app.Run();
