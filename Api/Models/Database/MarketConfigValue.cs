namespace Api.Models.Database;

public class MarketConfigValue
{
    public Guid Id { get; set; }

    public string Category { get; set; } = null!; // Pricing, Market, Events
    public string Key { get; set; } = null!;

    public decimal Value { get; set; }

    public DateTime UpdatedAt { get; set; }
}