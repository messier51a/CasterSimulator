using System;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Torch
    {
        public Product NextProduct { get; private set; } // Next product to be cut
        public double TorchPosition { get; } // Fixed position of the torch in meters
        private double _headDistanceFromMold; // Current strand length

        public event EventHandler<Product> CutDone; // Event triggered when a product is cut

        public Torch(double torchPosition = 20.0)
        {
            TorchPosition = torchPosition;
        }
        public void Update(double headDistanceFromMold)
        {
            _headDistanceFromMold = headDistanceFromMold;
            var length = headDistanceFromMold - TorchPosition;
            if (NextProduct is null || length < NextProduct.LengthAim) return;
            NextProduct.LengthCut = length;
            OnCutDone();
        }
        public void SetNextProduct(Product product)
        {
            NextProduct = product ?? throw new ArgumentNullException(nameof(product), "Next product cannot be null.");
        }
        private void OnCutDone()
        {
            var cutProduct = NextProduct;
            CutDone?.Invoke(this, cutProduct);
            NextProduct = null;
        }
    }
}