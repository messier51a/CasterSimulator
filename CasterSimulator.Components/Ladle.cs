using System;
using System.Reactive.Linq;

namespace CasterSimulator.Components
{
    public class Ladle
    {
        private readonly string _heatId;
        private readonly double _initialSteelWeight;
        private double _remainingSteelWeight;
        private double _pouringRate;
        private IDisposable _pouringSubscription;

        public event EventHandler<double> SteelPoured; // Event for poured steel
        public event EventHandler LadleEmpty;

        public string HeatId => _heatId;
        public double RemainingSteelWeight => _remainingSteelWeight;

        public double PouringRate => _pouringRate;
        public Ladle(double steelWeight, string heatId, double pouringRate = 200.0)
        {
            this._heatId = heatId;
            this._initialSteelWeight = steelWeight;
            this._remainingSteelWeight = steelWeight;
            this._pouringRate = pouringRate;
        }

        public void OpenLadle(double initialFlowRate)
        {
            _pouringRate = initialFlowRate; // Set the initial high flow rate
            _pouringSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    if (_remainingSteelWeight <= 0)
                    {
                        _pouringSubscription.Dispose();
                        LadleEmpty?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    double pouredSteel = Math.Min(_pouringRate, _remainingSteelWeight);
                    _remainingSteelWeight -= pouredSteel;
                    SteelPoured?.Invoke(this, pouredSteel);
                });
        }

        public void AdjustPouringRate(double newRate)
        {
            _pouringRate = newRate;
        }

        public void CloseLadle()
        {
            _pouringSubscription?.Dispose();
        }
    }
}