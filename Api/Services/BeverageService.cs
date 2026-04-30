using Api.Models.Database;
using Api.Models.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BeverageService(DBContext context) 
{
    private readonly DbSet<Beverage> _beverages = context.Beverages;
    private readonly DbSet<BeverageEvent> _events = context.BeverageEvents;

    public async Task<List<Beverage>> GetAsync() 
    {
        return await _beverages
            .ToListAsync();
    }

    public async Task<Beverage?> GetByIdAsync(Guid id) 
    {
        return await _beverages
            .Include(b => b.Events)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task CreateAsync(Beverage beverage) 
    {
        beverage.Active = true;
        beverage.NextPriceDropAt = DateTime.UtcNow.AddMinutes(5);

        BeverageEvent @event = new() {
            BeverageId = beverage.Id,
            Type = BeverageEventType.Created.Value,
            Price = beverage.Price,
            PerformedOn = DateTime.UtcNow
        };

        await _beverages.AddAsync(beverage);
        await _events.AddAsync(@event);
        await context.SaveChangesAsync();
    }

    public async Task<Beverage> PurchaseAsync(Guid id) 
    {
        Beverage beverage = await _beverages.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception($"Could not retrieve beverage with Id {id}.");
        
        beverage.Price = Math.Round(beverage.Price * 1.1m, 2);
        beverage.NextPriceDropAt = DateTime.UtcNow.AddSeconds(10);

        await context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Beverages
            SET 
                Price = {beverage.Price},
                NextPriceDropAt = {beverage.NextPriceDropAt}
            WHERE Id = {id}
        ");

        BeverageEvent @event = new() {
            BeverageId = beverage.Id,
            Type = BeverageEventType.Purchase.Value,
            Price = beverage.Price,
            PerformedOn = DateTime.UtcNow
        };

        await _events.AddAsync(@event);
        await context.SaveChangesAsync();

        return beverage;
    }

    public async Task OnBeverageTimerExpired() 
    {
        Console.WriteLine($"Dropping Prices: {DateTime.Now:MM/dd/yyyy mm:hh:ss tt}");
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE Beverages
            SET 
                Price = ROUND(Price * 0.9, 2),
                NextPriceDropAt = datetime(CURRENT_TIMESTAMP, '+10 seconds')
            WHERE 
                Active = true 
                AND NextPriceDropAt <= CURRENT_TIMESTAMP
                AND Price > 1;
        ");
    }

    public async Task DeactivateAsync(Guid id) 
    {
        Beverage? beverage = await _beverages.FirstOrDefaultAsync(b => b.Id == id);
        if (beverage == null) return;

        beverage.Active = false;
        _beverages.Update(beverage);

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id) 
    {
        Beverage? beverage = await _beverages.FirstOrDefaultAsync(b => b.Id == id);
        if (beverage == null) return;

        _beverages.Remove(beverage);
        
        await context.SaveChangesAsync();
    }
}