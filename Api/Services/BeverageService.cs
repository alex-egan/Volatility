using Api.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BeverageService(DBContext context) 
{
    private readonly DbSet<Beverage> _beverages = context.Beverages;

    public async Task<List<Beverage>> GetAsync() 
    {
        return await _beverages.ToListAsync();
    }

    public async Task CreateAsync(Beverage beverage) 
    {
        beverage.NextPriceDropAt = DateTime.UtcNow.AddSeconds(30);
        await _beverages.AddAsync(beverage);
        await context.SaveChangesAsync();
    }

    public async Task<Beverage> PurchaseAsync(Guid id) 
    {
        Beverage beverage = await _beverages.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception($"Could not retrieve beverage with Id {id}.");
        
        beverage.Price *= 1.1m;
        beverage.NextPriceDropAt = DateTime.UtcNow.AddMinutes(1);

        await context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Beverages
            SET 
                Price = {beverage.Price},
                NextPriceDropAt = {beverage.NextPriceDropAt}
            WHERE Id = {id}
        ");

        return beverage;
    }

    public async Task OnBeverageTimerExpired() 
    {
        Console.WriteLine($"Dropping Prices: {DateTime.Now:MM/dd/yyyy mm:hh:ss tt}");
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE Beverages
            SET 
                Price = Price * 0.9,
                NextPriceDropAt = datetime(NextPriceDropAt, '+2 minutes')
            WHERE NextPriceDropAt <= CURRENT_TIMESTAMP;
        ");
    }
}