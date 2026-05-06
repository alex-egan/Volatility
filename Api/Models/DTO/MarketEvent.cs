namespace Api.Models.DTO;

public class MarketEvent
{
    public string Name { get; set; } = "";
    public decimal Impact { get; set; }
    public DateTime ExpiresAt { get; set; }
}