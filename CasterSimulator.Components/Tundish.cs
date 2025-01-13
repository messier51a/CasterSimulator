using System;

namespace CasterSimulator.Components
{
    public class Tundish
    {
        private readonly string tundishId;
        private readonly double _thresholdWeight;
        private readonly double _maxWeight;
        private double _currentSteelWeight;
        private double _steelWeightAtHeatBoundary;
        private double _tundishMixSteelWeight = 0.0; // Remaining mix steel weight (kg)
        private bool _isMixZoneActive = false; // Tracks if mix zone is active
        private bool _thresholdReached;

        public event EventHandler CastingThresholdReached;
        public event EventHandler TundishEmpty;
        public event EventHandler MixZoneEnded;
        public event EventHandler NextHeatOnStrand;

        public double CurrentSteelWeight => _currentSteelWeight;

        public Tundish(string id, double threshold = 6000.0, double maxWeight = 27000.0)
        {
            tundishId = id ?? throw new ArgumentNullException(nameof(id));
            _thresholdWeight = threshold;
            this._maxWeight = maxWeight;
        }

        public void AddSteel(double weight)
        {
            _currentSteelWeight += weight;

            // Prevent overflow
            if (_currentSteelWeight > _maxWeight)
            {
                _currentSteelWeight = _maxWeight;
            }

            // Trigger casting threshold if reached and not already triggered
            if (!_thresholdReached && _currentSteelWeight >= _thresholdWeight)
            {
                _thresholdReached = true; // Mark as triggered
                CastingThresholdReached?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RemoveSteel(double weight)
        {
            if (_currentSteelWeight <= 0)
            {
                TundishEmpty?.Invoke(this, EventArgs.Empty);
                return;
            }

            _currentSteelWeight = Math.Max(0, _currentSteelWeight - weight);

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

            if (_steelWeightAtHeatBoundary > 0)
            {
                _steelWeightAtHeatBoundary = Math.Max(0, _steelWeightAtHeatBoundary - weight);
                if (_steelWeightAtHeatBoundary==0)
                    NextHeatOnStrand?.Invoke(this, EventArgs.Empty);
            }
        }

        public void MixZoneStart()
        {
            _steelWeightAtHeatBoundary = _currentSteelWeight;
            _tundishMixSteelWeight = _currentSteelWeight * 0.5;
            _isMixZoneActive = true;
        }
    }
}
