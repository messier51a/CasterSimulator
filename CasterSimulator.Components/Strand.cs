using System;
using System.Reactive.Linq;

namespace CasterSimulator.Components
{
    public class Strand
    {
        private double _castLength; // Total length of steel cast (in meters)
        private double _strandLength; // Length of steel remaining on the machine (in meters)
        private double _tailOffset; // Distance from the mold to the tail of the steel being cast
        private const double TorchPosition = 20.0; // Fixed position of the torch (in meters)
        private const double SlabLength = 10.0; // Length of each slab after cutting (in meters)
        private IDisposable _strandAdvanceSubscription; // Reactive subscription for strand advancement
        private bool _isCasting; // Indicates whether casting is ongoing
        private bool _isTailOut; // Indicates whether the strand is in tail-out mode
        private double _currentCastSpeed; // Current casting speed in m/s
        private double _rampTargetSpeed; // Target speed for ramp-up
        private double _rampDuration; // Duration for speed ramp-up
        private double _rampElapsedTime; // Elapsed time for ramp-up
        private double lastIncrement; // Last length increment advanced

        public double CastSpeed => _currentCastSpeed * 60;
        public double CastLength => _castLength; // Expose total cast length
        public double StrandLength => _strandLength; // Expose current strand length
        public double TailOffset => _tailOffset; // Expose current tail offset
        public double LastIncrement => lastIncrement; // Expose the last length increment

        public event EventHandler StrandUpdated; // Event triggered on strand update
        public event EventHandler SlabCut; // Event triggered when a slab is cut

        public Strand(Mold mold)
        {
            _castLength = 0.0;
            _strandLength = 0.0;
            _tailOffset = 0.0;
            lastIncrement = 0.0;
            _isCasting = false;
            _isTailOut = false;
        }

        // Start the casting process with optional speed ramp-up
        public void StartCasting(double initialSpeed, double targetSpeed = 0.0, double duration = 0.0)
        {
            if (_isCasting) return;

            _isCasting = true;
            _isTailOut = false;
            _castLength = 0.0;
            _strandLength = 0.0;
            _tailOffset = 0.0;
            _currentCastSpeed = initialSpeed;
            _rampTargetSpeed = targetSpeed;
            _rampDuration = duration;
            _rampElapsedTime = 0.0;

            _strandAdvanceSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1)) // Advance every second
                .Subscribe(_ => AdvanceStrand());
        }

        // Stop the casting process and switch to tail-out mode
        public void TailOut()
        {
            _isTailOut = true;
        }

        // Advance the strand for a given time interval
        private void AdvanceStrand()
        {
            if (_currentCastSpeed <= 0)
                return;

            double deltaTimeSeconds = 1.0; // Interval of 1 second

            // Handle speed ramp-up if applicable
            if (_rampElapsedTime < _rampDuration)
            {
                _rampElapsedTime += deltaTimeSeconds;
                double rampProgress = Math.Min(_rampElapsedTime / _rampDuration, 1.0);
                _currentCastSpeed = (1 - rampProgress) * _rampTargetSpeed + rampProgress * _rampTargetSpeed;
            }


            lastIncrement = _currentCastSpeed * deltaTimeSeconds;

            if (_isTailOut)
            {
                // Increment tail offset only in tail-out mode
                _tailOffset += lastIncrement;
            }
            else if (_isCasting)
            {
                // Increment cast length and strand length during casting
                _castLength += lastIncrement;
                _strandLength += lastIncrement;

                // Perform a cut if the strand length exceeds the torch position + slab length
                if (_strandLength >= TorchPosition + SlabLength)
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
            _strandLength = TorchPosition;

            // Trigger the SlabCut event
            SlabCut?.Invoke(this, EventArgs.Empty);
        }

        // Stop all strand activity
        public void StopCasting()
        {
            _isCasting = false;
            _strandAdvanceSubscription?.Dispose();
        }
    }
}
