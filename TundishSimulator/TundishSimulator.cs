using System;

namespace SteelCastingSimulation
{
    public class TundishSimulator
    {
        private double tundishWeight;
        private readonly double threshold = 24000; // 12 tons in lbs
        private bool hasFiredStart;

        public event EventHandler CastingStart;
        public event EventHandler CastingEnd;

        public double CurrentSteelWeight => tundishWeight;

        public void AddSteel(double amount)
        {
            tundishWeight += amount;
            if (!hasFiredStart && tundishWeight >= threshold)
            {
                hasFiredStart = true;
                CastingStart?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Drain(double deltaTimeSeconds, double drainRateLbsPerSecond)
        {
            double beforeDrain = tundishWeight;
            double drainAmount = drainRateLbsPerSecond * deltaTimeSeconds;
            if (drainAmount > tundishWeight)
                drainAmount = tundishWeight;

            tundishWeight -= drainAmount;

            if (beforeDrain > 0 && tundishWeight <= 0)
            {
                tundishWeight = 0;
                CastingEnd?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}