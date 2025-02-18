using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Represents the configuration details of a steel container.
    /// </summary>
    public class ContainerDetails
    {
        public ContainerDetails(string id, bool autoPour)
        {
            Id = id;
            AutoPour = autoPour;
        }

        public string Id { get; init; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public double MaxLevel { get; set; }
        public double ThresholdWeight { get; set; }
        public double InitialFlowRate { get; set; }
        public double SteelDensity { get; set; } = 7850;
        public bool AutoPour { get; }
    }

    /// <summary>
    /// Abstract base class for all steel containers that hold and pour molten steel.
    /// </summary>
    public abstract class SteelContainer : IDisposable
    {
        private bool _disposed;
        private bool _thresholdReached;
        private bool _isHeatOut;
        private readonly ConcurrentQueue<HeatMin> _heats = new();
        private IDisposable? _pouringSubscription;

        protected readonly ContainerDetails _containerDetails;

        public event EventHandler? WeightThresholdReached;
        public event EventHandler? Empty;
        public event EventHandler<int>? HeatOut;
        public event EventHandler<HeatMin>? SteelPoured;

        public double FlowRate { get; private set; }
        public bool IsOpen { get; private set; }
        public string Id => _containerDetails.Id;

        /// <summary>
        /// Gets the total net weight of steel in the container in kilograms.
        /// </summary>
        public double NetWeight => _heats.Sum(x => x.Weight);

        public HeatMin[] Heats => _heats.ToArray();

        /// <summary>
        /// Gets the cross-sectional area of the container in square meters.
        /// </summary>
        public double CrossSectionalArea => _containerDetails.Width * _containerDetails.Depth;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteelContainer"/> class.
        /// </summary>
        protected SteelContainer(ContainerDetails details)
        {
            if (string.IsNullOrWhiteSpace(details?.Id))
                throw new ArgumentException("ID must be a valid string.", nameof(details.Id));

            _containerDetails = details;
            FlowRate = details.InitialFlowRate;
        }

        /// <summary>
        /// Adds a new heat to the container.
        /// </summary>
        public void AddHeat(int id)
        {
            if (_heats.Count >= 2) throw new InvalidOperationException("Too many heats in container.");
            _heats.Enqueue(new HeatMin(id, 0));
        }

        /// <summary>
        /// Adds steel to the container and starts pouring if the threshold is reached (when AutoPour is enabled).
        /// </summary>
        public void AddSteel(double weight)
        {
            if (!_heats.TryPeek(out var currentHeat))
                throw new InvalidOperationException("No heats in container.");

            currentHeat.Weight += weight;

            if (!_thresholdReached && NetWeight >= _containerDetails.ThresholdWeight)
            {
                _thresholdReached = true;
                WeightThresholdReached?.Invoke(this, EventArgs.Empty);

                if (_containerDetails.AutoPour)
                    StartPouring();
            }
        }

        /// <summary>
        /// Starts the pouring process.
        /// </summary>
        protected void StartPouring()
        {
            if (!_containerDetails.AutoPour || _pouringSubscription != null) return;

            _pouringSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => PourSteel());
        }

        private void PourSteel()
        {
            if (!_containerDetails.AutoPour || NetWeight <= 0)
            {
                StopPouring();
                return;
            }

            var steelWeight = Math.Min(FlowRate, NetWeight);
            RemoveSteelInternal(steelWeight);
            var heat = _heats.FirstOrDefault();
            if (heat != null)
                SteelPoured?.Invoke(this, heat);
        }

        /// <summary>
        /// Stops the pouring process.
        /// </summary>
        public void StopPouring()
        {
            _pouringSubscription?.Dispose();
            _pouringSubscription = null;
        }

        /// <summary>
        /// Manually removes steel from the container when AutoPour is disabled.
        /// </summary>
        public void RemoveSteel(double weight)
        {
            if (_containerDetails.AutoPour)
                throw new InvalidOperationException("Manual removal is only allowed when AutoPour is disabled.");

            RemoveSteelInternal(weight);
        }

        private void RemoveSteelInternal(double weight)
        {
            var lastHeatId = 0;

            while (weight > 0 && _heats.TryPeek(out var heat))
            {
                if (!IsOpen) IsOpen = true;

                if (!_isHeatOut)
                {
                    HeatOut?.Invoke(this, heat.Id); // Provide heat ID
                    _isHeatOut = true;
                }

                if (heat.Weight > weight)
                {
                    heat.Weight -= weight;
                    break;
                }

                weight -= heat.Weight;
                lastHeatId = heat.Id;
                _heats.TryDequeue(out _);
                _isHeatOut = false;
            }

            if (NetWeight <= 0)
            {
                Empty?.Invoke(this, EventArgs.Empty); // Provide last heat ID
                IsOpen = false;
            }
        }

        /// <summary>
        /// Gets the current steel level in the container in millimeters.
        /// </summary>
        public double GetSteelLevel()
        {
            var volume = NetWeight / _containerDetails.SteelDensity;
            return Math.Min((volume * 1_000_000) / (_containerDetails.Width * _containerDetails.Depth),
                _containerDetails.MaxLevel);
        }

        /// <summary>
        /// Sets the steel flow rate in kilograms per second.
        /// </summary>
        public void SetFlowRate(double flowRate)
        {
            FlowRate = flowRate;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopPouring();
            GC.SuppressFinalize(this);
        }

        ~SteelContainer()
        {
            Dispose();
        }
    }
}