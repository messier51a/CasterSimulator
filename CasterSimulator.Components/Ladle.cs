using System;

namespace CasterSimulator.Components
{
    public class Ladle
    {
        private readonly string heatId; // Unique identifier for the ladle (e.g., heat sequence)
        private double remainingSteelWeight; // Remaining steel in the ladle (in lbs)
        private readonly double initialSteelWeight; // Initial steel weight for reference (in lbs)
        private bool isOpen; // Indicates whether the ladle is currently pouring
        private readonly double highPourRate = 2000; // High pouring rate during the initial phase (lbs/sec)
        private readonly double lowPourRate = 800;  // Lower pouring rate after the first phase (lbs/sec)
        private double pouredSteelSinceOpen; // Tracks steel poured during the current open state

        public string HeatId => heatId;
        public double RemainingSteelWeight => remainingSteelWeight;
        public double InitialSteelWeight => initialSteelWeight;
        public bool IsOpen => isOpen;
        public double LastPoured { get; private set; } // Amount poured in the last update (in lbs)

        // Events for ladle operations
        public event EventHandler LadleOpened;
        public event EventHandler LadleClosed;

        public Ladle(double initialWeight, string id)
        {
            if (initialWeight <= 0)
                throw new ArgumentException("Initial weight must be greater than zero.", nameof(initialWeight));

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Heat ID must be a valid string.", nameof(id));

            initialSteelWeight = initialWeight;
            remainingSteelWeight = initialWeight;
            heatId = id;
            isOpen = false;
            pouredSteelSinceOpen = 0;
            LastPoured = 0;
        }

        // Opens the ladle for pouring
        public void OpenLadle()
        {
            if (isOpen)
                return;

            isOpen = true;
            pouredSteelSinceOpen = 0;
            LastPoured = 0;
            LadleOpened?.Invoke(this, EventArgs.Empty);
        }

        // Closes the ladle to stop pouring
        public void CloseLadle()
        {
            if (!isOpen)
                return;

            isOpen = false;
            LastPoured = 0;
            LadleClosed?.Invoke(this, EventArgs.Empty);
        }

        // Updates the ladle's state for a given time interval
        public void Update(double deltaTimeSeconds)
        {
            if (!isOpen || deltaTimeSeconds <= 0)
            {
                LastPoured = 0;
                return;
            }

            // Determine pouring rate
            double pourRate = pouredSteelSinceOpen < 16000 ? highPourRate : lowPourRate;

            // Calculate the amount of steel poured
            LastPoured = Math.Min(pourRate * deltaTimeSeconds, remainingSteelWeight);
            remainingSteelWeight -= LastPoured;
            pouredSteelSinceOpen += LastPoured;

            // Automatically close the ladle when nearly empty
            if (remainingSteelWeight <= 2000)
            {
                CloseLadle();
            }
        }
    }
}
