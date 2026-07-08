using Api.Models.DTO;
using Api.Services;

namespace Api.Engines;

public class PricingEngine(ILogger<PricingEngine> logger, IMarketConfigService config)
{
    private readonly ILogger<PricingEngine> _logger = logger;
    private readonly IMarketConfigService _config = config;
    private static readonly Random _rng = new();

    public decimal Calculate(DrinkState drink, BarState bar)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Drink"] = drink.Id,
            ["Tick"] = DateTime.Now
        });

        double basePrice = (double)drink.BasePrice;
        double currentPrice = (double)drink.Price;

        _logger.LogInformation(
            "Tick started for {Drink}: price={CurrentPrice:C}, population={Population}/{MaxPopulation}",
            drink.Id, drink.Price, bar.CustomerCount, bar.ExpectedCustomerCount);

        // 1. Demand pressure: crowd size relative to a "typical max", centered at 0
        double populationRatio = Math.Clamp(
            (double)bar.CustomerCount / Math.Max(1, bar.ExpectedCustomerCount), 0, 1);
        double demandPressure = (populationRatio - 0.5) * 2 * 0.12;

      // 2. Momentum: is this tick's purchase pace above or below the recent trend?
        double recentAvg = drink.PurchaseHistory.Count > 0
            ? drink.PurchaseHistory.Average()
            : drink.PurchaseCount; // no history yet - treat this tick as the baseline, momentum = 0

        double momentumRaw = recentAvg > 0
            ? (drink.PurchaseCount - recentAvg) / recentAvg
            : (drink.PurchaseCount > 0 ? 1 : 0); // recentAvg is 0 but sales just happened - treat as fully positive

        momentumRaw = Math.Clamp(momentumRaw, -1, 1);
        double momentumComponent = momentumRaw * 0.35;

        _logger.LogInformation(
            "Purchases this tick for {Drink}: {Count} vs recent avg {RecentAvg:F2} -> momentum={Momentum:F4}",
            drink.Id, drink.PurchaseCount, recentAvg, momentumRaw);

        // roll this tick's count into history for next time, then reset per your design
        drink.PurchaseHistory.Enqueue(drink.PurchaseCount);
        while (drink.PurchaseHistory.Count > 5)
            drink.PurchaseHistory.Dequeue();

        drink.PurchaseCount = 0;

        // 3. Volatility: Gaussian random walk step scaled by measured volatility
        double noise = NextGaussian();
        double volatilityComponent = noise * (double)drink.Volatility * 0.25;

        // 4. Mean reversion: pulls price back toward BasePrice, proportional to distance
        double meanReversionComponent = (basePrice - currentPrice) / basePrice * 0.06;

        _logger.LogInformation(
            "Components for {Drink}: demand={Demand:F4}, momentum={Momentum:F4}, volatility={Volatility:F4}, reversion={Reversion:F4}",
            drink.Id, demandPressure, momentumComponent, volatilityComponent, meanReversionComponent);

        // Combine forces into a fractional change, applied to current price
        double fractionalDelta = demandPressure + momentumComponent + volatilityComponent + meanReversionComponent;
        double candidatePrice = currentPrice * (1 + fractionalDelta);

        // 5. Time-of-day / special-event multiplier (configurable layer)
        // double timeMultiplier = GetTimeMultiplier(bar.Timestamp, config);
        // candidatePrice *= timeMultiplier;

        // if (Math.Abs(timeMultiplier - 1.0) > 0.0001)
        // {
        //     _logger.LogDebug(
        //         "Time/event multiplier applied for {Drink}: {Multiplier:F2}x at {Timestamp}",
        //         config.DrinkName, timeMultiplier, bar.Timestamp);
        // }

        // 6. Soft max: logistic deceleration as price nears the ceiling
        bool hitSoftMax = candidatePrice > (double)drink.MaxPrice;
        double softCapped = ApplySoftMax(candidatePrice, (double)drink.MaxPrice);

        if (hitSoftMax)
        {
            _logger.LogWarning(
                "{Drink} entered soft-max compression: raw={Raw:C}, compressed={Compressed:C}, ceiling={Ceiling:C}",
                drink.Id, (decimal)candidatePrice, (decimal)softCapped, drink.MaxPrice);
        }

        // 7. Hard min: simple floor clamp, no exceptions
        bool hitHardMin = softCapped < (double)drink.MinPrice;
        double floored = Math.Max(softCapped, (double)drink.MinPrice);

        if (hitHardMin)
        {
            _logger.LogWarning(
                "{Drink} hit hard floor: raw={Raw:C}, floor={Floor:C}",
                drink.Id, (decimal)softCapped, drink.MinPrice);
        }

        // 8. Circuit breaker: cap how much price can move in a single tick
        //double maxChange = currentPrice * (double)config.MaxTickChangePercent;
        //bool circuitBreakerTripped = Math.Abs(floored - currentPrice) > maxChange;
        bool circuitBreakerTripped = false;
        double clamped = floored;

        if (circuitBreakerTripped)
        {
            _logger.LogWarning(
                "Circuit breaker tripped for {Drink}: wanted={Wanted:C}, capped={Capped:C}, maxChangePercent={MaxChangePercent:P0}",
                drink.Id, (decimal)floored, (decimal)clamped, clamped / (double)drink.Price);
        }

        decimal finalPrice = Math.Round((decimal)clamped, 2);

        _logger.LogInformation(
            "{Drink} price updated: {PreviousPrice:C} -> {NewPrice:C}",
            drink.Id, drink.Price, finalPrice);

        return finalPrice;
    }

    // Logistic-style compression: below the threshold ratio, price moves freely.
        // Above it, each additional dollar of "excess" buys diminishing returns,
        // asymptotically approaching SoftMaxPrice rather than hitting a hard wall.
        private static double ApplySoftMax(double price, double softMax)
        {
            double threshold = softMax * 0.8;
            if (price <= threshold) return price;

            double headroom = softMax - threshold;
            double excess = price - threshold;
            double compressed = headroom * (1 - Math.Exp(-2.5 * excess / headroom));
            return threshold + compressed;
        }

        // private static double GetTimeMultiplier(DateTime timestamp)
        // {
        //     double multiplier = config.TimeCurve?.GetMultiplier(timestamp) ?? 1.0;
        //     foreach (var evt in config.SpecialEvents.Where(e => e.IsActive(timestamp)))
        //         multiplier *= evt.Multiplier;
        //     return multiplier;
        // }

    private static double NextGaussian()
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

    // public decimal Calculate(
    //     DrinkState drink,
    //     BarState bar)
    // {
    //     _logger.LogInformation(
    //         "=== PRICE CALCULATION START | DrinkId={DrinkId} | Price={Price} ===",
    //         drink.Id,
    //         drink.Price);

    //     //-----------------------------------
    //     // CONFIGURATION WEIGHTS
    //     //-----------------------------------
    //     // These weights define how strongly each factor influences demand/supply imbalance.
    //     // They act as "market sensitivity coefficients".
    //     decimal wPurchasePressure = _config.Get("Pricing", "wPurchasePressure");
    //     decimal wCustomerPressure  = _config.Get("Pricing", "wCustomerPressure");
    //     decimal wEventPressure     = _config.Get("Pricing", "wEventPressure");

    //     decimal wInventoryPressure  = _config.Get("Pricing", "wInventoryPressure");
    //     decimal wTimePressure       = _config.Get("Pricing", "wTimePressure");

    //     _logger.LogInformation(
    //         "Config Weights | Purchase={wPurchase} Customer={wCustomer} Event={wEvent} Inventory={wInventory} Time={wTime}",
    //         wPurchasePressure,
    //         wCustomerPressure,
    //         wEventPressure,
    //         wInventoryPressure,
    //         wTimePressure);

    //     //-----------------------------------
    //     // DEMAND CALCULATION
    //     //-----------------------------------
    //     // Demand represents upward price pressure from:
    //     // - purchases (direct demand)
    //     // - customer flow (traffic pressure)
    //     // - events (external shocks)

    //     decimal purchasePressure =
    //         CalculateWeightedPressure(drink.ExpectedPurchaseCount, drink.PurchaseCount, wPurchasePressure);
        
    //     // PURCHASE MOMENTUM
    //     //  Average Purchase Pressure over the past 5 intervals
    //     //  Smooths the pricing
    //     drink.PurchaseMomentum.Enqueue(purchasePressure);
    //     while (drink.PurchaseMomentum.Count > 5)
    //     {
    //         drink.PurchaseMomentum.Dequeue();
    //     }

    //     decimal momentum =
    //         drink.PurchaseMomentum.Average() * 0.6m +
    //         purchasePressure * 0.4m;

    //     decimal customerPressure =
    //         CalculateWeightedPressure(bar.ExpectedCustomerCount, bar.CustomerCount, wCustomerPressure);

    //     decimal eventPressure =
    //         CalculateWeightedPressure(bar.ExpectedEventMultiplier, bar.EventMultiplier, wEventPressure);

    //     decimal demand =
    //         momentum +
    //         customerPressure +
    //         eventPressure;

    //     _logger.LogInformation(
    //         "Demand | Purchase={PurchasePressure} Customer={CustomerPressure} Event={EventPressure} Total={Demand}",
    //         purchasePressure,
    //         customerPressure,
    //         eventPressure,
    //         demand);

    //     //-----------------------------------
    //     // SUPPLY CALCULATION
    //     //-----------------------------------
    //     // Supply represents downward pressure from:
    //     // - inventory abundance
    //     // - time decay / stagnation

    //     decimal inventoryPressure =
    //         CalculateWeightedPressure(drink.ExpectedInventory, drink.Inventory, wInventoryPressure);

    //     decimal timePressure =
    //         CalculateWeightedPressure(bar.ExpectedTimeMultiplier, bar.TimeMultiplier, wTimePressure);

    //     decimal supply =
    //         inventoryPressure + timePressure;

    //     _logger.LogInformation(
    //         "Supply | Inventory={InventoryPressure} Time={TimePressure} Total={Supply}",
    //         inventoryPressure,
    //         timePressure,
    //         supply);

    //     _logger.LogWarning(
    //         "RAW PRESSURES | Purchase={Purchase} Customer={Customer} Event={Event} Inventory={Inventory} Time={Time}",
    //         purchasePressure,
    //         customerPressure,
    //         eventPressure,
    //         inventoryPressure,
    //         timePressure);

    //     //-----------------------------------
    //     // MARKET IMBALANCE
    //     //-----------------------------------
    //     // Positive = upward pressure
    //     // Negative = downward pressure

    //     decimal imbalance =
    //         (demand - supply) / Math.Max(1m, demand + supply);

    //     _logger.LogInformation(
    //         "Imbalance | Demand={Demand} Supply={Supply} Imbalance={Imbalance}",
    //         demand,
    //         supply,
    //         imbalance);

    //     //-----------------------------------
    //     // POSITION-BASED RESISTANCE
    //     //-----------------------------------
    //     // Adds stability in mid-range pricing.
    //     // Prevents volatility spikes and chaotic oscillation.

    //     decimal pricePosition =
    //         (drink.Price - drink.MinPrice) /
    //         (drink.MaxPrice - drink.MinPrice);

    //     decimal upwardResistance =
    //         (decimal)Math.Pow((double)pricePosition, 2);

    //     decimal downwardResistance =
    //         (decimal)Math.Pow((double)(1m - pricePosition), 2);

    //     decimal resistance =
    //         imbalance > 0
    //             ? 1m - upwardResistance
    //             : 1m - downwardResistance;

    //     _logger.LogDebug(
    //         "Position Resistance | PricePosition={PricePosition} Resistance={Resistance}",
    //         pricePosition,
    //         resistance);

    //     _logger.LogWarning(
    //         "DEBUG STATE | Demand={Demand} Supply={Supply} Imbalance={Imbalance} Resistance={Resistance} Volatility={Volatility}",
    //         demand,
    //         supply,
    //         imbalance,
    //         resistance,
    //         drink.Volatility);

    //     //-----------------------------------
    //     // RAW PRICE MOVEMENT (DELTA)
    //     //-----------------------------------

    //     decimal delta = imbalance 
    //         * resistance 
    //         * drink.Volatility 
    //         * drink.BasePrice;

    //     //-----------------------------------
    //     // Increase vs. Decrease Multiplier
    //     //-----------------------------------
    //     // delta = delta > 0
    //     //     ? 10 * delta
    //     //     : .5m * delta;

    //     _logger.LogInformation(
    //         "Delta (Pre-MeanReversion) | Delta={Delta}",
    //         delta);

    //     decimal oldPrice = drink.Price;

    //     //-----------------------------------
    //     // UNCLAMPED PRICE UPDATE
    //     //-----------------------------------
    //     decimal finalPrice =
    //         oldPrice + delta;

    //     _logger.LogInformation(
    //         "Final Price | Min={Min} Max={Max} Final={Final}",
    //         drink.MinPrice,
    //         drink.MaxPrice,
    //         finalPrice);

    //     _logger.LogInformation(
    //         "=== PRICE CALCULATION END | DrinkId={DrinkId} ===",
    //         drink.Id);

    //     return finalPrice;
    // }

    private static decimal CalculateWeightedPressure(decimal expected, decimal actual, decimal weight) 
    {
        return CalculateMetricPressure(expected, actual) * weight;
    }

    private static decimal CalculateMetricPressure(decimal expected, decimal actual) 
    {
        decimal ratio = SafeDivide(actual - expected, expected);
        return ratio;
    }

    public static decimal SafeDivide(
        decimal numerator,
        decimal denominator,
        decimal fallback = 0m)
    {
        if (denominator <= 0m)
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