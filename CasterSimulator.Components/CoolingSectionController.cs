using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Controls and coordinates multiple cooling sections along the strand path.
    /// Monitors strand position and adjusts cooling rates based on strand location and casting speed.
    /// </summary>
    public class CoolingSectionController : IDisposable
    {
        private readonly Subject<(double HeadPosition, double TailPosition, double CastSpeed)> _coolingSubject = new();
        private IDisposable _coolingSubscription;
        private ConcurrentDictionary<int, CoolingSection> _coolingSections;
        private bool _disposed;

        /// <summary>
        /// The base flow rate in liters per second, applied regardless of casting speed.
        /// </summary>
        private double _baseFlowLps { get; set; }

        /// <summary>
        /// The additional flow rate in liters per second to be added per unit of casting speed.
        /// </summary>
        private double _flowPerSpeedLps { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoolingSectionController"/> class.
        /// </summary>
        /// <param name="baseFlowLps">The base flow rate in liters per second.</param>
        /// <param name="flowPerSpeedLps">The additional flow rate per unit of casting speed in liters per second.</param>
        public CoolingSectionController(double baseFlowLps, double flowPerSpeedLps)
        {
            _baseFlowLps = baseFlowLps;
            _flowPerSpeedLps = flowPerSpeedLps;
        }

        /// <summary>
        /// Starts the reactive monitoring of cooling sections.
        /// Sets up a subscription that processes cooling section updates with throttling
        /// to prevent excessive update frequency.
        /// </summary>
        /// <param name="coolingSections">Dictionary of cooling sections to be monitored and controlled.</param>
        public void StartCoolingMonitoring(ConcurrentDictionary<int, CoolingSection> coolingSections)
        {
            _coolingSections = coolingSections;

            _coolingSubscription = _coolingSubject
                .Throttle(TimeSpan.FromMilliseconds(500)) // Reduce rapid updates
                .DistinctUntilChanged()
                .Subscribe(data => UpdateCooling(data.HeadPosition, data.TailPosition, data.CastSpeed));
        }

        /// <summary>
        /// Updates the cooling flow rates for all sections based on strand position.
        /// Activates cooling in sections that contain either the head or tail of the strand,
        /// and deactivates cooling in sections that the strand has completely passed.
        /// </summary>
        /// <param name="headPosition">The position of the strand head in meters from the mold.</param>
        /// <param name="tailPosition">The position of the strand tail in meters from the mold.</param>
        /// <param name="castSpeed">The current casting speed in meters per minute.</param>
        private void UpdateCooling(double headPosition, double tailPosition, double castSpeed)
        {
            foreach (var section in _coolingSections.Values)
            {
                var isHeadInSection = headPosition >= section.StartPosition;
                var isTailStillInSection = tailPosition > 0 && tailPosition < section.EndPosition; // Ignore tail at 0

                if (isHeadInSection || isTailStillInSection)
                {
                    section.CurrentFlowRate = CalculateFlowRate(castSpeed, section.PositionFactor);
                }
                else
                {
                    section.CurrentFlowRate = 0; // Stop cooling only when the tail has fully exited
                }
            }
        }

        /// <summary>
        /// Calculates the flow rate for a cooling section based on casting speed and position factor.
        /// The flow rate consists of a base component plus a speed-dependent component,
        /// adjusted by the section's position-dependent factor.
        /// </summary>
        /// <param name="castSpeed">The current casting speed in meters per minute.</param>
        /// <param name="positionFactor">The position-dependent adjustment factor for the section.</param>
        /// <returns>The calculated flow rate in liters per second.</returns>
        private double CalculateFlowRate(double castSpeed, double positionFactor)
        {
            return (_baseFlowLps + _flowPerSpeedLps * castSpeed) * positionFactor;
        }

        /// <summary>
        /// Signals the controller that the strand has moved to a new position.
        /// This method is called when the strand advances, triggering updates to cooling sections.
        /// </summary>
        /// <param name="headPositionMeters">The position of the strand head in meters from the mold.</param>
        /// <param name="tailPositionMeters">The position of the strand tail in meters from the mold.</param>
        /// <param name="castSpeedMetersPerMin">The current casting speed in meters per minute.</param>
        public void ActivateSections(double headPositionMeters, double tailPositionMeters, double castSpeedMetersPerMin)
        {
            _coolingSubject.OnNext((headPositionMeters, tailPositionMeters, castSpeedMetersPerMin));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _coolingSubscription?.Dispose();
                    _coolingSubject?.Dispose();
                }

                _disposed = true;
            }
        }

        ~CoolingSectionController()
        {
            Dispose(false);
        }
    }
}