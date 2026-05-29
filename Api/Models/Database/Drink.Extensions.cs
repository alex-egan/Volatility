namespace Api.Models.Database;

public partial class Drink 
{
    public void IncreasePrice()
        => Price *= 1.1m;

    public void DecreasePrice()
        => Price *= 0.9m;
}