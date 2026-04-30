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
    public decimal Price { get; set; }
    public decimal LowPrice { get; set; }
    public decimal HighPrice { get; set; }

    public DateTime NextPriceDropAt { get; set; }

    public bool Active { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
}