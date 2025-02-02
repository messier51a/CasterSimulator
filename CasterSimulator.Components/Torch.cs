using System;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Torch
    {
        public Product NextProduct { get; private set; } // Next product to be cut
        public double TorchLocation { get; } // Fixed position of the torch in meters
        private double _increment;
        public double MeasuredCutLength { get; private set; }
        public event EventHandler<Product> CutDone; // Event triggered when a product is cut

        public Torch(double torchLocation)
        {
            TorchLocation = torchLocation;
        }

        public void Measure(double increment)
        {
            _increment += increment;
            //Console.WriteLine($"MeasuredCutLength raw: {_increment - TorchLocation}");
            MeasuredCutLength = double.Max(0, _increment - TorchLocation);
            if (MeasuredCutLength < NextProduct.LengthAim) return;
            NextProduct.CutLength = MeasuredCutLength;
            _increment = TorchLocation;
            CutDone?.Invoke(this, NextProduct);
        }

        public void SetNextProduct(Product product)
        {
            NextProduct = product ?? throw new ArgumentNullException(nameof(product), "Next product cannot be null.");
        }
    }
}