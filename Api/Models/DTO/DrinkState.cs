using Api.Models.Database;

namespace Api.Models.DTO;

public class DrinkState
{
    public Guid Id { get; set; }

    public decimal Price { get; set; }
    public decimal BasePrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal MinPrice { get; set; }

    public decimal Inventory { get; set; } = 1.0m;
    public decimal ExpectedInventory { get; set; } = 1.0m;

    public int PurchaseCount { get; set; }
    public int ExpectedPurchaseCount { get; set; } = 1;

    /// <summary>
    ///     Stores the last 5 purchase pressure values
    ///     Used to calculate momentum for smoother pricing
    /// </summary>
    public Queue<decimal> PurchaseMomentum { get; set; } = new(5);

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