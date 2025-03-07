using System;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Torch
    {
        public Product NextProduct { get; private set; }
        public double TorchLocation { get; } // Fixed position of the torch in meters
        private double _increment;
        public double MeasCutLengthMeters { get; private set; }
        public event EventHandler<Product> CutDone; // Event triggered when a product is cut

        public Torch(double torchLocation)
        {
            TorchLocation = torchLocation;
        }

        public void Measure(double increment)
        {
            _increment += increment;
            MeasCutLengthMeters = double.Max(0, _increment - TorchLocation);
            if (MeasCutLengthMeters == 0 || NextProduct == null) return;
            if (MeasCutLengthMeters < NextProduct.LengthAimMeters) return;
            NextProduct.CutLength = MeasCutLengthMeters;
            _increment = TorchLocation;
            CutDone?.Invoke(this, new Product(NextProduct));
            NextProduct = null;
        }

        public void SetNextProduct(Product product)
        {
            Console.WriteLine($"Set next product {product.ProductId}");
            NextProduct = product ?? throw new ArgumentNullException(nameof(product), "Next product cannot be null.");
        }
    }
}