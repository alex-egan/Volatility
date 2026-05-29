using Api.Models.Database;
using Api.Models.DTO;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Engines;

public class MarketSimulator
{
    private readonly BarState _bar;
    private readonly EventService _events;
    private readonly PricingEngine _pricing;

    private readonly Dictionary<Guid, DrinkState> _drinks;

    public MarketSimulator(
        BarState bar,
        EventService events,
        PricingEngine pricing,
        Dictionary<Guid, DrinkState> drinks)
    {
        _bar = bar;
        _events = events;
        _pricing = pricing;
        _drinks = drinks;
    }

    public void Tick()
    {
        foreach (DrinkState drink in _drinks.Values)
        {
            // Price update
            drink.Price = _pricing.Calculate(drink, _bar);
            
            drink.PurchaseCount = 0;
        }
    }

    public void Purchase(Guid id) 
    {
        _drinks[id].PurchaseCount += 1;
    }

    public void AddDrink(DrinkState drink)
    {
        _drinks[drink.Id] = drink;
    }

    public DrinkState GetDrink(Guid id)
    {
        return _drinks[id];
    }
}