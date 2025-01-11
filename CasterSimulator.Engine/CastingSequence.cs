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
        private CastingStatus _status;

        private readonly List<HeatSegment> _heatSegments = new List<HeatSegment>();
        private HeatSegment _currentHeatSegment;

        public IReadOnlyList<HeatSegment> HeatSegments => _heatSegments; // Expose heat segments
        public double CastSpeed => _strand.CastSpeed;
        public double CastLength => _strand.CastLength; // Expose cast length
        public double StrandLength => _strand.StrandLength; // Expose strand length
        public bool IsRunning => _isRunning; // Expose whether the sequence is running
        public Product NextProduct => _torch.NextProduct; // Expose next product to cut
        public Ladle CurrentLadle => _ladles[_currentLadleIndex];
        public double TundishWeight => _tundish.CurrentSteelWeight; // Expose current tundish weight
        public double LastStrandIncrement => _strand.LastIncrement; // Expose last strand increment
        public bool IsTorchMonitoring => _torch.NextProduct != null; // Expose torch monitoring status
        public CastingStatus Status => _status; // Expose casting status

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
            _status = CastingStatus.Idle;

            RegisterLadleEvents(_ladles[0]);
            RegisterTundishEvents();
            RegisterStrandEvents();
            RegisterTorchEvents();
        }

        public void Run()
        {
            _status = CastingStatus.ReadyToCast;

            // Start the initial rapid fill
            _ladles[_currentLadleIndex].OpenLadle(300.0); // High initial flow rate
            _status = CastingStatus.Pouring;

            // Example products
            _productQueue.Enqueue(new Product("Prod1", 10.0));
            _productQueue.Enqueue(new Product("Prod2", 12.0));
            _productQueue.Enqueue(new Product("Prod3", 8.0));

            SetNextProduct();

            _isRunning = true;

            while (_isRunning)
            {
                // Monitor and dynamically adjust flow rate
                if (_tundish.CurrentSteelWeight > 27000.0) // Prevent overflow (27 tons in kg)
                {
                    _ladles[_currentLadleIndex].AdjustPouringRate(68.0); // Lower flow rate (kg/s)
                }
                else if (_tundish.CurrentSteelWeight > 20000.0 && _strand.CastLength < 7.0) // During ramp-up
                {
                    _ladles[_currentLadleIndex].AdjustPouringRate(90.0); // Adjust during ramp-up (kg/s)
                }
                else
                {
                    _ladles[_currentLadleIndex].AdjustPouringRate(84.0); // Steady-state rate (kg/s)
                }
            }

            _status = CastingStatus.Cast;
        }

        private void RegisterLadleEvents(Ladle ladle)
        {
            ladle.SteelPoured += (s, pouredSteel) => { _tundish.AddSteel(pouredSteel); };

            ladle.LadleEmpty += (s, e) => { SwitchLadle(); };
        }

        private void RegisterTundishEvents()
        {
            _tundish.CastingThresholdReached += (s, e) =>
            {
                _strand.StartCasting(0, 4.0 / 60.0, 90.0); // Ramp speed from 0 to 4 m/min over 30 seconds
                _status = CastingStatus.Casting;
            };

            _tundish.MixZoneEnded += (s, e) =>
            {
                if (_currentHeatSegment != null)
                {
                    _currentHeatSegment.MixZoneEnd = _strand.CastLength;
                    Console.WriteLine(
                        $"Mix Zone Ended for Heat {_currentHeatSegment.HeatId}. Start: {_currentHeatSegment.MixZoneStart:F2} m, End: {_currentHeatSegment.MixZoneEnd:F2} m.");
                }
            };

            _tundish.TundishEmpty += (s, e) =>
            {
                _strand.TailOut();
                _status = CastingStatus.Tailout;
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
                double massFlow = crossSectionalArea * _strand.LastIncrement * steelDensity; // kg

                // Remove steel from the tundish
                _tundish.RemoveSteel(massFlow);

                // Advance heat boundaries for all heats
                foreach (var segment in _heatSegments)
                {
                    segment.HeatBoundary += _strand.LastIncrement;
                }
            };
        }

        private void RegisterTorchEvents()
        {
            _torch.CutDone += (s, product) => { SetNextProduct(); };
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
                _status = CastingStatus.Cast;
                _isRunning = false;
                return;
            }

            var nextLadle = _ladles[_currentLadleIndex];
            RegisterLadleEvents(nextLadle);

            nextLadle.OpenLadle(300.0); // Start the next ladle with a high flow rate
            _status = CastingStatus.Pouring;

            if (_currentLadleIndex > 0) // Not the first heat
            {
                var heatSegment = new HeatSegment
                {
                    HeatId = nextLadle.HeatId,
                    MixZoneStart = _strand.CastLength,
                    HeatBoundary = _strand.CastLength // Initialize boundary at current length
                };

                _heatSegments.Add(heatSegment);

                _currentHeatSegment = heatSegment;

                _tundish.StartMixZone();
            }
        }
    }
}