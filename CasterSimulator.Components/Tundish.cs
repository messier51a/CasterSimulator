using System;
using System.Reactive.Linq;
using CasterSimulator.Models;
using System.Linq;

namespace CasterSimulator.Components
{
    public class Tundish : SteelContainer
    {
        public double Temperature { get; private set; }

        public double SuperheatC => CalculateSuperheat();
        public double SuperheatTargetC => CalculateWeightedAverage(h => h.TargetSuperheatC);
        
        public double StopperRodPositionPercent =>
            Math.Clamp((FlowRateKgSec / ContainerDetails.MaxFlowRateKgSec) * 100.0, 0, 100);

        private bool IsSteelFlowing => FlowRateKgSec > 0;
        private IDisposable _temperatureUpdater;

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

        private void UpdateTemperature()
        {
            if (Temperature == 0) return;
            var rand = new Random();
            var coolingRate = IsSteelFlowing ? rand.NextDouble() * 0.05 + 0.02 : rand.NextDouble() * 0.1 + 0.05;
            Temperature -= coolingRate;
        }

        private void StartTemperatureSimulation()
        {
            _temperatureUpdater = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => UpdateTemperature());
        }

        private double CalculateSuperheat()
        {
            var liquidusTemp = CalculateWeightedAverage(h => h.LiquidusTemperatureC);
            return Temperature - liquidusTemp;
        }

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
