namespace Api.Models.Helpers;

public sealed class BeverageEventType
{
    public string Value { get; }

    private BeverageEventType(string value)
    {
        Value = value;
    }

    public static readonly BeverageEventType Created = new("CREATED");
    public static readonly BeverageEventType Purchase = new("PURCHASED");
    public static readonly BeverageEventType PriceDecreased = new("PRICE_DECREASED");
    public static readonly BeverageEventType PriceIncreased = new("PRICE_INCREASED");

    public override string ToString() => Value;

    // Optional: equality support
    public override bool Equals(object? obj)
        => obj is BeverageEventType other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(BeverageEventType a, BeverageEventType b)
        => a?.Value == b?.Value;

    public static bool operator !=(BeverageEventType a, BeverageEventType b)
        => !(a == b);

    // Optional: parsing from DB
    public static BeverageEventType From(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "PURCHASE" => Purchase,
            "PRICE_DECREASED" => PriceDecreased,
            "PRICE_INCREASED" => PriceIncreased,
            _ => new BeverageEventType(value) // allows unknown future values
        };
    }
}