using System;
using System.Reactive.Linq;

namespace CasterSimulator.Components
{
    public class Ladle
    {
        private readonly string heatId;
        private readonly double initialSteelWeight;
        private double remainingSteelWeight;
        private double pouringRate;
        private IDisposable pouringSubscription;

        public event EventHandler<double> SteelPoured; // Event for poured steel
        public event EventHandler LadleEmpty;

        public string HeatId => heatId;
        public double RemainingSteelWeight => remainingSteelWeight;

        public Ladle(double steelWeight, string heatId, double pouringRate = 200.0)
        {
            this.heatId = heatId;
            this.initialSteelWeight = steelWeight;
            this.remainingSteelWeight = steelWeight;
            this.pouringRate = pouringRate;
        }

        public void OpenLadle()
        {
            pouringSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    if (remainingSteelWeight <= 0)
                    {
                        pouringSubscription.Dispose();
                        LadleEmpty?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    double pouredSteel = Math.Min(pouringRate, remainingSteelWeight);
                    remainingSteelWeight -= pouredSteel;
                    SteelPoured?.Invoke(this, pouredSteel);
                });
        }

        public void CloseLadle()
        {
            pouringSubscription?.Dispose();
        }
    }
}