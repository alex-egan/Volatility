
using Api.Models.Database;
using Api.Services;

namespace Api.Workers;

public class BeverageWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            BeverageService service = scope.ServiceProvider.GetRequiredService<BeverageService>();

            try
            {
                await service.OnBeverageTimerExpired();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not perform the timer expired update: {exception}");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}