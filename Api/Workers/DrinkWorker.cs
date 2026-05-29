
using Api.Services;

namespace Api.Workers;

public class DrinkWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            DrinkService service = scope.ServiceProvider.GetRequiredService<DrinkService>();

            try
            {
                await service.OnDrinkTimerExpired();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not perform the timer expired update: {exception}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}