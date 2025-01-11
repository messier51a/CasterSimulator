using System;

namespace CasterSimulator.Components
{
    public class Tundish
    {
        private readonly string tundishId; // Unique identifier for the tundish
        private readonly double thresholdWeight = 12000.0; // Threshold weight to trigger CastingStart (lbs)
        private readonly double minimumWeight = 2000.0; // Minimum weight to trigger CastingEnd (lbs)
        private double currentSteelWeight; // Current weight of steel in the tundish (lbs)
        private bool castingStarted; // Flag indicating if casting has started
        private bool castingEnded; // Flag indicating if casting has ended

        public string TundishId => tundishId; // Expose tundish ID
        public double CurrentSteelWeight => currentSteelWeight; // Expose current weight

        // Events for casting milestones
        public event EventHandler CastingStart;
        public event EventHandler CastingEnd;

        public Tundish(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tundish ID must be a valid string.", nameof(id));

            tundishId = id;
            currentSteelWeight = 0.0;
            castingStarted = false;
            castingEnded = false;
        }

        // Adds steel to the tundish
        public void AddSteel(double amount)
        {
            if (amount <= 0)
                return;

            currentSteelWeight += amount;

            // Trigger CastingStart if the weight exceeds the threshold
            if (!castingStarted && currentSteelWeight >= thresholdWeight)
            {
                castingStarted = true;
                CastingStart?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"Tundish {tundishId}: Casting started. Current weight: {currentSteelWeight:F2} lbs");
            }
        }

        // Drains steel from the tundish
        public void Drain(double deltaTimeSeconds, double drainRate)
        {
            if (deltaTimeSeconds <= 0 || drainRate <= 0)
                return;

            double drainedAmount = drainRate * deltaTimeSeconds;

            // Ensure we don't drain more than the current weight
            if (drainedAmount > currentSteelWeight)
                drainedAmount = currentSteelWeight;

            currentSteelWeight -= drainedAmount;

            // Trigger CastingEnd if the weight falls below the minimum
            if (!castingEnded && currentSteelWeight <= minimumWeight)
            {
                castingEnded = true;
                CastingEnd?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"Tundish {tundishId}: Casting ended. Current weight: {currentSteelWeight:F2} lbs");
            }
        }
    }
}
