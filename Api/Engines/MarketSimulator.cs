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

    private readonly Dictionary<Guid, DrinkMarketState> _drinks;

    public MarketSimulator(
        BarState bar,
        EventService events,
        PricingEngine pricing,
        Dictionary<Guid, DrinkMarketState> drinks)
    {
        _bar = bar;
        _events = events;
        _pricing = pricing;
        _drinks = drinks;
    }

    public void Tick()
    {
        var eventImpact = _events.GetImpact();

        foreach (var drink in _drinks.Values)
        {
            Decay(drink);

            drink.Price = _pricing.Calculate(
                drink,
                _bar,
                eventImpact
            );
        }
    }

    public void AddDrink(DrinkMarketState drink)
    {
        _drinks[drink.DrinkId] = drink;
    }

    public DrinkMarketState GetDrink(Guid id)
    {
        return _drinks[id];
    }

    public void RegisterPurchase(Guid drinkId)
    {
        var d = _drinks[drinkId];

        d.OrderVelocity += 1;
        d.Momentum = (d.Momentum * 0.7m) + 0.3m;

        d.Inventory = Math.Max(0, d.Inventory - 1);

        _bar.UpdateGuests(1);
    }

    private void Decay(DrinkMarketState drink)
    {
        drink.OrderVelocity *= 0.9m;
        drink.Momentum *= 0.85m;
    }
}