using System;

namespace SteelCastingSimulation
{
    public class LadleSimulator
    {
        private double _initialSteelWeight;
        private double _steelWeight;
        private bool _isOpen;
        private double _pouredAmount;
        private double _pouredThisStep;
        private const double HighPourRate = 200;
        private const double StablePourRate = 100;
        private const double CloseThreshold = 4000;

        public event EventHandler LadleOpened;
        public event EventHandler LadleClosed;

        public string HeatId { get; }
        public double RemainingSteelWeight => _steelWeight;
        public double LastPoured => _pouredThisStep;

        public double InitialWeight => _initialSteelWeight;

        public bool IsOpen => _isOpen;

        public LadleSimulator(double initialSteelWeight, string heatId)
        {
            _initialSteelWeight = initialSteelWeight;
            _steelWeight = initialSteelWeight;
            HeatId = heatId;
        }

        public void OpenLadle()
        {
            if (!_isOpen)
            {
                _isOpen = true;
                LadleOpened?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CloseLadle()
        {
            if (_isOpen)
            {
                _isOpen = false;
                LadleClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Update(double deltaTimeSeconds)
        {
            _pouredThisStep = 0;
            if (!_isOpen)
                return;

            double rate = _pouredAmount < 16000 ? HighPourRate : StablePourRate;

            double amountPoured = rate * deltaTimeSeconds;
            if (amountPoured > _steelWeight)
                amountPoured = _steelWeight;

            _steelWeight -= amountPoured;
            _pouredAmount += amountPoured;
            _pouredThisStep = amountPoured;

            // Auto-close if near empty
            if (_steelWeight < CloseThreshold && _isOpen)
                CloseLadle();
        }
    }
}