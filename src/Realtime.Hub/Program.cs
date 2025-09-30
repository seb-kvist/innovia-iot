var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
app.MapHub<TelemetryHub>("/hub/telemetry");
app.Run();

public class TelemetryHub : Microsoft.AspNetCore.SignalR.Hub
{
    public Task JoinTenant(string tenant) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenant}");
}
