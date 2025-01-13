using System;
using System.Reactive.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Ladle
    {
        private readonly Heat _heat;
        private readonly double _initialSteelWeight;
        private double _remainingSteelWeight;
        private double _pouringRate;
        public int HeatId { get; private set; } 

        public event Action<object, double, int> SteelPoured;
        public event Action<object, int> LadleEmpty;
        public double RemainingSteelWeight => _remainingSteelWeight;
        public double PouringRate => _pouringRate;

        public Ladle(Heat heat, double pouringRate = 200.0)
        {
            HeatId = heat.Id;
            _heat = heat;
            this._initialSteelWeight = heat.NetWeight;
            this._remainingSteelWeight = heat.NetWeight;
            this._pouringRate = pouringRate;
        }

        public async Task OpenAsync(double initialFlowRate, double simulationIntervalSeconds = 1.0)
        {
            _pouringRate = initialFlowRate; // Set the initial high flow rate

            while (_remainingSteelWeight > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(simulationIntervalSeconds));

                var pouredSteel = Math.Min(_pouringRate, _remainingSteelWeight);
                _remainingSteelWeight -= pouredSteel;
                SteelPoured?.Invoke(this, pouredSteel, _heat.Id);
            }

            LadleEmpty?.Invoke(this, _heat.Id);
        }


        public void SetPouringRate(double newRate)
        {
            _pouringRate = newRate;
        }
        
    }
}