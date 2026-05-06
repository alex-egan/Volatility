using Api.Models.DTO;
using Api.Services;

namespace Api.Engines;

public class PricingEngine
{
    private readonly IMarketConfigService _config;

    public PricingEngine(IMarketConfigService config)
    {
        _config = config;
    }

    public decimal Calculate(
        DrinkMarketState drink,
        BarState bar,
        decimal eventImpact)
    {
        // -------------------------
        // CONFIG LOAD (ALL FROM DB)
        // -------------------------

        decimal wDemand = _config.Get("Pricing", "WDemand");
        decimal wSupply = _config.Get("Pricing", "WSupply");

        decimal wMomentum = _config.Get("Pricing", "WMomentum");
        decimal wMeanReversion = _config.Get("Pricing", "WMeanReversion");
        decimal wLiquidity = _config.Get("Pricing", "WLiquidity");

        decimal volatility = _config.Get("Pricing", "Volatility");
        decimal momentumDecay = _config.Get("Pricing", "MomentumDecay");

        decimal maxStep = _config.Get("Pricing", "MaxStep");
        decimal maxMomentum = _config.Get("Pricing", "MaxMomentum");
        decimal minNoise = _config.Get("Pricing", "MinNoise");

        decimal minClamp = _config.Get("Pricing", "MinMultiplier");
        decimal maxClamp = _config.Get("Pricing", "MaxMultiplier");

        decimal wCeilingPressure = _config.Get("Pricing", "WCeilingPressure");
        decimal wPurchaseImpulse = _config.Get("Pricing", "WPurchaseImpulse");
        decimal softMaxMultiplier = _config.Get("Pricing", "SoftMaxMultiplier");
        decimal ceilingExponent = _config.Get("Pricing", "CeilingExponent");
        decimal minPurchaseStep = _config.Get("Pricing", "MinPurchaseStep");

        Console.WriteLine("----- CONFIG -----");
        Console.WriteLine($"WDemand: {wDemand}, WSupply: {wSupply}");
        Console.WriteLine($"WMomentum: {wMomentum}, WMeanReversion: {wMeanReversion}, WLiquidity: {wLiquidity}");
        Console.WriteLine($"Volatility: {volatility}, MomentumDecay: {momentumDecay}");
        Console.WriteLine($"MaxStep: {maxStep}, MaxMomentum: {maxMomentum}, MinNoise: {minNoise}");
        Console.WriteLine($"Clamp: {minClamp} - {maxClamp}");
        Console.WriteLine($"CeilingPressure: {wCeilingPressure}");
        Console.WriteLine($"Purchase Impulse: {wPurchaseImpulse}");
        Console.WriteLine($"Soft Max Multiplier: {softMaxMultiplier}");
        Console.WriteLine($"Ceiling Exponent: {ceilingExponent}");
        Console.WriteLine($"Min Purchase Step: {minPurchaseStep}");

        // -------------------------
        // SMOOTHED DEMAND INPUT
        // -------------------------

        drink.SmoothedVelocity =
            drink.SmoothedVelocity * 0.8m +
            drink.OrderVelocity * 0.2m;

        var demand = (double)drink.SmoothedVelocity;

        Console.WriteLine("----- DEMAND -----");
        Console.WriteLine($"Raw Velocity: {drink.OrderVelocity}");
        Console.WriteLine($"Smoothed Velocity: {drink.SmoothedVelocity}");

        // -------------------------
        // FUNDAMENTAL VALUE
        // -------------------------

        var priceRatio = (double)(drink.Price / drink.MaxPrice);

        var demandEffect =
            demand * (1 - Math.Pow(priceRatio, 1.5));

        decimal supplyPressure =
            1 - SafeDiv(drink.Inventory, drink.InventoryMax);

        decimal fairValue =
            drink.BasePrice *
            (1 + ((decimal)demandEffect * wDemand) - (supplyPressure * wSupply));

        Console.WriteLine("----- FUNDAMENTALS -----");
        Console.WriteLine($"Price: {drink.Price}, FairValue: {fairValue}");
        Console.WriteLine($"DemandEffect: {demandEffect}, SupplyPressure: {supplyPressure}");

        // -------------------------
        // MOMENTUM (STATEFUL)
        // -------------------------

        drink.Momentum += (decimal)demandEffect * wMomentum;
        drink.Momentum *= (1 - momentumDecay);

        drink.Momentum = Math.Clamp(drink.Momentum, -maxMomentum, maxMomentum);

        Console.WriteLine("----- MOMENTUM -----");
        Console.WriteLine($"Momentum: {drink.Momentum}");

        // -------------------------
        // MARKET FORCES
        // -------------------------

        decimal drift =
            (fairValue - drink.Price) / fairValue;

        drift *= wMeanReversion;

        decimal momentumEffect = drink.Momentum;

        decimal baseNoise =
            (decimal)Random.Shared.NextDouble() - 0.5m;

        decimal noise =
            baseNoise * volatility;

        if (Math.Abs(noise) < minNoise)
        {
            noise = Math.Sign(noise == 0 ? 1 : noise) * minNoise;
        }

        decimal liquidityDamping =
            1 / (1 + drink.Liquidity * wLiquidity);

        Console.WriteLine("----- MARKET FORCES -----");
        Console.WriteLine($"Drift: {drift}");
        Console.WriteLine($"MomentumEffect: {momentumEffect}");
        Console.WriteLine($"Noise: {noise}");
        Console.WriteLine($"LiquidityDamping: {liquidityDamping}");
        Console.WriteLine($"EventImpact: {eventImpact}");

        // -------------------------
        // TOTAL CHANGE (LOG RETURN)
        // -------------------------

        decimal change =
            (drift + momentumEffect + noise + eventImpact)
            * liquidityDamping;

        if (eventImpact > 0)
        {
            decimal purchaseImpulse =
                wPurchaseImpulse * (1 + (decimal)Math.Abs(drink.Momentum));

            purchaseImpulse = Math.Max(purchaseImpulse, minPurchaseStep);

            change += purchaseImpulse;
        }

        if (eventImpact > 0)
        {
            change = Math.Max(change, minPurchaseStep);
        }

        // soft ceiling (prevents hard plateau)
        decimal softMaxPrice = drink.MaxPrice * softMaxMultiplier;

        // how close we are to soft cap (0 = far, 1+ = beyond cap)
        decimal ceilingRatio = drink.Price / softMaxPrice;

        // smooth nonlinear pressure curve
        decimal ceilingPressure =
            (decimal)Math.Pow((double)Math.Max(0, ceilingRatio), (double)ceilingExponent);

        // only apply pressure when approaching cap meaningfully
        if (drink.Price > softMaxPrice * 0.6m)
        {
            change -= ceilingPressure * wCeilingPressure;
        }

        Console.WriteLine("----- CHANGE -----");
        Console.WriteLine($"Final Change: {change}");

        // -------------------------
        // PRICE EVOLUTION (LOG-BASED)
        // -------------------------

        decimal newPrice =
            drink.Price * (decimal)Math.Exp((double)change);

        Console.WriteLine("----- PRICE -----");
        Console.WriteLine($"Old Price: {drink.Price}");
        Console.WriteLine($"New Price (pre-clamp): {newPrice}");

        // -------------------------
        // SAFETY CLAMP ONLY
        // -------------------------

        decimal finalPrice = Clamp(
            newPrice,
            drink.BasePrice * minClamp,
            drink.BasePrice * maxClamp
        );

        Console.WriteLine($"Final Price: {finalPrice}");
        Console.WriteLine("--------------------------");

        return finalPrice;
    }

    private static decimal SafeDiv(decimal a, decimal b)
        => b == 0 ? 0 : a / b;

    private static decimal Clamp(decimal v, decimal min, decimal max)
        => Math.Round(Math.Max(min, Math.Min(max, v)), 2);
}