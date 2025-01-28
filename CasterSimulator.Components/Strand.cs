using System;
using System.Reactive.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Strand : IDisposable
    {
        private readonly Mold _mold;
        private readonly Torch _torch;
        private double _lastIncrement;
        private double _massFlow;
        private IDisposable? _strandMonitorSubscription;
        private StrandMode _strandMode;
        private readonly double _intervalMilliseconds;
        private readonly SpeedControl _speedControl;
        public double SteelLeftOnStrand => Math.Max(HeadDistanceFromMold - TailDistanceFromMold, 0);
        public StrandMode Mode => _strandMode;
        public double TotalCastLength { get; private set; }

        public double HeadDistanceFromMold { get; private set; }

        public double TailDistanceFromMold { get; private set; }

        public double CastSpeed { get; private set; }

        public event EventHandler<double>? StrandAdvanced; // Event triggered when the strand advances
        public event EventHandler? CastingFinished; // Event triggered when casting is fully completed

        public Strand(Mold mold, Torch torch, SpeedControl speedControl, double intervalMilliseconds = 1000)
        {
            _mold = mold ?? throw new ArgumentNullException(nameof(mold));
            _torch = torch ?? throw new ArgumentNullException(nameof(torch));
            _speedControl = speedControl ?? throw new ArgumentNullException(nameof(speedControl));
            _intervalMilliseconds = intervalMilliseconds;
            CastSpeed = 0;

            _torch.CutDone += (s, product) => { HeadDistanceFromMold = _torch.TorchPosition; };
        }

        public void StartCasting()
        {
            _strandMode = StrandMode.Casting;

            _strandMonitorSubscription = Observable
                .Interval(TimeSpan.FromMilliseconds(_intervalMilliseconds))
                .Subscribe(_ => AdvanceStrand(_intervalMilliseconds));
        }
        
        public void TailOut()
        {
            _strandMode = StrandMode.Tailout;
        }
        private void AdvanceStrand(double deltaTimeMilliseconds)
        {
            CastSpeed = _speedControl.CalculateCurrentSpeed(deltaTimeMilliseconds); 
            var currentCastSpeedMetersPerSecond = CastSpeed / 60.0;
            _lastIncrement = currentCastSpeedMetersPerSecond * (deltaTimeMilliseconds / 1000.0);
            HeadDistanceFromMold += _lastIncrement;

            switch (_strandMode)
            {
                case StrandMode.Tailout:
                    TailDistanceFromMold += _lastIncrement;
                    // Trigger CastingFinished when tail crosses the torch position
                    if (TailDistanceFromMold > _torch.TorchPosition)
                    {
                        StopCasting();
                        CastingFinished?.Invoke(this, EventArgs.Empty);
                    }

                    break;

                case StrandMode.Casting:
                    TotalCastLength += _lastIncrement;
                    break;

                case StrandMode.Idle:
                case StrandMode.DummyBarInsert:
                case StrandMode.ReadyToCast:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _torch.Update(HeadDistanceFromMold);

            // Calculate mass flow
            var crossSectionalArea = _mold.GetCrossSectionalArea(); // m²
            var steelDensity = 7850; // kg/m³
            _massFlow = crossSectionalArea * _lastIncrement * steelDensity; // kg

            // Notify listeners
            StrandAdvanced?.Invoke(this, _massFlow);
        }

        private void StopCasting()
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