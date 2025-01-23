using System;
using System.Reactive.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Strand : IDisposable
    {
        private readonly Mold _mold;
        private readonly Torch _torch;
        private double _totalCastLength;
        private double _headDistanceFromMold;
        private double _tailDistanceFromMold;
        private double _currentCastSpeed;
        private double _lastIncrement;
        private double _massFlow;
        private IDisposable? _strandMonitorSubscription;
        private StrandMode _strandMode;
        private readonly double _intervalSeconds;
        private readonly SpeedControl _speedControl;
        public double SteelLeftOnStrand => Math.Max(_headDistanceFromMold - _tailDistanceFromMold, 0);
        public StrandMode Mode => _strandMode;
        public double TotalCastLength => _totalCastLength; // Total cast length in meters
        public double HeadDistanceFromMold => _headDistanceFromMold; // Current strand length in meters
        public double TailDistanceFromMold => _tailDistanceFromMold; // Distance from mold to tail in meters
        public double CastSpeed => _currentCastSpeed; // Cast speed in meters per minute
        public event EventHandler<double> StrandAdvanced; // Event triggered when the strand advances
        public event EventHandler CastingFinished; // Event triggered when casting is fully completed

        public Strand(Mold mold, Torch torch, SpeedControl speedControl)
        {
            _mold = mold ?? throw new ArgumentNullException(nameof(mold));
            _torch = torch ?? throw new ArgumentNullException(nameof(torch));
            _speedControl = speedControl ?? throw new ArgumentNullException(nameof(speedControl));
            _intervalSeconds = 1;
            _currentCastSpeed = 0;

            _torch.CutDone += (s, product) => { _headDistanceFromMold = _torch.TorchPosition; };
        }

        public void StartCasting()
        {
            _strandMode = StrandMode.Casting;

            _strandMonitorSubscription = Observable
                .Interval(TimeSpan.FromSeconds(_intervalSeconds))
                .Subscribe(_ => AdvanceStrand(_intervalSeconds));
        }
        
        public void TailOut()
        {
            _strandMode = StrandMode.Tailout;
        }
        private void AdvanceStrand(double deltaTimeSeconds)
        {
            _currentCastSpeed = _speedControl.CalculateCurrentSpeed(deltaTimeSeconds); 
            var currentCastSpeedMetersPerSecond = _currentCastSpeed / 60.0;
            _lastIncrement = currentCastSpeedMetersPerSecond * deltaTimeSeconds;
            _headDistanceFromMold += _lastIncrement;

            switch (_strandMode)
            {
                case StrandMode.Tailout:
                    _tailDistanceFromMold += _lastIncrement;
                    // Trigger CastingFinished when tail crosses the torch position
                    if (_tailDistanceFromMold > _torch.TorchPosition)
                    {
                        StopCasting();
                        CastingFinished?.Invoke(this, EventArgs.Empty);
                    }

                    break;

                case StrandMode.Casting:
                    _totalCastLength += _lastIncrement;
                    break;

                case StrandMode.Idle:
                case StrandMode.DummyBarInsert:
                case StrandMode.ReadyToCast:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _torch.Update(_headDistanceFromMold);

            // Calculate mass flow
            var crossSectionalArea = _mold.GetCrossSectionalArea(); // m²
            var steelDensity = 7850; // kg/m³
            _massFlow = crossSectionalArea * _lastIncrement * steelDensity; // kg

            // Notify listeners
            StrandAdvanced?.Invoke(this, _massFlow);
        }

        public void StopCasting()
        {
            _strandMode = StrandMode.Idle;
            _strandMonitorSubscription?.Dispose();
            _strandMonitorSubscription = null;
        }

        public void Dispose()
        {
            _strandMonitorSubscription?.Dispose();
        }
    }

    public enum StrandMode
    {
        Idle,
        DummyBarInsert,
        ReadyToCast,
        Casting,
        Tailout
    }
}