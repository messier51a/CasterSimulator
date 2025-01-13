using System;
using System.Reactive.Linq;

namespace CasterSimulator.Components
{
    public class Strand : IDisposable
    {
        private bool _disposed = false; 
        private Mold _mold;
        private Torch _torch;
        private double _massFlow;
        private double _castLength; // Total length of steel cast (in meters)
        private double _strandLength; // Length of steel remaining on the machine (in meters)
        private double _tailOffset; // Distance from the mold to the tail of the steel being cast
        private const double TorchPosition = 20.0; // Fixed position of the torch (in meters)
        private const double SlabLength = 10.0; // Length of each slab after cutting (in meters)
        private IDisposable _strandAdvanceSubscription = null!; // Reactive subscription for strand advancement
        private double _initialCastSpeed;
        private double _currentCastSpeed; // Current casting speed in m/s
        private double _rampTargetSpeed; // Target speed for ramp-up
        private double _rampDuration; // Duration for speed ramp-up
        private double _rampElapsedTime; // Elapsed time for ramp-up
        private double _lastIncrement; // Last length increment advanced
        public StrandMode _strandMode;

        public double CastSpeed => _currentCastSpeed * 60;
        public double CastLength => _castLength; // Expose total cast length
        public double StrandLength => _strandLength; // Expose current strand length
        public double TailOffset => _tailOffset; // Expose current tail offset
        public double LastIncrement => _lastIncrement; // Expose the last length increment

        public StrandMode Mode => _strandMode;

        public event Action<object, double> StrandAdvanced = null!;
        public event EventHandler SlabCut = null!; // Event triggered when a slab is cut

        public Strand(Mold mold, Torch torch)
        {
            ArgumentNullException.ThrowIfNull(mold);
            ArgumentNullException.ThrowIfNull(torch);

            _mold = mold;
            _torch = torch;
            _castLength = 0.0;
            _strandLength = 0.0;
            _tailOffset = 0.0;
            _lastIncrement = 0.0;
        }

        // Start the casting process with optional speed ramp-up
        public void StartCasting(double initialSpeed, double targetSpeed = 0.0, double duration = 0.0,
            double simulationIntervalSeconds = 1.0)
        {
            if (_strandMode == StrandMode.Casting) return;

            _strandMode = StrandMode.Casting;
            _initialCastSpeed = initialSpeed;
            _rampTargetSpeed = targetSpeed;
            _rampDuration = duration;
            _rampElapsedTime = 0.0;

            _strandAdvanceSubscription = Observable
                .Interval(TimeSpan.FromMilliseconds(200)) // Faster simulation (5 ticks per second)
                .Subscribe(_ => AdvanceStrand(1.0)); // 1 simulated second per tick
        }

        // Stop the casting process and switch to tail-out mode
        public void TailOut()
        {
            _strandMode = StrandMode.Tailout;
        }

        // Advance the strand for a given time interval
        private void AdvanceStrand(double deltaTimeSeconds)
        {
            // Handle speed ramp-up if applicable
            if (_rampElapsedTime < _rampDuration)
            {
                _rampElapsedTime += deltaTimeSeconds;
                double rampProgress = Math.Min(_rampElapsedTime / _rampDuration, 1.0);
                _currentCastSpeed = rampProgress * _rampTargetSpeed; // Interpolate from 0 to target speed
            }

            _lastIncrement = _currentCastSpeed * deltaTimeSeconds;

            switch (_strandMode)
            {
                case StrandMode.Tailout:
                    // Increment tail offset only in tail-out mode
                    _tailOffset += _lastIncrement;
                    break;
                case StrandMode.Casting:
                {
                    // Increment cast length and strand length during casting
                    _castLength += _lastIncrement;
                    _strandLength += _lastIncrement;

                    // Perform a cut if the strand length exceeds the torch position + slab length
                    if (_strandLength >= TorchPosition + SlabLength)
                    {
                        PerformSlabCut();
                    }

                    break;
                }
                case StrandMode.Idle:
                case StrandMode.DummyBarInsert:
                case StrandMode.ReadyToCast:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Calculate mass flow based on strand's length increment
            double crossSectionalArea = _mold.GetCrossSectionalArea(); // m²
            double steelDensity = 7850; // kg/m³
            _massFlow = crossSectionalArea * _lastIncrement * steelDensity; // kg

            // Trigger strand updated event
            StrandAdvanced?.Invoke(this, _massFlow);
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
            _strandMode = StrandMode.Idle;
            _strandAdvanceSubscription?.Dispose();
        }
        
        public void Dispose()
        {
            Dispose(true); // Explicit disposal
            GC.SuppressFinalize(this); // Suppress finalization
        }
        
        // Protected Dispose method for actual cleanup
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                   
                }

                _disposed = true;
            }
        }
        
        ~Strand()
        {
            Dispose(false);
        }
    }

    public enum StrandMode
    {
        Idle,
        DummyBarInsert,
        ReadyToCast,
        Casting,
        Tailout,
    }
}