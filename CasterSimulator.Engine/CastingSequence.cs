using System;
using System.Collections.Generic;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine
{
    public class CastingSequence
    {
        private readonly Tundish _tundish;
        private readonly Ladle[] _ladles;
        private readonly Mold _mold;
        private readonly Strand _strand;
        private readonly Torch _torch;
        private readonly Queue<Product> _productQueue; // Queue of products to cut
        private int _currentLadleIndex;
        private bool _isRunning;

        public int CurrentLadleIndex => _currentLadleIndex; // Expose current ladle index
        public double StrandLength => _strand.StrandLength; // Expose strand length
        public bool IsRunning => _isRunning; // Expose whether the sequence is running
        public Product NextProduct => _torch.NextProduct; // Expose next product to cut
        public double CurrentLadleWeight => _ladles[_currentLadleIndex].RemainingSteelWeight; // Expose current ladle weight
        public double TundishWeight => _tundish.CurrentSteelWeight; // Expose current tundish weight
        public double LastStrandIncrement => _strand.LastIncrement; // Expose last strand increment
        public bool IsTorchMonitoring => _torch.NextProduct != null; // Expose torch monitoring status

        public CastingSequence(Ladle[] ladlesArray, Mold mold, Tundish tundish)
        {
            if (ladlesArray == null || ladlesArray.Length == 0)
                throw new ArgumentException("At least one ladle is required.", nameof(ladlesArray));

            _mold = mold ?? throw new ArgumentNullException(nameof(mold));
            _tundish = tundish ?? throw new ArgumentNullException(nameof(tundish));
            _ladles = ladlesArray;
            _strand = new Strand(_mold);
            _torch = new Torch(20.0); // Default torch position
            _productQueue = new Queue<Product>();

            _currentLadleIndex = 0;

            RegisterLadleEvents(_ladles[0]);
            RegisterTundishEvents();
            RegisterStrandEvents();
            RegisterTorchEvents();
        }

        public void Run()
        {
            _ladles[_currentLadleIndex].OpenLadle();

            // Example products
            _productQueue.Enqueue(new Product("Prod1", 10.0));
            _productQueue.Enqueue(new Product("Prod2", 12.0));
            _productQueue.Enqueue(new Product("Prod3", 8.0));

            SetNextProduct();

            _isRunning = true;

            while (_isRunning)
            {
                // Simulation logic
            }
        }

        private void RegisterLadleEvents(Ladle ladle)
        {
            ladle.SteelPoured += (s, pouredSteel) =>
            {
                _tundish.AddSteel(pouredSteel);
            };

            ladle.LadleEmpty += (s, e) =>
            {
                SwitchLadle();
            };
        }

        private void RegisterTundishEvents()
        {
            _tundish.CastingThresholdReached += (s, e) =>
            {
                _strand.StartCasting(3.0 / 60.0); // Start with an initial speed of 3 m/min
            };
            _tundish.TundishEmpty += (s, e) =>
            {
                _strand.TailOut();
                _isRunning = false;
            };
        }

        private void RegisterStrandEvents()
        {
            _strand.StrandUpdated += (s, e) =>
            {
                // Update torch with the current strand length
                _torch.SetStrandLength(_strand.StrandLength);

                // Calculate mass flow based on strand's length increment
                double crossSectionalArea = _mold.GetCrossSectionalArea(); // m²
                double steelDensity = 7850; // kg/m³
                double massFlow = crossSectionalArea * _strand.LastIncrement * steelDensity * 2.20462; // lbs

                // Remove steel from the tundish
                _tundish.RemoveSteel(massFlow);
            };
        }

        private void RegisterTorchEvents()
        {
            _torch.CutDone += (s, product) =>
            {
                SetNextProduct();
            };
        }

        private void SetNextProduct()
        {
            if (_productQueue.Count > 0)
            {
                var nextProduct = _productQueue.Dequeue();
                _torch.SetNextProduct(nextProduct);
            }
        }

        private void SwitchLadle()
        {
            if (++_currentLadleIndex >= _ladles.Length)
            {
                _isRunning = false;
                return;
            }

            var nextLadle = _ladles[_currentLadleIndex];
            RegisterLadleEvents(nextLadle);
            nextLadle.OpenLadle();
        }
    }
}
