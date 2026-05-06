using Api.Models.Enums;
using Api.Models.Interfaces;

namespace Api.Models.Database;

public partial class Beverage : IBeverage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BeverageType Type { get; set; }
    public List<BeverageContainerType> AvailableContainers { get; set; } = [];
    public string Code { get; set; } = string.Empty;
    public decimal BasePrice { get; set; } = 6.50m;
    public decimal Price { get; set; }
    public decimal LowPrice { get; set; } = 2.0m;
    public decimal HighPrice { get; set; } = 20.0m;

    public decimal Inventory { get; set; }
    public decimal InventoryMax { get; set; }


    public DateTime NextPriceDropAt { get; set; }

    public bool Active { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }

    public List<BeverageEvent> Events { get; set; } = [];
}