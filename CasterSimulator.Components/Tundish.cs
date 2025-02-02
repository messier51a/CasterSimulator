using System;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Schema;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class TundishHeat
    {
        public Heat Heat { get; init; }
        public double Weight { get; set; }

        public TundishHeat(Heat heat)
        {
            Heat = heat;
            Weight = 0;
        }
    }

    public class Tundish : IDisposable
    {
        private bool _disposed;
        private readonly double _thresholdWeight;
        private bool _thresholdReached;
        private bool _heatOnStrand;
        private Queue<TundishHeat> _tundishHeats = new();
        public TundishHeat[] Heats => _tundishHeats.ToArray();
        public event EventHandler WeightThresholdReached;
        public event EventHandler Empty;
        public event EventHandler<int>? HeatOnStrand;

        public string Id { get; private set; }
        public double NetWeight => _tundishHeats.Sum(x => x.Weight);

        public Tundish(string id, double thresholdWeight)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tundish ID must be a valid string.", nameof(id));
            ArgumentOutOfRangeException.ThrowIfLessThan(thresholdWeight, 3000);
            Id = id;
            _thresholdWeight = thresholdWeight;
        }

        public void AddHeat(Heat heat)
        {
            if (_tundishHeats.Count == 2) throw new InvalidOperationException("Too many heats in tundish.");

            _tundishHeats.Enqueue(new TundishHeat(heat));
        }

        public void AddSteel(double weight)
        {
            var lastHeat = _tundishHeats.LastOrDefault();
            if (lastHeat is null) throw new InvalidOperationException();
            lastHeat.Weight += weight;

            if (!_thresholdReached && NetWeight >= _thresholdWeight)
            {
                _thresholdReached = true;
                WeightThresholdReached?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RemoveSteel(double weight)
        {
            while (true)
            {
                if (weight <= 0 || NetWeight <= 0) return;

                if (_tundishHeats.Count == 0) throw new InvalidOperationException($"No heats in tundish.");

                var heat = _tundishHeats.Peek();

                if (!_heatOnStrand)
                {
                    HeatOnStrand?.Invoke(this, heat.Heat.Id);
                    _heatOnStrand = true;
                }

                if (heat.Weight > weight)
                {
                    heat.Weight -= weight;
                }
                else
                {
                    weight -= heat.Weight;
                    _tundishHeats.Dequeue();
                    if (_tundishHeats.Count == 0)
                    {
                        Empty?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        _heatOnStrand = false;
                        continue;
                    }
                }

                break;
            }
        }

        public void Dispose()
        {
            Dispose(true); // Explicit disposal
            GC.SuppressFinalize(this); // Suppress finalization
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
            }

            _disposed = true;
        }

        ~Tundish()
        {
            Dispose(false);
        }
    }
}