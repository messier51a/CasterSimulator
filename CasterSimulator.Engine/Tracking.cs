using System.Collections.Concurrent;
using CasterSimulator.Common.Collections;
using CasterSimulator.Components;
using CasterSimulator.MES;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public class Tracking : IDisposable
{
    private bool _disposed;

    public Caster Caster { get; private set; }

    //private ConcurrentDictionary<int, Heat> _heats = new();
    private Sequence _sequence;

    private bool _isScheduleOptimized;
    
    private TaskCompletionSource<bool> _castingFinishedSignal;
    public ConcurrentDictionary<int, Heat> Heats => _sequence.Heats;
    public ObservableConcurrentQueue<Product> CutProducts { get; private set; } = new();
    public ObservableConcurrentQueue<Product> Products => _sequence.Products;

    private EventHandler _castingFinishedHandler;
    private EventHandler _strandAdvancedHandler;
    private EventHandler<Product> _torchCutDoneHandler;
    private EventHandler<int> _ladleOpenedHandler;
    private EventHandler<int> _ladleClosedHandler;
    private EventHandler _tundishWeightThresholdHandler;
    private EventHandler<int> _tundishHeatOut;
    private EventHandler<int> _tundishEmptyHandler;

    public event Action? HeatStatusChanged;

    public Tracking()
    {
        Caster = new();
        RegisterEvents();
    }

    public async Task<long> StartSequence(Sequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence, nameof(sequence));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Id, nameof(sequence.Id));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Heats.Count, nameof(sequence.Heats));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Products.Count, nameof(sequence.Products));

        _sequence = sequence;

        _isScheduleOptimized = false;

        _castingFinishedSignal = new TaskCompletionSource<bool>();

        Console.WriteLine(sequence.Heats.Count);

        while (sequence.Heats.Values.Any(heat => heat.Status == HeatStatus.New))
        {
            var nextHeat = sequence.Heats.Values
                .Where(heat => heat.Status == HeatStatus.New)
                .OrderBy(heat => heat.Id)
                .FirstOrDefault();

            if (nextHeat == null)
                break;

            nextHeat.Status = HeatStatus.Next;
            var ladle = new Ladle("L001");
            var heat = new HeatMin(nextHeat.Id, nextHeat.NetWeight)
            {
                SteelGradeId = nextHeat.SteelGradeId,
                LiquidusTemperatureC = Schedule.SteelGrades[nextHeat.SteelGradeId].LiquidusTemperatureC,
                TargetSuperheatC = Schedule.SteelGrades[nextHeat.SteelGradeId].TargetSuperheatC
            };
            ladle.AddSteel(heat);
            Caster.Turret.AddLadle(ladle);
            await Caster.Turret.Rotate();
            if (Caster.Turret.LadleInCastPosition.Id != ladle.Id)
                throw new Exception($"Ladle {ladle.Id} is not in cast position.");
            RegisterLadleEvents();
            await Caster.Turret.LadleInCastPosition.PourAsync();
        }

        await _castingFinishedSignal.Task;
        return sequence.Id;
    }

    private void SetHeatStatus(int heatId, HeatStatus status)
    {
        switch (status)
        {
            case HeatStatus.Pouring:
                _sequence.Heats[heatId].HeatOpenTimeUtc = DateTime.UtcNow;
                break;
            case HeatStatus.Closed:
                _sequence.Heats[heatId].HeatCloseTimeUtc = DateTime.UtcNow;
                break;
            case HeatStatus.Casting:
                _sequence.Heats[heatId].HeatCastingTimeUtc = DateTime.UtcNow;
                break;
            case HeatStatus.New:
            case HeatStatus.Next:
            case HeatStatus.Cutting:
            case HeatStatus.Cast:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        _sequence.Heats[heatId].Status = status;
        HeatStatusChanged?.Invoke();
    }

    private void RegisterCasterEvents()
    {
        _castingFinishedHandler = (s, e) => { _castingFinishedSignal.TrySetResult(true); };
        Caster.CastingFinished += _castingFinishedHandler;
    }

    private void RegisterStrandEvent()
    {
        _strandAdvancedHandler = (s, e) =>
        {
            foreach (var heat in _sequence.Heats.Where(heat => heat.Value.HeatCastingTimeUtc > DateTime.MinValue))
            {
                heat.Value.HeatBoundary += Caster.Strand.CastLengthIncrement;
            }

            foreach (var heat in _sequence.Heats.Where(heat => heat.Value.Status == HeatStatus.Cutting))
            {
                heat.Value.Status = HeatStatus.Cast;
            }

            foreach (var heat in _sequence.Heats.Where(heat =>
                         heat.Value.Status == HeatStatus.Casting &&
                         Caster.Strand.TotalCastLengthMeters - heat.Value.CastLengthAtStartMeters >
                         Caster.Torch.TorchLocation))
            {
                heat.Value.Status = HeatStatus.Cutting;
            }
        };

        Caster.Strand.Advanced += _strandAdvancedHandler;
    }

    private void RegisterTorchEvents()
    {
        _torchCutDoneHandler = (s, product) =>
        {
            Console.WriteLine($"Cut done {product.ProductId}, {product.CutLength} meters");
            product.Weight = product.CutLength * product.Width * product.Thickness * _sequence.SteelDensity;
            CutProducts.Enqueue(product);
            
            if (Caster.Strand.Mode == StrandMode.Tailout && !_isScheduleOptimized)
            {
                var optimizedSchedule = CutScheduler.Optimize(
                    Caster.Strand.HeadFromMoldMeters - Caster.Strand.TailFromMoldMeters,
                    new Queue<Product>(_sequence.Products));
                _sequence.Products.ReplaceAll(optimizedSchedule);
                _isScheduleOptimized = true;
                Console.WriteLine($"Schedule Optimized at {DateTime.Now.ToShortTimeString()}");
                foreach (var p in _sequence.Products)
                {
                    Console.WriteLine($"Product {p.ProductId}");
                }
            }
          
            if (!_sequence.Products.TryDequeue(out var nextProduct))
            {
                nextProduct = new Product(Caster.Torch.NextProduct);
                //nextProduct.CutNumber++;
            }

            Console.WriteLine($"Next to cut: {nextProduct.ProductId}");
            Caster.Torch.SetNextProduct(nextProduct);
        };

        Caster.Torch.CutDone += _torchCutDoneHandler;
    }

    private void RegisterLadleEvents()
    {
        if (_ladleOpenedHandler is not null) Caster.Ladle.HeatOut -= _ladleOpenedHandler;
        if (_ladleClosedHandler is not null) Caster.Ladle.ContainerEmptied -= _ladleClosedHandler;

        _ladleOpenedHandler = (s, heatId) => { SetHeatStatus(heatId, HeatStatus.Pouring); };

        _ladleClosedHandler = (s, heatId) => { SetHeatStatus(heatId, HeatStatus.Closed); };

        Caster.Ladle.HeatOut += _ladleOpenedHandler;
        Caster.Ladle.ContainerEmptied += _ladleClosedHandler;
    }

    private void RegisterTundishEvents()
    {
        _tundishWeightThresholdHandler = (s, e) =>
        {
            if (!_sequence.Products.TryDequeue(out var nextProduct))
                throw new Exception($"No product available");
            Caster.Torch.SetNextProduct(nextProduct);
        };

        _tundishHeatOut = (s, heatId) =>
        {
            _sequence.Heats[heatId].CastLengthAtStartMeters = Caster.Strand.TotalCastLengthMeters;
            SetHeatStatus(heatId, HeatStatus.Casting);
        };
        
        Caster.Tundish.WeightThresholdReached += _tundishWeightThresholdHandler;
        Caster.Tundish.HeatOut += _tundishHeatOut;
        Caster.Tundish.ContainerEmptied += _tundishEmptyHandler;
    }

    private void RegisterEvents()
    {
        RegisterCasterEvents();
        RegisterTundishEvents();
        RegisterStrandEvent();
        RegisterTorchEvents();
    }

    private void UnregisterEvents()
    {
        Caster.CastingFinished -= _castingFinishedHandler;
        Caster.Strand.Advanced -= _strandAdvancedHandler;
        Caster.Torch.CutDone -= _torchCutDoneHandler;
        Caster.Ladle.HeatOut -= _ladleOpenedHandler;
        Caster.Ladle.ContainerEmptied -= _ladleClosedHandler;
        Caster.Tundish.WeightThresholdReached -= _tundishWeightThresholdHandler;
        Caster.Tundish.HeatOut -= _tundishHeatOut;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            UnregisterEvents();
            Caster?.Dispose();
        }

        _disposed = true;
    }

    ~Tracking()
    {
        Dispose(false);
    }
}