using System;

namespace SteelCastingSimulation
{
    public class StrandSimulator
    {
        private readonly double torchPosition; // Location of the torch machine in meters
        private double castLength; // Total length of steel cast
        private double strandLength; // Length of steel on the machine
        private bool isCasting; // Flag indicating if casting is ongoing

        public double CastLength => castLength; // Exposed property for total cast length
        public double StrandLength => strandLength; // Exposed property for current strand length

        public event EventHandler SlabCut; // Event triggered when a slab is cut

        public StrandSimulator(double torchPosition = 20.0)
        {
            this.torchPosition = torchPosition;
            castLength = 0.0;
            strandLength = 0.0;
            isCasting = true;
        }

        public void StartCasting()
        {
            isCasting = true;
            castLength = 0.0;
            strandLength = 0.0;
        }

        public void StopCasting()
        {
            isCasting = false;
        }

        // Update the strand logic based on cast speed and elapsed time
        public void Update(double deltaTimeSeconds, double castSpeed)
        {
            if (!isCasting || castSpeed <= 0)
                return;

            // Increment cast length and strand length
            double increment = castSpeed * deltaTimeSeconds;
            castLength += increment;
            strandLength += increment;

            // Check if a cut should be performed
            if (strandLength > torchPosition + 10.0) // Torch position + slab length
            {
                PerformCut();
            }
        }

        private void PerformCut()
        {
            // Reset the strand length to the torch position
            strandLength = torchPosition;

            // Trigger the SlabCut event
            SlabCut?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"Slab cut performed. Cast Length: {castLength:F2} meters, Strand Length reset to {strandLength:F2} meters.");
        }
    }
}
