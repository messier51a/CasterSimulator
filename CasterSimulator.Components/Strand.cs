using System;
using System.Reactive.Linq;
using System.Reflection.Metadata;
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
        public double TotalCastLengthMeters { get; private set; }
        public double TailDistanceFromMold { get; private set; }
        public double HeadFromMoldMeters { get; set; }
        public double CastSpeedMetersMin { get; private set; }
        public event EventHandler? Advanced; // Event triggered when the strand advances
        
        public Strand(double targetCastSpeed, double speedRampDuration = 90)
        {
            _speedControl  = new SpeedControl(0.1, targetCastSpeed, speedRampDuration);
            CastSpeedMetersMin = 0;
        }
        public void Start()
        {
            _strandMode = StrandMode.Casting;

            _strandMonitorSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => AdvanceStrand());
        }
        
        public void SetMode(StrandMode strandMode)
        {
            _strandMode = strandMode;
        }
        private void AdvanceStrand()
        {
            CastSpeedMetersMin = _speedControl.CalculateCurrentSpeed(); 
            CastLengthIncrement = CastSpeedMetersMin / 60.0;
            HeadFromMoldMeters += CastLengthIncrement;

            switch (_strandMode)
            {
                case StrandMode.Tailout:
                    TailDistanceFromMold += CastLengthIncrement;
                    break;

                case StrandMode.Casting:
                    TotalCastLengthMeters += CastLengthIncrement;
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