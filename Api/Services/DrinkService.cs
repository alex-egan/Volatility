using Api.Models.Database;
using Api.Models.Helpers;
using Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Api.Engines;
using Api.Models.DTO;

namespace Api.Services;

public class DrinkService(DBContext context, IHubContext<DrinkHub> hub, MarketSimulator simulator) 
{
    private readonly DbSet<Drink> _drinks = context.Drinks;
    private readonly DbSet<DrinkEvent> _events = context.DrinkEvents;

    public async Task<List<Drink>> GetAsync() 
    {
        return await _drinks
            .ToListAsync();
    }

    public async Task<Drink?> GetByIdAsync(Guid id) 
    {
        return await _drinks
            .Include(b => b.Events)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task CreateAsync(Drink drink) 
    {
        drink.Active = true;
        
        await _drinks.AddAsync(drink);
        await context.SaveChangesAsync();

        // 🔥 register in simulator
        simulator.AddDrink(new(drink));

        await hub.Clients.All.SendAsync("BeverageCreated", drink);
    }

    public Task PurchaseAsync(Guid id) 
    {
        simulator.Purchase(id);
        return Task.CompletedTask;
    }

    public async Task OnDrinkTimerExpired() 
    {
        Console.WriteLine(Path.GetFullPath(context.Database.GetDbConnection().DataSource));
        Console.WriteLine($"Market Tick: {DateTime.UtcNow}");

        // 1. Run simulation tick
        simulator.Tick();

        // 2. Sync all active beverages
        List<Drink> drinks = await _drinks
            .Where(b => b.Active)
            .ToListAsync();

        Console.WriteLine($"High Price: {drinks.First().MaxPrice}");

        Dictionary<Guid, decimal> updates = [];

        foreach (Drink drink in drinks)
        {
            DrinkState? state = simulator.GetDrink(drink.Id);
            state.Volatility = drink.Volatility;

            decimal newPrice = Math.Round(state.Price, 2);

            updates.TryAdd(drink.Id, newPrice);

            drink.Price = newPrice;
            _events.Add(new DrinkEvent
                {
                    DrinkId = drink.Id,
                    Type = BeverageEventType.Purchase.Value,
                    Price = newPrice,
                    PerformedOn = DateTime.UtcNow
                });
        }

        await context.SaveChangesAsync();
        await hub.Clients.All.SendAsync("UpdatePrices", updates);
    }

    public async Task DeactivateAsync(Guid id) 
    {
        Drink? beverage = await _drinks.FirstOrDefaultAsync(b => b.Id == id);
        if (beverage == null) return;

        beverage.Active = false;
        _drinks.Update(beverage);

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id) 
    {
        Drink? drink = await _drinks.FirstOrDefaultAsync(b => b.Id == id);
        if (drink == null) return;

        _drinks.Remove(drink);
        
        await context.SaveChangesAsync();
    }
}