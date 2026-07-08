using System;
using System.Collections.Generic;
using System.Linq;

namespace BarExchange
{
    // ---------------------------------------------------------------------
    // CONFIGURATION
    // Everything an operator can tune without touching the algorithm itself.
    // ---------------------------------------------------------------------
    public class DrinkPricingConfig
    {
        public string DrinkName { get; set; } = "House Special";

        // Anchor / bounds
        public decimal BasePrice { get; set; } = 6.00m;      // "fundamental value" the price reverts to
        public decimal HardMinPrice { get; set; } = 3.00m;   // never sell below cost+margin floor
        public decimal SoftMaxPrice { get; set; } = 15.00m;  // ceiling the price decelerates into, not slams into

        // Force weights (tune these to change the "personality" of the market)
        public double PopulationSensitivity { get; set; } = 0.12;  // how hard crowd size pushes price
        public double MomentumWeight { get; set; } = 0.35;         // how much recent trend continues
        public double VolatilityWeight { get; set; } = 0.25;       // how much randomness shows up per tick
        public double MeanReversionSpeed { get; set; } = 0.06;     // how fast price is pulled back to BasePrice

        // Soft-max shaping
        public double SoftCapThresholdRatio { get; set; } = 0.8;   // % of SoftMax where deceleration starts
        public double SoftCapSharpness { get; set; } = 2.5;        // higher = harder resistance near the ceiling

        // Configurable multiplier layers (date/time/events)
        public TimeOfDayCurve TimeCurve { get; set; } = TimeOfDayCurve.Default();
        public List<SpecialEvent> SpecialEvents { get; set; } = new();

        public decimal MaxTickChangePercent { get; set; } = 0.08m; // circuit breaker: cap % move per tick
    }

    // Multiplier curve keyed by hour-of-day, e.g. happy hour discount, late-night surge
    public class TimeOfDayCurve
    {
        // 24 entries, one multiplier per hour (0-23)
        public double[] HourlyMultipliers { get; set; } = new double[24];

        public double GetMultiplier(DateTime timestamp) => HourlyMultipliers[timestamp.Hour];

        public static TimeOfDayCurve Default()
        {
            var curve = new TimeOfDayCurve();
            for (int h = 0; h < 24; h++)
            {
                curve.HourlyMultipliers[h] = h switch
                {
                    >= 16 and < 18 => 0.85,  // happy hour discount
                    >= 22 or < 2 => 1.15,    // late-night surge
                    _ => 1.0
                };
            }
            return curve;
        }
    }

    // One-off or recurring events (New Year's Eve, game night, private booking, etc.)
    public class SpecialEvent
    {
        public string Name { get; set; } = "";
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public double Multiplier { get; set; } = 1.0;

        public bool IsActive(DateTime timestamp) => timestamp >= StartsAt && timestamp <= EndsAt;
    }

    // ---------------------------------------------------------------------
    // MARKET STATE
    // Snapshot of "current conditions" fed into Calculate() each tick.
    // ---------------------------------------------------------------------
    public class MarketSnapshot
    {
        public decimal CurrentPrice { get; set; }
        public int BarPopulation { get; set; }
        public int TypicalMaxPopulation { get; set; } = 150; // used to normalize crowd size to 0-1
        public double RecentMomentum { get; set; }           // e.g. EMA of last N price deltas, roughly -1..1
        public double RecentVolatility { get; set; }         // e.g. stddev of last N price deltas, in price units
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // ---------------------------------------------------------------------
    // RESULT
    // Returns the new price plus a breakdown, so a UI can render it like
    // a trading terminal ("price moved because of X, Y, Z").
    // ---------------------------------------------------------------------
    public class PriceResult
    {
        public decimal PreviousPrice { get; set; }
        public decimal NewPrice { get; set; }
        public double DemandPressure { get; set; }
        public double MomentumComponent { get; set; }
        public double VolatilityComponent { get; set; }
        public double MeanReversionComponent { get; set; }
        public double TimeMultiplier { get; set; }
        public bool HitSoftMax { get; set; }
        public bool HitHardMin { get; set; }
        public bool CircuitBreakerTripped { get; set; }
    }

    // ---------------------------------------------------------------------
    // ENGINE
    // ---------------------------------------------------------------------
    public static class DrinkPricingEngine
    {
        private static readonly Random _rng = new Random();

        public static PriceResult Calculate(MarketSnapshot market, DrinkPricingConfig config)
        { 
            double basePrice = (double)config.BasePrice;
            double currentPrice = (double)market.CurrentPrice;

            // 1. Demand pressure: crowd size relative to a "typical max", centered at 0
            double populationRatio = Math.Clamp(
                (double)market.BarPopulation / Math.Max(1, market.TypicalMaxPopulation), 0, 1);
            double demandPressure = (populationRatio - 0.5) * 2 * config.PopulationSensitivity;

            // 2. Momentum: recent trend continues, dampened by its configured weight
            double momentumComponent = market.RecentMomentum * config.MomentumWeight;

            // 3. Volatility: Gaussian random walk step scaled by measured volatility
            double noise = NextGaussian();
            double volatilityComponent = noise * market.RecentVolatility * config.VolatilityWeight;

            // 4. Mean reversion: pulls price back toward BasePrice, proportional to distance
            double meanReversionComponent = (basePrice - currentPrice) / basePrice * config.MeanReversionSpeed;

            // Combine forces into a fractional change, applied to current price
            double fractionalDelta = demandPressure + momentumComponent + volatilityComponent + meanReversionComponent;
            double candidatePrice = currentPrice * (1 + fractionalDelta);

            // 5. Time-of-day / special-event multiplier (configurable layer)
            double timeMultiplier = GetTimeMultiplier(market.Timestamp, config);
            candidatePrice *= timeMultiplier;

            // 6. Soft max: logistic deceleration as price nears the ceiling
            bool hitSoftMax = candidatePrice > (double)config.SoftMaxPrice * config.SoftCapThresholdRatio;
            double softCapped = ApplySoftMax(candidatePrice, (double)config.SoftMaxPrice, config);

            // 7. Hard min: simple floor clamp, no exceptions
            bool hitHardMin = softCapped < (double)config.HardMinPrice;
            double floored = Math.Max(softCapped, (double)config.HardMinPrice);

            // 8. Circuit breaker: cap how much price can move in a single tick
            double maxChange = currentPrice * (double)config.MaxTickChangePercent;
            bool circuitBreakerTripped = Math.Abs(floored - currentPrice) > maxChange;
            double clamped = circuitBreakerTripped
                ? currentPrice + Math.Sign(floored - currentPrice) * maxChange
                : floored;

            decimal finalPrice = Math.Round((decimal)clamped, 2);

            return new PriceResult
            {
                PreviousPrice = market.CurrentPrice,
                NewPrice = finalPrice,
                DemandPressure = demandPressure,
                MomentumComponent = momentumComponent,
                VolatilityComponent = volatilityComponent,
                MeanReversionComponent = meanReversionComponent,
                TimeMultiplier = timeMultiplier,
                HitSoftMax = hitSoftMax,
                HitHardMin = hitHardMin,
                CircuitBreakerTripped = circuitBreakerTripped
            };
        }

        // Logistic-style compression: below the threshold ratio, price moves freely.
        // Above it, each additional dollar of "excess" buys diminishing returns,
        // asymptotically approaching SoftMaxPrice rather than hitting a hard wall.
        private static double ApplySoftMax(double price, double softMax, DrinkPricingConfig config)
        {
            double threshold = softMax * config.SoftCapThresholdRatio;
            if (price <= threshold) return price;

            double headroom = softMax - threshold;
            double excess = price - threshold;
            double compressed = headroom * (1 - Math.Exp(-config.SoftCapSharpness * excess / headroom));
            return threshold + compressed;
        }

        private static double GetTimeMultiplier(DateTime timestamp, DrinkPricingConfig config)
        {
            double multiplier = config.TimeCurve?.GetMultiplier(timestamp) ?? 1.0;
            foreach (var evt in config.SpecialEvents.Where(e => e.IsActive(timestamp)))
                multiplier *= evt.Multiplier;
            return multiplier;
        }

        // Box-Muller transform: converts uniform random draws into a normal
        // (Gaussian) distribution, the standard way to simulate a stock-like
        // random walk instead of flat, unrealistic uniform jitter.
        private static double NextGaussian()
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }

    // ---------------------------------------------------------------------
    // EXAMPLE USAGE
    // ---------------------------------------------------------------------
    public static class Example
    {
        public static void Run()
        {
            var config = new DrinkPricingConfig
            {
                DrinkName = "Old Fashioned",
                BasePrice = 12.00m,
                HardMinPrice = 8.00m,
                SoftMaxPrice = 22.00m,
                SpecialEvents = new List<SpecialEvent>
                {
                    new SpecialEvent
                    {
                        Name = "New Year's Eve",
                        StartsAt = new DateTime(2026, 12, 31, 20, 0, 0),
                        EndsAt = new DateTime(2027, 1, 1, 1, 0, 0),
                        Multiplier = 1.3
                    }
                }
            };

            var market = new MarketSnapshot
            {
                CurrentPrice = 12.00m,
                BarPopulation = 95,
                TypicalMaxPopulation = 120,
                RecentMomentum = 0.02,
                RecentVolatility = 0.15,
                Timestamp = DateTime.Now
            };

            var result = DrinkPricingEngine.Calculate(market, config);
            Console.WriteLine($"{config.DrinkName}: {result.PreviousPrice:C} -> {result.NewPrice:C}");
        }
    }
}