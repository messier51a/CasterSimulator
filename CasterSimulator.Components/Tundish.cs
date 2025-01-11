using System;

namespace CasterSimulator.Components
{
    public class Tundish
    {
        private readonly string tundishId;
        private readonly double thresholdWeight;
        private readonly double maxWeight;
        private double currentSteelWeight;
        private double _tundishMixSteelWeight = 0.0; // Remaining mix steel weight (kg)
        private bool _isMixZoneActive = false; // Tracks if mix zone is active
        private bool _thresholdReached;

        public event EventHandler CastingThresholdReached;
        public event EventHandler TundishEmpty;
        public event EventHandler MixZoneEnded;

        public double CurrentSteelWeight => currentSteelWeight;

        public Tundish(string id, double threshold = 6000.0, double maxWeight = 27000.0)
        {
            tundishId = id ?? throw new ArgumentNullException(nameof(id));
            thresholdWeight = threshold;
            this.maxWeight = maxWeight;
        }

        public void AddSteel(double weight)
        {
            currentSteelWeight += weight;

            // Prevent overflow
            if (currentSteelWeight > maxWeight)
            {
                currentSteelWeight = maxWeight;
            }

            // Trigger casting threshold if reached and not already triggered
            if (!_thresholdReached && currentSteelWeight >= thresholdWeight)
            {
                _thresholdReached = true; // Mark as triggered
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

            // Decrement mix zone steel weight if active
            if (_isMixZoneActive)
            {
                _tundishMixSteelWeight -= weight;

                if (_tundishMixSteelWeight <= 0)
                {
                    _isMixZoneActive = false;
                    MixZoneEnded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void StartMixZone()
        {
            _tundishMixSteelWeight = currentSteelWeight * 0.5;
            _isMixZoneActive = true;
        }
    }
}
