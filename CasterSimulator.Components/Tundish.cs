using System;

namespace CasterSimulator.Components
{
    public class Tundish
    {
        private readonly string tundishId;
        private readonly double thresholdWeight;
        private double currentSteelWeight;

        public event EventHandler CastingThresholdReached;
        public event EventHandler TundishEmpty;

        public double CurrentSteelWeight => currentSteelWeight;

        public Tundish(string id, double threshold = 12000.0)
        {
            tundishId = id ?? throw new ArgumentNullException(nameof(id));
            thresholdWeight = threshold;
        }

        public void AddSteel(double weight)
        {
            currentSteelWeight += weight;
            if (currentSteelWeight >= thresholdWeight)
            {
                CastingThresholdReached?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RemoveSteel(double weight)
        {
            if (currentSteelWeight <= 0)
            {
                TundishEmpty?.Invoke(this, EventArgs.Empty);
                return;
            }

            currentSteelWeight = Math.Max(0, currentSteelWeight - weight);
        }
    }
}