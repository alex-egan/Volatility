using Microsoft.EntityFrameworkCore;

namespace Api.Models.Database;

public class DBContext(DbContextOptions<DBContext> options) : DbContext(options), IDisposable
{
    public DbSet<Beverage> Beverages { get; set; } = null!;
    public DbSet<BeverageEvent> BeverageEvents { get; set; } = null!;
    public DbSet<Tab> Tabs { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!; 
    public DbSet<MarketConfigValue> MarketConfigValues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketConfigValue>()
            .HasIndex(x => new { x.Category, x.Key })
            .IsUnique();
    }
}