using System.Text.Json.Serialization;
using Api.Models.Helpers;

namespace Api.Models.Database;

public class DrinkEvent
{
    public int Id { get; set; }
    public Guid DrinkId { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = BeverageEventType.PriceDecreased.Value;
    public DateTime PerformedOn { get; set; }

    [JsonIgnore]
    public Drink Drink { get; set; } = null!;
}