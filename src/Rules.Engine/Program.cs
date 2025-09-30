using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(lb => lb.AddConsole())
    .ConfigureServices((ctx, services) => {
        services.AddHostedService<RulesWorker>();
    })
    .RunConsoleAsync();

public class RulesWorker : BackgroundService
{
    private readonly ILogger<RulesWorker> _logger;
    public RulesWorker(ILogger<RulesWorker> logger) { _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rules engine started");
        while(!stoppingToken.IsCancellationRequested)
        {
            // TODO: fetch recent measurements, evaluate rules, create alerts
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
