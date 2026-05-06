namespace Api.Services;

public class ConfigRefreshService : BackgroundService
{
    private readonly IMarketConfigService _config;

    public ConfigRefreshService(IMarketConfigService config)
    {
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _config.ReloadAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}