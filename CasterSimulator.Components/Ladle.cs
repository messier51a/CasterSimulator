using System;
using System.Reactive.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Ladle
    {
        private readonly Heat _heat;
        private readonly double _initialSteelWeight;
        public int HeatId { get; private set; } 

        public event Action<object, double, int>? SteelPoured;
        public event Action<object, int>? LadleEmpty;
        public double RemainingSteelWeight { get; private set; }

        public double PouringRate { get; private set; }

        public Ladle(Heat heat, double pouringRate = 200.0)
        {
            HeatId = heat.Id;
            _heat = heat;
            _initialSteelWeight = heat.NetWeight;
            RemainingSteelWeight = heat.NetWeight;
            PouringRate = pouringRate;
        }

        public async Task OpenAsync(double initialFlowRate, double simulationIntervalMilliseconds = 1000)
        {
            PouringRate = initialFlowRate; // Set the initial high flow rate

            while (RemainingSteelWeight > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(simulationIntervalMilliseconds));

                var pouredSteel = Math.Min(PouringRate, RemainingSteelWeight);
                RemainingSteelWeight -= pouredSteel;
                SteelPoured?.Invoke(this, pouredSteel, _heat.Id);
            }

            LadleEmpty?.Invoke(this, _heat.Id);
        }


        public void SetPouringRate(double newRate)
        {
            PouringRate = newRate;
        }
        
    }
}