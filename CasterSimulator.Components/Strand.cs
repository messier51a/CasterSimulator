using System;
using System.Reactive.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Strand : IDisposable
    {
        private bool _disposed;
        private IDisposable? _strandMonitorSubscription;
        private StrandMode _strandMode = StrandMode.Idle;
        private readonly SpeedControl _speedControl;

        public StrandMode Mode => _strandMode;

        public double CastLengthIncrement { get; private set; }
        public double TotalCastLength { get; private set; }
        public double TailDistanceFromMold { get; private set; }
        public double HeadDistanceFromMold { get; private set; }
        public double CastSpeed { get; private set; }
        public event EventHandler? Advanced; // Event triggered when the strand advances
        

        public Strand(double targetCastSpeed, double speedRampDuration = 90)
        {
            _speedControl  = new SpeedControl(0.1, targetCastSpeed, speedRampDuration);
            CastSpeed = 0;
        }
        public void Start()
        {
            _strandMode = StrandMode.Casting;

            _strandMonitorSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => AdvanceStrand());
        }
        
        public void TailOut()
        {
            _strandMode = StrandMode.Tailout;
        }
        private void AdvanceStrand()
        {
            CastSpeed = _speedControl.CalculateCurrentSpeed(); 
            CastLengthIncrement = CastSpeed / 60.0;
            HeadDistanceFromMold += CastLengthIncrement;

            switch (_strandMode)
            {
                case StrandMode.Tailout:
                    TailDistanceFromMold += CastLengthIncrement;
                    break;

                case StrandMode.Casting:
                    TotalCastLength += CastLengthIncrement;
                    break;

                case StrandMode.Idle:
                case StrandMode.DummyBarInsert:
                case StrandMode.ReadyToCast:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Advanced?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            _strandMode = StrandMode.Idle;
            _strandMonitorSubscription?.Dispose();
            _strandMonitorSubscription = null;
        }

        public void Dispose()
        {
            Dispose(true); 
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
               _strandMonitorSubscription?.Dispose();
            }

            _disposed = true;
        }

        ~Strand()
        {
            Dispose(false);
        }
    }

   
}