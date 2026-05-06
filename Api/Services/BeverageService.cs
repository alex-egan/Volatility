using Api.Models.Database;
using Api.Models.Helpers;
using Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Api.Models.DTO;
using Api.Engines;

namespace Api.Services;

public class BeverageService(DBContext context, IHubContext<BeverageHub> hub, MarketSimulator simulator) 
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

        await _beverages.AddAsync(beverage);
        await context.SaveChangesAsync();

        // 🔥 register in simulator
        simulator.AddDrink(new(beverage));

        await hub.Clients.All.SendAsync("BeverageCreated", beverage);
    }

    public async Task<Beverage> PurchaseAsync(Guid id) 
    {
        var beverage = await _beverages.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception($"Could not retrieve beverage {id}");

        // 1. Tell simulator about purchase
        simulator.RegisterPurchase(id);

        // 2. Run one tick (price reacts immediately)
        simulator.Tick();

        // 3. Get updated price
        var updatedState = simulator.GetDrink(id);

        beverage.Price = Math.Round(updatedState.Price, 2);
        beverage.NextPriceDropAt = DateTime.UtcNow.AddSeconds(10);

        // 4. Persist
        await context.SaveChangesAsync();

        // 5. Log event
        _events.Add(new BeverageEvent
        {
            BeverageId = id,
            Type = BeverageEventType.Purchase.Value,
            Price = beverage.Price,
            PerformedOn = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // 6. Broadcast
        await hub.Clients.All.SendAsync(
            "BeveragePurchased",
            beverage.Id,
            beverage.Price
        );

        return beverage;
    }

    public async Task OnBeverageTimerExpired() 
    {
        Console.WriteLine(Path.GetFullPath(context.Database.GetDbConnection().DataSource));
        Console.WriteLine($"Market Tick: {DateTime.UtcNow}");

        // 1. Run simulation tick
        simulator.Tick();

        // 2. Sync all active beverages
        var beverages = await _beverages
            .Where(b => b.Active)
            .AsNoTracking()
            .ToListAsync();

        Console.WriteLine($"High Price: {beverages.First().HighPrice}");

        var updates = new Dictionary<Guid, decimal>();

        foreach (var b in beverages)
        {
            var state = simulator.GetDrink(b.Id);

            var newPrice = Math.Round(state.Price, 2);

            if (b.Price != newPrice)
            {
                b.Price = newPrice;
                b.NextPriceDropAt = DateTime.UtcNow.AddSeconds(10);

                updates[b.Id] = newPrice;
            }
        }

        await context.SaveChangesAsync();

        if (updates.Count > 0)
        {
            await hub.Clients.All.SendAsync("PricesUpdated", updates);
        }
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