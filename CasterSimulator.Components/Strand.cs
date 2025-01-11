using System;
using System.Reactive.Linq;

namespace CasterSimulator.Components
{
    public class Strand
    {
        private readonly Mold mold; // Reference to the mold for dimensions
        private double castLength; // Total length of steel cast (in meters)
        private double strandLength; // Length of steel remaining on the machine (in meters)
        private double tailOffset; // Distance from the mold to the tail of the steel being cast
        private readonly double torchPosition = 20.0; // Fixed position of the torch (in meters)
        private readonly double slabLength = 10.0; // Length of each slab after cutting (in meters)
        private IDisposable strandAdvanceSubscription; // Reactive subscription for strand advancement
        private bool isCasting; // Indicates whether casting is ongoing
        private bool _isTailOut; // Indicates whether the strand is in tail-out mode
        private double lastIncrement; // Last length increment advanced

        public double CastLength => castLength; // Expose total cast length
        public double StrandLength => strandLength; // Expose current strand length
        public double TailOffset => tailOffset; // Expose current tail offset
        public double LastIncrement => lastIncrement; // Expose the last length increment

        public event EventHandler StrandUpdated; // Event triggered on strand update
        public event EventHandler SlabCut; // Event triggered when a slab is cut

        public Strand(Mold mold)
        {
            this.mold = mold ?? throw new ArgumentNullException(nameof(mold));
            castLength = 0.0;
            strandLength = 0.0;
            tailOffset = 0.0;
            lastIncrement = 0.0;
            isCasting = false;
            _isTailOut = false;
        }

        // Start the casting process
        public void StartCasting(double castSpeed)
        {
            if (isCasting) return;

            isCasting = true;
            _isTailOut = false;
            castLength = 0.0;
            strandLength = 0.0;
            tailOffset = 0.0;

            strandAdvanceSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1)) // Advance every second
                .Subscribe(_ => AdvanceStrand(castSpeed));
        }

        // Stop the casting process and switch to tail-out mode
        public void TailOut()
        {
            _isTailOut = true;
        }

        // Advance the strand for a given time interval
        private void AdvanceStrand(double castSpeed)
        {
            if (castSpeed <= 0)
                return;

            double deltaTimeSeconds = 1.0; // Interval of 1 second
            lastIncrement = castSpeed * deltaTimeSeconds;

            if (_isTailOut)
            {
                // Increment tail offset only in tail-out mode
                tailOffset += lastIncrement;
            }
            else if (isCasting)
            {
                // Increment cast length and strand length during casting
                castLength += lastIncrement;
                strandLength += lastIncrement;

                // Perform a cut if the strand length exceeds the torch position + slab length
                if (strandLength >= torchPosition + slabLength)
                {
                    PerformSlabCut();
                }
            }

            // Trigger strand updated event
            StrandUpdated?.Invoke(this, EventArgs.Empty);
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
