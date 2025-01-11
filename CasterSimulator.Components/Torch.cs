using System;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class Torch
    {
        public Product NextProduct { get; private set; } // Next product to be cut
        public double TorchPosition { get; } // Fixed position of the torch in meters
        private double _currentStrandLength; // Current strand length

        public event EventHandler<Product> CutDone; // Event triggered when a product is cut

        public Torch(double torchPosition = 20.0)
        {
            TorchPosition = torchPosition;
        }

        // Set the current strand length
        public void SetStrandLength(double strandLength)
        {
            _currentStrandLength = strandLength;

            // Check if a cut is needed
            if (NextProduct != null && _currentStrandLength - TorchPosition >= NextProduct.ProductLength)
            {
                OnCutDone();
            }
        }

        // Set the next product to be cut
        public void SetNextProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product), "Next product cannot be null.");

            NextProduct = product;
            Console.WriteLine($"Next product set: ProductId={product.ProductId}, ProductLength={product.ProductLength:F2} m");
        }

        // Raise the CutDone event
        private void OnCutDone()
        {
            var cutProduct = NextProduct;

            // Trigger event with the product information
            CutDone?.Invoke(this, cutProduct);

            Console.WriteLine($"Cut Done: ProductId={cutProduct.ProductId}, ProductLength={cutProduct.ProductLength:F2} m");

            // Clear current product after cut
            NextProduct = null;
        }
    }
}