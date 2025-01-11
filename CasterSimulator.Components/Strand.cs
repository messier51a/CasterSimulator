using System;

namespace CasterSimulator.Components
{
    public class Strand
    {
        private readonly Mold mold; // Reference to the mold for dimensions
        private double castLength; // Total length of steel cast (in meters)
        private double strandLength; // Length of steel remaining on the machine (in meters)
        private readonly double torchPosition = 20.0; // Fixed position of the torch (in meters)
        private readonly double slabLength = 10.0; // Length of each slab after cutting (in meters)
        private bool isCasting; // Indicates whether casting is ongoing

        public double CastLength => castLength; // Expose total cast length
        public double StrandLength => strandLength; // Expose current strand length

        public event EventHandler SlabCut; // Event triggered when a slab is cut

        public Strand(Mold mold)
        {
            this.mold = mold ?? throw new ArgumentNullException(nameof(mold));
            castLength = 0.0;
            strandLength = 0.0;
            isCasting = false;
        }

        // Start the casting process
        public void StartCasting()
        {
            isCasting = true;
            castLength = 0.0;
            strandLength = 0.0;
        }

        // Stop the casting process
        public void StopCasting()
        {
            isCasting = false;
        }

        // Update the strand's state for a given time interval
        public void Update(double deltaTimeSeconds, double castSpeed)
        {
            if (!isCasting || castSpeed <= 0 || deltaTimeSeconds <= 0)
                return;

            // Increment cast length and strand length
            double increment = castSpeed * deltaTimeSeconds;
            castLength += increment;
            strandLength += increment;

            // Perform a cut if the strand length exceeds the torch position + slab length
            if (strandLength >= torchPosition + slabLength)
            {
                PerformSlabCut();
            }
        }

        private void PerformSlabCut()
        {
            // Reset the strand length to the torch position
            strandLength = torchPosition;

            // Trigger the SlabCut event
            SlabCut?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"Slab cut performed. Cast Length: {castLength:F2} meters, Strand Length reset to {strandLength:F2} meters.");
        }
    }
}
