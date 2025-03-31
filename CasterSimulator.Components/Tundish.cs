using System;
using System.Reactive.Linq;
using CasterSimulator.Models;
using System.Linq;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Represents the tundish in the steel casting machine.
    /// It manages steel flow, temperature simulation, and superheat calculations.
    /// </summary>
    public class Tundish : SteelContainer
    {
        /// <summary>
        /// Current temperature of the steel in the tundish (°C).
        /// </summary>
        public double Temperature { get; private set; }

        /// <summary>
        /// Calculates the superheat in the tundish (difference between steel and liquidus temperature).
        /// </summary>
        public double SuperheatC => CalculateSuperheat();
        
        /// <summary>
        /// Calculates the target superheat based on weighted average of heats.
        /// </summary>
        public double SuperheatTargetC => CalculateWeightedAverage(h => h.TargetSuperheatC);
        
        /// <summary>
        /// Stopper rod position as a percentage, based on flow rate.
        /// </summary>
        public double StopperRodPositionPercent =>
            Math.Clamp((FlowRateKgSec / ContainerDetails.MaxFlowRateKgSec) * 100.0, 0, 100);

        private bool IsSteelFlowing => FlowRateKgSec > 0;
        private IDisposable _temperatureUpdater;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tundish"/> class with default dimensions and properties.
        /// </summary>
        /// <param name="id">Unique identifier for the tundish.</param>
        /// <param name="thresholdLevel">Threshold level for weight-based triggering.</param>
        public Tundish(string id, double thresholdLevel)
            : base(new ContainerDetails(id)
            {
                Width = 3.876,
                Depth = 1.550,
                MaxLevel = 1.181,
                ThresholdLevelMm = 127,
                InitialFlowRate = 30,
                MaxFlowRateKgSec = 150,
            })
        {
            NewSteelAdded += OnNewSteelAdded;
            StartTemperatureSimulation();
        }

        /// <summary>
        /// Handles temperature increase when new steel is added.
        /// </summary>
        private void OnNewSteelAdded(object? sender, int heatId)
        {
            if (Heats.Length == 0) return;

            if (Temperature == 0)
            {
                // Generate a random temperature around 1550°C with a ±10°C variation
                Temperature = 1550 + new Random().Next(0, 10);
            }
            else
            {
                // Small increase when adding new steel
                Temperature += new Random().NextDouble() * 5 + 3;
            }
        }

        /// <summary>
        /// Updates the temperature based on cooling rates, depending on whether steel is flowing.
        /// </summary>
        private void UpdateTemperature()
        {
            if (Temperature == 0) return;
            var rand = new Random();
            var coolingRate = IsSteelFlowing ? rand.NextDouble() * 0.05 + 0.02 : rand.NextDouble() * 0.1 + 0.05;
            Temperature -= coolingRate;
        }

        /// <summary>
        /// Starts the temperature simulation, decreasing temperature over time.
        /// </summary>
        private void StartTemperatureSimulation()
        {
            _temperatureUpdater = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => UpdateTemperature());
        }

        /// <summary>
        /// Calculates the superheat as the difference between tundish temperature and liquidus temperature.
        /// </summary>
        private double CalculateSuperheat()
        {
            var liquidusTemp = CalculateWeightedAverage(h => h.LiquidusTemperatureC);
            return Temperature - liquidusTemp;
        }

        /// <summary>
        /// Computes the weighted average of a given property across all heats in the tundish.
        /// </summary>
        /// <param name="selector">Function to select the property from each heat.</param>
        private double CalculateWeightedAverage(Func<HeatMin, double> selector)
        {
            if (Heats.Length == 0 || NetWeightKgs == 0) return 0.0;
            return Heats.Sum(h => selector(h) * h.Weight) / NetWeightKgs;
        }
        public override void Dispose()
        {
            _temperatureUpdater?.Dispose();
            base.Dispose();
        }
    }
}
