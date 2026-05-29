using Api.Models.Database;

namespace Api.Models.DTO;

public class DrinkState
{
    public Guid Id { get; set; }

    public decimal Price { get; set; }
    public decimal BasePrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal MinPrice { get; set; }

    // TODO: This should be a calculation eventually depending on purchase rate
    public decimal Inventory { get; set; } = 1.0m;
    public decimal ExpectedInventory { get; set; } = 1.0m;

    public int PurchaseCount { get; set; }
    public int ExpectedPurchaseCount { get; set; } = 1;

    public decimal Volatility { get; set; } = 0.05m;

    public DrinkState() {}

    public DrinkState(Drink b)
    {
        Id = b.Id;
        Price = b.Price;
        BasePrice = b.Price;
        MinPrice = b.MinPrice;
        MaxPrice = b.MaxPrice;
        Inventory = b.Inventory;
        Volatility = b.Volatility;
    }
}