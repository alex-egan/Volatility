using Api.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class MarketConfigService : IMarketConfigService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private Dictionary<string, decimal> _cache = new();
    private readonly object _lock = new();

    public MarketConfigService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public decimal Get(string category, string key)
    {
        var fullKey = $"{category}:{key}";

        return _cache.TryGetValue(fullKey, out var value)
            ? value
            : 0m; // safe fallback
    }

    public async Task ReloadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DBContext>();

        var entries = await db.MarketConfigValues.ToListAsync();

        var newCache = entries.ToDictionary(
            x => $"{x.Category}:{x.Key}",
            x => x.Value
        );

        lock (_lock)
        {
            _cache = newCache;
        }
    }
}