using Api.Models.Database;

namespace Api.Models.DTO;

public class DrinkMarketState
{
    public Guid DrinkId { get; set; }

    public decimal Price { get; set; }
    public decimal BasePrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal MinPrice { get; set; }

    public decimal OrderVelocity { get; set; }
    public decimal SmoothedVelocity { get; set; }
    public decimal Momentum { get; set; }

    public decimal Liquidity { get; set; } = 0.5m;

    public decimal Inventory { get; set; }
    public decimal InventoryMax { get; set; }

    public DrinkMarketState() {}

    public DrinkMarketState(Beverage b)
    {
        DrinkId = b.Id;
        Price = b.Price;
        BasePrice = b.Price;
        MinPrice = b.LowPrice;
        MaxPrice = b.HighPrice;

        Inventory = b.Inventory;
        InventoryMax = b.InventoryMax;
    }
}