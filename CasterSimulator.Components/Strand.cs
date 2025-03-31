using System;
using System.Reactive.Linq;
using System.Reflection.Metadata;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Represents the strand component in the continuous casting machine.
    /// Manages the movement, mode, and continuous advancement of the solidifying steel strand.
    /// Also tracks the position of the strand's head and tail relative to the mold.
    /// </summary>
    public class Strand : IDisposable
    {
        private bool _disposed;
        private IDisposable? _strandMonitorSubscription;
        private readonly SpeedController _speedController;

        /// <summary>
        /// Gets the current operational mode of the strand.
        /// </summary>
        public StrandMode Mode { get; private set; } = StrandMode.Idle;

        /// <summary>
        /// Gets the incremental distance the strand advances in a single step, in meters.
        /// This is derived from the cast speed and update frequency.
        /// </summary>
        public double CastLengthIncrement { get; private set; }

        /// <summary>
        /// Gets the total length of steel cast so far, in meters.
        /// </summary>
        public double TotalCastLengthMeters { get; private set; }

        /// <summary>
        /// Gets the distance from the mold to the tail end of the strand, in meters.
        /// Used to determine when the entire cast is complete.
        /// </summary>
        public double TailFromMoldMeters { get; private set; }

        /// <summary>
        /// Gets or sets the distance from the mold to the leading edge of the strand, in meters.
        /// This is reset when the torch cuts the strand.
        /// </summary>
        public double HeadFromMoldMeters { get; set; }

        /// <summary>
        /// Gets the current casting speed in meters per minute.
        /// </summary>
        public double CastSpeedMetersMin { get; private set; }

        /// <summary>
        /// Event triggered when the strand advances one increment.
        /// Handlers can use this to update related components.
        /// </summary>
        public event EventHandler? Advanced;

        /// <summary>
        /// Initializes a new instance of the <see cref="Strand"/> class.
        /// </summary>
        /// <param name="targetCastSpeed">The target casting speed in meters per minute.</param>
        /// <param name="speedRampDuration">The time in seconds it takes to reach the target speed from zero.</param>
        public Strand(double targetCastSpeed, double speedRampDuration = 90)
        {
            _speedController = new SpeedController(0.1, targetCastSpeed, speedRampDuration);
            CastSpeedMetersMin = 0;
        }

        /// <summary>
        /// Starts the strand movement and sets up periodic monitoring to advance the strand.
        /// Changes the strand mode to Casting.
        /// </summary>
        public void Start()
        {
            Mode = StrandMode.Casting;

            _strandMonitorSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => AdvanceStrand());
        }

        /// <summary>
        /// Sets the operational mode of the strand.
        /// </summary>
        /// <param name="strandMode">The new mode to set for the strand.</param>
        public void SetMode(StrandMode strandMode)
        {
            Mode = strandMode;
        }

        /// <summary>
        /// Advances the strand based on the current cast speed.
        /// Updates position measurements and triggers the Advanced event.
        /// Behavior varies based on the current strand mode.
        /// </summary>
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

        /// <summary>
        /// Stops the strand movement and cancels the monitoring subscription.
        /// Sets the strand mode to Idle and resets the cast speed to zero.
        /// </summary>
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