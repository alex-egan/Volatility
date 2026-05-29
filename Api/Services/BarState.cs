namespace Api.Services;

public class BarState
{
    public int CustomerCount { get; private set; } = 10;
    public int ExpectedCustomerCount { get; set;} = 15;
    public int MaxCustomers { get; set; }
    public decimal EventMultiplier { get; set;} = 0.0m;
    public decimal ExpectedEventMultiplier { get; set; } = 0.0m;

    public decimal TimeMultiplier { get; set; }
    public decimal ExpectedTimeMultiplier { get; set; }

    public int ActiveStaff { get; set; }
    public int MaxStaff { get; set; }

    public void UpdateGuests(int delta)
    {
        CustomerCount = Math.Max(0, CustomerCount + delta);
    }
}