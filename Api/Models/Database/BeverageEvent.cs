using System.Text.Json.Serialization;
using Api.Models.Helpers;

namespace Api.Models.Database;

public class BeverageEvent
{
    public int Id { get; set; }
    public Guid BeverageId { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = BeverageEventType.PriceDecreased.Value;
    public DateTime PerformedOn { get; set; }

    [JsonIgnore]
    public Beverage Beverage { get; set; } = null!;
}