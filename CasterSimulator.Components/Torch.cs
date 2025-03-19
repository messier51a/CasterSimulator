using System;
using System.Collections.Concurrent;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Represents the cutting torch component in the continuous casting machine.
    /// Responsible for measuring strand length and cutting it into finished products
    /// at a fixed position along the casting line.
    /// </summary>
    public class Torch
    {
        /// <summary>
        /// Gets the fixed position of the torch along the strand path, measured in meters from the mold.
        /// </summary>
        public double TorchLocation { get; } 
        
        private double _increment;
        
        /// <summary>
        /// Gets the measured length of strand available for cutting, in meters.
        /// </summary>
        public double MeasCutLengthMeters { get; private set; }
        
        /// <summary>
        /// Event triggered when a product is cut from the strand.
        /// Provides the cut product details to event handlers.
        /// </summary>
        public event EventHandler<Product> CutDone; 
        
        /// <summary>
        /// Gets the next product to be cut from the strand.
        /// </summary>
        public Product NextProduct { get; private set; }

        private bool _isLastCut;
        private bool _optimizationInProgress;

        /// <summary>
        /// Initializes a new instance of the <see cref="Torch"/> class.
        /// </summary>
        /// <param name="torchLocation">The fixed position of the torch in meters from the mold.</param>
        public Torch(double torchLocation)
        {
            TorchLocation = torchLocation;
        }

        /// <summary>
        /// Measures the strand advancement and determines if a cut should be made.
        /// Triggers the CutDone event when a product reaches its target length.
        /// </summary>
        /// <param name="increment">The incremental distance the strand has advanced, in meters.</param>
        /// <param name="tailPosition">The position of the strand's tail from the mold, in meters.</param>
        public void Measure(double increment, double tailPosition)
        {
            _increment += increment;

            if (_optimizationInProgress) return;

            if (_isLastCut && tailPosition <= TorchLocation) return;
            
            MeasCutLengthMeters = Math.Max(0, _increment - TorchLocation);

            if (NextProduct == null || MeasCutLengthMeters == 0 ||
                MeasCutLengthMeters < NextProduct.LengthAimMeters) return;

            NextProduct.CutLength = MeasCutLengthMeters;
            _increment = TorchLocation;
            
            CutDone?.Invoke(this, NextProduct);
        }

        /// <summary>
        /// Sets the specifications for the next product to be cut.
        /// </summary>
        /// <param name="product">The product specification for the next cut.</param>
        /// <param name="isLastCut">Indicates whether this is the final product to be cut in the sequence.</param>
        /// <exception cref="ArgumentNullException">Thrown when product is null.</exception>
        public void SetNextProduct(Product product, bool isLastCut = false)
        {
            _isLastCut = isLastCut;
            NextProduct = product ?? throw new ArgumentNullException(nameof(product));
        }

        /// <summary>
        /// Sets a flag indicating whether cut length optimization is in progress.
        /// When optimization is in progress, the cutting process is temporarily suspended.
        /// </summary>
        /// <param name="optimizationInProgress">True to pause cutting for optimization; otherwise, false.</param>
        public void SetOptimizationInProgress(bool optimizationInProgress)
        {
            _optimizationInProgress = optimizationInProgress;
        }

        /// <summary>
        /// Resets the next product to null, indicating no scheduled product to cut.
        /// </summary>
        public void ResetNextProduct()
        {
            NextProduct = null;
        }
    }
}