using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine
{
    public sealed class CastingSequence : IDisposable
    {
        private const double MaxTundishWeight = 27000.0; // Maximum tundish weight in kg
        private const double RampUpThreshold = 20000.0; // Ramp-up threshold in kg
        private const double LowPouringRate = 68.0; // Low flow rate in kg/s
        private const double RampUpPouringRate = 90.0; // Ramp-up flow rate in kg/s
        private const double SteadyStateRate = 84.0; // Steady-state flow rate in kg/s
        private const double TorchLocation = 10; //Distance from mold to torch machine in meters.
        private const double IntervalMilliseconds = 200;
        private const double SteelDensity = 7850;

        private bool _disposed = false;
        private Dictionary<int, Heat> _heats = new();
        private readonly Tundish _tundish;
        private Ladle _ladle = null!;
        private readonly Torch _torch;
        private bool _isFirstHeat;
        private TaskCompletionSource<bool> _ladleEmptySignal;
        private TaskCompletionSource<bool> _castingFinishedSignal;
        public List<Product> Products { get; private set; } = [];

        private IDisposable? _pouringRateSubscription;
        public Dictionary<int, Heat> Heats => _heats;
        public Strand Strand { get; }

        public Product NextProduct => _torch.NextProduct; // Expose next product to cut
        public Tundish Tundish => _tundish;
        public Ladle Ladle => _ladle;

        public Torch Torch => _torch;

        private readonly Sequence _sequence;

        public event Action<object, long> SequenceDone = null!;

        public CastingSequence(Sequence sequence)
        {
            ArgumentNullException.ThrowIfNull(sequence);
            if (sequence.Heats.Count == 0) throw new ArgumentException("Sequence has no heats");
            if (sequence.Products.Count == 0) throw new ArgumentException("Sequence has no products");

            _sequence = sequence;

            _tundish = new Tundish("Tundish1");
            _torch = new Torch(TorchLocation); // Default torch position

            var mold = new Mold("Mold1", sequence.Width, sequence.Thickness);
         
            Strand = new Strand(mold, _torch, speedControl, _sequence.SteelDensity, IntervalMilliseconds);

            RegisterTundishEvents();
            RegisterTorchEvents();
            RegisterStrandEvents();

            _isFirstHeat = true;

            _castingFinishedSignal = new TaskCompletionSource<bool>();
        }

        public async Task StartAsync()
        {
            while (_sequence.Heats.Count > 0)
            {
                _ladleEmptySignal = new TaskCompletionSource<bool>();

                // Process the next heat
                var nextHeat = _sequence.Heats.Dequeue();
                _heats.Add(nextHeat.Id, nextHeat);
                _ladle = new Ladle(nextHeat);

                _ladle.SteelPoured += (s, pouredSteel, heatId) =>
                {
                    _tundish.AddSteel(pouredSteel);
                    if (_heats[heatId].Status != HeatStatus.Next) return;
                    _heats[heatId].Status = HeatStatus.Pouring;
                    _heats[heatId].HeatStartUtcTime = DateTime.Now;
                };

                _ladle.Empty += (s, heatId) =>
                {
                        _ladleEmptySignal.TrySetResult(true);
                    _heats[heatId].HeatEndUtcTime = DateTime.Now;
                };

                StartPouringRateMonitor();

                await _ladle.PourSteel(300, IntervalMilliseconds);

                if (!_isFirstHeat)
                {
                    _tundish.MixZoneStart();
                }

                _isFirstHeat = false; // Ensure only the first heat skips MixZoneStart

                    await _ladleEmptySignal.Task;
            }

            await _castingFinishedSignal.Task;
            // Trigger SequenceDone event
            SequenceDone?.Invoke(this, _sequence.Id);
        }

        private void StartPouringRateMonitor()
        {
            _pouringRateSubscription = Observable.Interval(TimeSpan.FromMilliseconds(IntervalMilliseconds))
                .TakeUntil(_ladleEmptySignal.Task.ToObservable())
                .Subscribe(_ => AdjustPouringRate());
        }

        private void AdjustPouringRate()
        {
            // Monitor and dynamically adjust flow rate
            if (_tundish.CurrentSteelWeight > MaxTundishWeight) // Prevent overflow
            {
                _ladle.SetPouringRate(LowPouringRate);
            }
            else if (_tundish.CurrentSteelWeight < RampUpThreshold && Strand.TotalCastLength < 7.0) // During ramp-up
            {
                _ladle.SetPouringRate(RampUpPouringRate);
            }
            else
            {
                _ladle.SetPouringRate(SteadyStateRate); // Steady-state rate
            }
        }

        public void StopPouringRateAdjustment()
        {
            _pouringRateSubscription?.Dispose();
        }

        private void RegisterTundishEvents()
        {
            _tundish.WeightThresholdReached += (s, e) =>
            {
                Strand.StartCasting(); // Ramp speed from 0 to 4 m/min over 30 seconds
                _heats[_ladle.HeatId].Status = HeatStatus.Casting;
                _torch.SetNextProduct(_sequence.Products.Dequeue());
            };

            _tundish.MixZoneEnded += (s, e) => { _heats[_ladle.HeatId].MixZoneEnd = Strand.TotalCastLength; };

            _tundish.Empty += (s, e) => { Strand.TailOut(); };

            _tundish.HeatOnStrand += (s, e) =>
            {
                _heats[_ladle.HeatId].CastLengthAtStartMeters = Strand.TotalCastLength;
                _heats[_ladle.HeatId].Status = HeatStatus.Casting;
            };
        }

        private void RegisterStrandEvents()
        {
            Strand.Advanced += (s, massFlow) =>
            {
                _tundish.RemoveSteel(massFlow);
                
                foreach (var heat in _heats
                             .Where(heat => heat.Value.Status == HeatStatus.Cutting))
                {
                    heat.Value.Status = HeatStatus.Cast;
                }

                foreach (var heat in _heats
                             .Where(heat => heat.Value.Status == HeatStatus.Casting &&
                                 Strand.TotalCastLength - heat.Value.CastLengthAtStartMeters > _torch.TorchLocation))
                {
                    heat.Value.Status = HeatStatus.Cutting;
                }
            };
            Strand.CastingFinished += (s, e) => { _castingFinishedSignal.TrySetResult(true); };
        }

        private void RegisterTorchEvents()
        {
            _torch.CutDone += (s, product) =>
            {
                product.Weight = product.CutLength * product.Width * product.Thickness * _sequence.SteelDensity;
                Products.Add(product.Clone());
                var nextProduct = _sequence.Products.Dequeue();
                _torch.SetNextProduct(nextProduct);
            };
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
                _pouringRateSubscription?.Dispose();
            }

            _disposed = true;
        }

        ~CastingSequence()
        {
            Dispose(false);
        }
    }
}