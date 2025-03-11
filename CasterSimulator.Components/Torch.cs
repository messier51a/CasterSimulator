using System;
using System.Collections.Concurrent;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Torch
    {
        public double TorchLocation { get; } // Fixed position of the torch in meters
        private double _increment;
        public double MeasCutLengthMeters { get; private set; }
        public event EventHandler<Product> CutDone; // Event triggered when a product is cut
        public Product NextProduct { get; private set; }

        private bool _isLastCut;

        public Torch(double torchLocation)
        {
            TorchLocation = torchLocation;
        }

        public void Measure(double increment, double tailPosition)
        {
            _increment += increment;

            if (_isLastCut && tailPosition <= TorchLocation) return;
            
            MeasCutLengthMeters = Math.Max(0, _increment - TorchLocation);

            if (NextProduct == null || MeasCutLengthMeters == 0 ||
                MeasCutLengthMeters < NextProduct.LengthAimMeters) return;

            NextProduct.CutLength = MeasCutLengthMeters;
            _increment = TorchLocation;
            
            CutDone?.Invoke(this, NextProduct);
        }

        public void SetNextProduct(Product product, bool isLastCut = false)
        {
            _isLastCut = isLastCut;
            NextProduct = product ?? throw new ArgumentNullException(nameof(product));
        }

        public void ResetNextProduct()
        {
            NextProduct = null;
        }
    }
}