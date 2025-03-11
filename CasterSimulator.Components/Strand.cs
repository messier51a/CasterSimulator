using System;
using System.Reactive.Linq;
using System.Reflection.Metadata;

namespace CasterSimulator.Components
{
    public class Strand : IDisposable
    {
        private bool _disposed;
        private IDisposable? _strandMonitorSubscription;
        private readonly SpeedController _speedController;
        public StrandMode Mode { get; private set; } = StrandMode.Idle;
        public double CastLengthIncrement { get; private set; }
        public double TotalCastLengthMeters { get; private set; }
        public double TailFromMoldMeters { get; private set; }
        public double HeadFromMoldMeters { get; set; }
        public double CastSpeedMetersMin { get; private set; }

        public event EventHandler? Advanced; // Event triggered when the strand advances
        
        public Strand(double targetCastSpeed, double speedRampDuration = 90)
        {
            _speedController  = new SpeedController(0.1, targetCastSpeed, speedRampDuration);
            CastSpeedMetersMin = 0;
        }
        public void Start()
        {
            Mode = StrandMode.Casting;

            _strandMonitorSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => AdvanceStrand());
        }
        
        public void SetMode(StrandMode strandMode)
        {
            Mode = strandMode;
        }
        private void AdvanceStrand()
        {
            CastSpeedMetersMin = _speedController.CalculateCurrentSpeed(); 
            CastLengthIncrement = CastSpeedMetersMin / 60.0;
            HeadFromMoldMeters += CastLengthIncrement;

            switch (Mode)
            {
                case StrandMode.Tailout:
                    TailFromMoldMeters += CastLengthIncrement;
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
            Mode = StrandMode.Idle;
            _strandMonitorSubscription?.Dispose();
            _strandMonitorSubscription = null;
            CastSpeedMetersMin = 0;
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