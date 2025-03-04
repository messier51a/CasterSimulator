using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class CoolingSectionController : IDisposable
    {
        private readonly Subject<(double HeadPosition, double TailPosition, double CastSpeed)> _coolingSubject = new();
        private IDisposable _coolingSubscription;
        private ConcurrentDictionary<int, CoolingSection> _coolingSections;
        private bool _disposed;
        private double _baseFlowLps { get; set; }
        private double _flowPerSpeedLps { get; set; }

        public CoolingSectionController(double baseFlowLps, double flowPerSpeedLps)
        {
            _baseFlowLps = baseFlowLps;
            _flowPerSpeedLps = flowPerSpeedLps;
        }
        public void StartCoolingMonitoring(ConcurrentDictionary<int, CoolingSection> coolingSections)
        {
            _coolingSections = coolingSections;

            _coolingSubscription = _coolingSubject
                .Throttle(TimeSpan.FromMilliseconds(500)) // Reduce rapid updates
                .DistinctUntilChanged()
                .Subscribe(data => UpdateCooling(data.HeadPosition, data.TailPosition, data.CastSpeed));
        }
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
        private double CalculateFlowRate(double castSpeed, double positionFactor)
        {
            return (_baseFlowLps + _flowPerSpeedLps * castSpeed) * positionFactor;
        }
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