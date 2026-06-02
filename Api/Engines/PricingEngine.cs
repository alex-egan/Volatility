using Api.Models.DTO;
using Api.Services;

namespace Api.Engines;

public class PricingEngine
{
    private readonly ILogger<PricingEngine> _logger;
    private readonly IMarketConfigService _config;

    public PricingEngine(ILogger<PricingEngine> logger, IMarketConfigService config)
    {
        _logger = logger;
        _config = config;
    }

    public decimal Calculate(
        DrinkState drink,
        BarState bar)
    {
        _logger.LogInformation(
            "=== PRICE CALCULATION START | DrinkId={DrinkId} | Price={Price} ===",
            drink.Id,
            drink.Price);

        //-----------------------------------
        // CONFIGURATION WEIGHTS
        //-----------------------------------

        // These weights define how strongly each factor influences demand/supply imbalance.
        // They act as "market sensitivity coefficients".
        decimal wPurchasePressure = _config.Get("Pricing", "wPurchasePressure");
        decimal wCustomerPressure  = _config.Get("Pricing", "wCustomerPressure");
        decimal wEventPressure     = _config.Get("Pricing", "wEventPressure");

        decimal wInventoryPressure  = _config.Get("Pricing", "wInventoryPressure");
        decimal wTimePressure       = _config.Get("Pricing", "wTimePressure");

        _logger.LogInformation(
            "Config Weights | Purchase={wPurchase} Customer={wCustomer} Event={wEvent} Inventory={wInventory} Time={wTime}",
            wPurchasePressure,
            wCustomerPressure,
            wEventPressure,
            wInventoryPressure,
            wTimePressure);

        //-----------------------------------
        // DEMAND CALCULATION
        //-----------------------------------
        // Demand represents upward price pressure from:
        // - purchases (direct demand)
        // - customer flow (traffic pressure)
        // - events (external shocks)

        decimal purchasePressure =
            CalculateWeightedPressure(drink.ExpectedPurchaseCount, drink.PurchaseCount, wPurchasePressure);
        
        // PURCHASE MOMENTUM
        //  Average Purchase Pressure over the past 5 intervals
        //  Smooths the pricing
        drink.PurchaseMomentum.Enqueue(purchasePressure);
        while (drink.PurchaseMomentum.Count > 5)
        {
            drink.PurchaseMomentum.Dequeue();
        }

        decimal momentum =
            drink.PurchaseMomentum.Average();

        decimal customerPressure =
            CalculateWeightedPressure(bar.ExpectedCustomerCount, bar.CustomerCount, wCustomerPressure);

        decimal eventPressure =
            CalculateWeightedPressure(bar.ExpectedEventMultiplier, bar.EventMultiplier, wEventPressure);

        decimal demand =
            momentum +
            customerPressure +
            eventPressure;

        _logger.LogInformation(
            "Demand | Purchase={PurchasePressure} Customer={CustomerPressure} Event={EventPressure} Total={Demand}",
            purchasePressure,
            customerPressure,
            eventPressure,
            demand);

        //-----------------------------------
        // SUPPLY CALCULATION
        //-----------------------------------
        // Supply represents downward pressure from:
        // - inventory abundance
        // - time decay / stagnation

        decimal inventoryPressure =
            CalculateWeightedPressure(drink.ExpectedInventory, drink.Inventory, wInventoryPressure);

        decimal timePressure =
            CalculateWeightedPressure(bar.ExpectedTimeMultiplier, bar.TimeMultiplier, wTimePressure);

        decimal supply =
            inventoryPressure + timePressure;

        _logger.LogInformation(
            "Supply | Inventory={InventoryPressure} Time={TimePressure} Total={Supply}",
            inventoryPressure,
            timePressure,
            supply);

        _logger.LogWarning(
            "RAW PRESSURES | Purchase={Purchase} Customer={Customer} Event={Event} Inventory={Inventory} Time={Time}",
            purchasePressure,
            customerPressure,
            eventPressure,
            inventoryPressure,
            timePressure);

        //-----------------------------------
        // MARKET IMBALANCE
        //-----------------------------------
        // Positive = upward pressure
        // Negative = downward pressure

        decimal imbalance =
            (demand - supply) / Math.Max(1m, demand + supply);

        _logger.LogInformation(
            "Imbalance | Demand={Demand} Supply={Supply} Imbalance={Imbalance}",
            demand,
            supply,
            imbalance);

        //-----------------------------------
        // POSITION-BASED RESISTANCE
        //-----------------------------------
        // Adds stability in mid-range pricing.
        // Prevents volatility spikes and chaotic oscillation.

        decimal pricePosition =
            (drink.Price - drink.MinPrice) /
            (drink.MaxPrice - drink.MinPrice);

        decimal positionResistance =
            1 - (decimal)Math.Pow((double)Math.Abs(pricePosition - 0.5m) * 2, 2);

        _logger.LogDebug(
            "Position Resistance | PricePosition={PricePosition} Resistance={Resistance}",
            pricePosition,
            positionResistance);

        //-----------------------------------
        // CEILING RESISTANCE (SOFT BOUNDARY)
        //-----------------------------------
        // Replaces hard max price.
        // As price exceeds expected range, resistance grows exponentially.

        decimal distanceFromMax =
            drink.Price - drink.MaxPrice;

        decimal ceilingResistance =
            (decimal)Math.Exp(
                (double)Math.Abs(distanceFromMax / drink.MaxPrice) * 3
            );

        _logger.LogDebug(
            "Ceiling Resistance | DistanceFromMax={Overshoot} Resistance={Resistance}",
            distanceFromMax,
            ceilingResistance);

        //-----------------------------------
        // COMBINED RESISTANCE MODEL
        //-----------------------------------
        // Position resistance controls mid-range stability.
        // Ceiling resistance controls extreme growth friction.

        decimal totalResistance =
            positionResistance * (2m - Math.Min(ceilingResistance, 1.5m));

        _logger.LogWarning(
            "DEBUG STATE | Demand={Demand} Supply={Supply} Imbalance={Imbalance} Resistance={Resistance} Volatility={Volatility}",
            demand,
            supply,
            imbalance,
            totalResistance,
            drink.Volatility);

        //-----------------------------------
        // RAW PRICE MOVEMENT (DELTA)
        //-----------------------------------
        // decimal delta =
        //     imbalance *
        //     totalResistance *
        //     drink.Volatility *
        //     drink.BasePrice;

        decimal delta =
            imbalance * totalResistance * drink.Volatility * drink.BasePrice;

        _logger.LogInformation(
            "Delta (Pre-MeanReversion) | Delta={Delta}",
            delta);

        decimal oldPrice = drink.Price;

        //-----------------------------------
        // UNCLAMPED PRICE UPDATE
        //-----------------------------------
        decimal finalPrice =
            oldPrice + delta;

        _logger.LogInformation(
            "Final Price | Min={Min} Max={Max} Final={Final}",
            drink.MinPrice,
            drink.MaxPrice,
            finalPrice);

        _logger.LogInformation(
            "=== PRICE CALCULATION END | DrinkId={DrinkId} ===",
            drink.Id);

        return finalPrice;
    }

    private decimal CalculateWeightedPressure(decimal expected, decimal actual, decimal weight) {
        return CalculateMetricPressure(expected, actual) * weight;
    }

    private decimal CalculateMetricPressure(decimal expected, decimal actual) 
    {
        decimal ratio = SafeDivide(actual - expected, actual);
        return Math.Round(ratio, 2);
    }

    public static decimal SafeDivide(
        decimal numerator,
        decimal denominator,
        decimal fallback = 0m)
    {
        if (denominator == 0m)
            return fallback;

        return numerator / denominator;
    }

    public void RecalculateDrinkPrices(
        IList<DrinkState> drinks,
        BarState bar,
        decimal eventImpact)
    {
        // -------------------------
        // CONFIG LOAD (DB DRIVEN)
        // -------------------------
        decimal kMomentum = _config.Get("Pricing", "MomentumSensitivity");   // k
        decimal wVolatility = _config.Get("Pricing", "PriceVolatility");     // w
        decimal decayRate = _config.Get("Pricing", "DecayRate");             // optional
        decimal enableDecay = _config.Get("Pricing", "EnableDecay");         // 0 or 1

        Console.WriteLine("=== PRICE RECALC START ===");
        Console.WriteLine($"Momentum k={kMomentum}, Volatility w={wVolatility}, Decay={decayRate}");

        // -------------------------
        // LOOP DRINKS
        // -------------------------
        foreach (DrinkState drink in drinks)
        {
            decimal oldPrice = drink.Price;
            decimal purchases = drink.PurchaseCount;

            // -------------------------
            // STEP 1: MOMENTUM (LOGARITHMIC)
            // -------------------------
            decimal momentum = (decimal)Math.Log(1 + (double)(kMomentum * purchases));

            // -------------------------
            // STEP 2: PRICE CHANGE PERCENT
            // -------------------------
            decimal priceChangePct = wVolatility * momentum;

            // -------------------------
            // STEP 3: OPTIONAL MEAN REVERSION / DECAY
            // -------------------------
            if (enableDecay > 0)
            {
                // soft decay when no demand
                if (purchases <= 0)
                {
                    priceChangePct -= decayRate;
                    Console.WriteLine($"[{drink.Id}] No demand decay applied: -{decayRate}");
                }
            }

            // -------------------------
            // STEP 4: APPLY MULTIPLICATIVE UPDATE
            // -------------------------
            decimal newPrice = oldPrice * (1 + priceChangePct);

            // -------------------------
            // STEP 5: SAFETY FLOOR (ONLY FOR STABILITY)
            // -------------------------
            if (newPrice < drink.MinPrice)
                newPrice = drink.MinPrice;

            // -------------------------
            // STEP 6: WRITE BACK + RESET WINDOW
            // -------------------------
            drink.Price = newPrice;

            drink.ExpectedInventory = drink.Inventory;

            drink.ExpectedPurchaseCount = drink.PurchaseCount;
            drink.PurchaseCount = 0; // reset for next interval

            // -------------------------
            // LOGGING
            // -------------------------
            Console.WriteLine(
                $"[{drink.Id}] old={oldPrice:F2}, purchases={purchases}, " +
                $"momentum={momentum:F4}, change%={priceChangePct:P2}, new={newPrice:F2}"
            );
        }

        Console.WriteLine("=== PRICE RECALC END ===");
    }

    private static decimal SafeDiv(decimal a, decimal b)
        => b == 0 ? 0 : a / b;

    private static decimal Clamp(decimal v, decimal min, decimal max)
        => Math.Round(Math.Max(min, Math.Min(max, v)), 2);
}