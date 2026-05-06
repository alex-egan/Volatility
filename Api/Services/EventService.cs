using Api.Models.DTO;

namespace Api.Services;

public class EventService
{
    private readonly List<MarketEvent> _events = new();

    public void AddEvent(string name, decimal impact, TimeSpan duration)
    {
        _events.Add(new MarketEvent
        {
            Name = name,
            Impact = impact,
            ExpiresAt = DateTime.UtcNow.Add(duration)
        });
    }

    public decimal GetImpact()
    {
        var now = DateTime.UtcNow;

        _events.RemoveAll(e => e.ExpiresAt <= now);

        return _events.Sum(e => e.Impact);
    }
}
