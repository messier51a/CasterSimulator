using System;

namespace SteelCastingSimulation
{
    public class LadleSimulator
    {
        private double steelWeight;
        private bool isOpen;
        private double pouredAmount;
        private double pouredThisStep;
        private readonly double highPourRate = 200;
        private readonly double stablePourRate = 100;
        private readonly double closeThreshold = 4000;

        public event EventHandler LadleOpened;
        public event EventHandler LadleClosed;

        public string HeatId { get; }
        public double RemainingSteelWeight => steelWeight;
        public double LastPoured => pouredThisStep;

        public bool IsOpen => isOpen;

        public LadleSimulator(double initialSteelWeight, string heatId)
        {
            steelWeight = initialSteelWeight;
            HeatId = heatId;
        }

        public void OpenLadle()
        {
            if (!isOpen)
            {
                isOpen = true;
                LadleOpened?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CloseLadle()
        {
            if (isOpen)
            {
                isOpen = false;
                LadleClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Update(double deltaTimeSeconds)
        {
            pouredThisStep = 0;
            if (!isOpen)
                return;

            double rate = pouredAmount < 16000 ? highPourRate : stablePourRate;

            double amountPoured = rate * deltaTimeSeconds;
            if (amountPoured > steelWeight)
                amountPoured = steelWeight;

            steelWeight -= amountPoured;
            pouredAmount += amountPoured;
            pouredThisStep = amountPoured;

            // Auto-close if near empty
            if (steelWeight < closeThreshold && isOpen)
                CloseLadle();
        }
    }
}