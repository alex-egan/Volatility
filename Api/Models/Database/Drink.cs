using Api.Models.Enums;
using Api.Models.Interfaces;

namespace Api.Models.Database;

public partial class Drink : IDrink
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BeverageType Type { get; set; }
    public List<BeverageContainerType> AvailableContainers { get; set; } = [];
    public string Code { get; set; } = string.Empty;
    public decimal BasePrice { get; set; } = 6.50m;
    public decimal Price { get; set; }
    public decimal MinPrice { get; set; } = 2.0m;
    public decimal MaxPrice { get; set; } = 20.0m;
    public decimal Volatility { get; set; } = 0.05m;

    public decimal Inventory { get; set; }

    public bool Active { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }

    public List<DrinkEvent> Events { get; set; } = [];
}