using System;
using System.Collections.Concurrent;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Torch
    {
        private ConcurrentQueue<Product> _nextProductQueue = new();
        public double TorchLocation { get; } // Fixed position of the torch in meters
        private double _increment;
        public double MeasCutLengthMeters { get; private set; }
        public event EventHandler<Product> CutDone; // Event triggered when a product is cut
        public Product NextProduct => _nextProductQueue.TryPeek(out var nextProduct) ? nextProduct : new Product();

        public Torch(double torchLocation)
        {
            TorchLocation = torchLocation;
        }
        public void Measure(double increment)
        {
            _increment += increment;
            MeasCutLengthMeters = Math.Max(0, _increment - TorchLocation);

            if (MeasCutLengthMeters == 0 || 
                !_nextProductQueue.TryPeek(out var nextProduct) ||
                MeasCutLengthMeters < nextProduct.LengthAimMeters) return;

            if (!_nextProductQueue.TryDequeue(out nextProduct)) return;
            
            nextProduct.CutLength = MeasCutLengthMeters;
            _increment = TorchLocation;

            CutDone?.Invoke(this, nextProduct);
        }

        public void SetNextProduct(Product product)
        {
            _nextProductQueue.Enqueue(product);
        }
    }
}