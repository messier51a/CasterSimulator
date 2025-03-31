using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using CasterSimulator.Common.Collections;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

/// <summary>
/// Manages the tracking of heats, products, and the overall sequence of the casting process.
/// Coordinates the various components and events within the casting system to ensure
/// proper progression of heats through the casting machine and accurate product cutting.
/// </summary>
public class Tracking : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the continuous casting machine instance being tracked.
    /// </summary>
    public Caster Caster { get; private set; }

    private Sequence _sequence;

    private bool _isScheduleOptimized;

    private bool _isLastCut;

    private TaskCompletionSource<bool> _castingFinishedSignal;

    /// <summary>
    /// Gets the dictionary of heats in the current sequence, indexed by heat ID.
    /// </summary>
    public ConcurrentDictionary<int, Heat> Heats => _sequence.Heats;

    /// <summary>
    /// Gets the queue of products that have been cut from the strand.
    /// </summary>
    public ObservableConcurrentQueue<Product> CutProducts { get; private set; } = new();

    /// <summary>
    /// Gets the queue of products scheduled to be cut from the strand.
    /// </summary>
    public ObservableConcurrentQueue<Product?> Products => _sequence.Products;

    private EventHandler _castingFinishedHandler;
    private EventHandler _strandAdvancedHandler;
    private EventHandler<Product> _torchCutDoneHandler;
    private EventHandler<int> _ladleOpenedHandler;
    private EventHandler<int> _ladleClosedHandler;
    private EventHandler _tundishWeightThresholdHandler;
    private EventHandler<int> _tundishHeatOut;
    private EventHandler<int> _moldEmptyHandler;

    /// <summary>
    /// Event triggered when the status of a heat changes during the casting process.
    /// </summary>
    public event Action? HeatStatusChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tracking"/> class.
    /// </summary>
    /// <param name="sequence">The sequence to be tracked, containing heats and products information.</param>
    /// <exception cref="ArgumentNullException">Thrown if sequence is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if sequence ID is zero, or if the sequence contains no heats or products.
    /// </exception>
    public Tracking(Sequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence, nameof(sequence));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Id, nameof(sequence.Id));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Heats.Count, nameof(sequence.Heats));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Products.Count, nameof(sequence.Products));

        _sequence = sequence;

        Caster = new Caster();
        RegisterEvents();
    }

    /// <summary>
    /// Starts the casting sequence, processing heats in order and managing the flow
    /// of steel through the continuous casting machine.
    /// </summary>
    /// <returns>A task that completes when the casting sequence is finished, returning the sequence ID.</returns>
    public async Task<long> StartSequence()
    {
        _isScheduleOptimized = false;

        _castingFinishedSignal = new TaskCompletionSource<bool>();

        Console.WriteLine(_sequence.Heats.Count);

        while (_sequence.Heats.Values.Any(heat => heat.Status == HeatStatus.New))
        {
            var nextHeat = _sequence.Heats.Values
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
        return _sequence.Id;
    }

    /// <summary>
    /// Updates the status of a heat and records relevant timestamps.
    /// </summary>
    /// <param name="heatId">The ID of the heat to update.</param>
    /// <param name="status">The new status to set for the heat.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the status is not a valid HeatStatus value.</exception>
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

    /// <summary>
    /// Registers event handlers for the Caster component.
    /// Handles the completion of the casting process.
    /// </summary>
    private void RegisterCasterEvents()
    {
        _castingFinishedHandler = (s, e) => { _castingFinishedSignal.TrySetResult(true); };
        Caster.CastingFinished += _castingFinishedHandler;
    }

    /// <summary>
    /// Registers event handlers for the Strand component.
    /// Tracks heat boundaries and updates heat statuses as the strand advances.
    /// </summary>
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

    /// <summary>
    /// Registers event handlers for the Torch component.
    /// Handles product cutting, calculates product weights, and manages the cutting schedule.
    /// </summary>
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
                    new Queue<Product?>(_sequence.Products));
                Console.WriteLine($"Replacing original queue - {DateTime.Now.ToLongTimeString()}");
                _sequence.Products.ReplaceAll(optimizedSchedule);
                Console.WriteLine($"Original queue replaced - {DateTime.Now.ToLongTimeString()}");
                _isScheduleOptimized = true;
            }

            Console.WriteLine($"Trying to get next cut from queue - {DateTime.Now.ToLongTimeString()}");
            if (!_sequence.Products.TryDequeue(out var nextProduct))
            {
                Caster.Torch.ResetNextProduct();
                return;
            }

            Console.WriteLine(
                $"Next to cut: {nextProduct.ProductId} - {DateTime.Now.ToLongTimeString()}. Sequence products count {_sequence.Products.Count}");
            Caster.Torch.SetNextProduct(nextProduct);
        };

        Caster.Torch.CutDone += _torchCutDoneHandler;
    }

    /// <summary>
    /// Registers event handlers for the Ladle component.
    /// Tracks when heats start pouring and when ladles are emptied.
    /// </summary>
    private void RegisterLadleEvents()
    {
        if (_ladleOpenedHandler is not null) Caster.Ladle.HeatOut -= _ladleOpenedHandler;
        if (_ladleClosedHandler is not null) Caster.Ladle.ContainerEmptied -= _ladleClosedHandler;

        _ladleOpenedHandler = (s, heatId) => { SetHeatStatus(heatId, HeatStatus.Pouring); };

        _ladleClosedHandler = (s, heatId) => { SetHeatStatus(heatId, HeatStatus.Closed); };

        Caster.Ladle.HeatOut += _ladleOpenedHandler;
        Caster.Ladle.ContainerEmptied += _ladleClosedHandler;
    }

    /// <summary>
    /// Registers event handlers for the Tundish component.
    /// Sets up the initial product cut when the tundish reaches its threshold weight
    /// and tracks when heats start entering the strand.
    /// </summary>
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
    }

    /// <summary>
    /// Registers event handlers for the Mold component.
    /// Currently contains an empty handler for the mold emptied event.
    /// </summary>
    private void RegisterMoldEvents()
    {
        _moldEmptyHandler = (s, e) => { };

        Caster.Mold.ContainerEmptied += _moldEmptyHandler;
    }

    /// <summary>
    /// Registers all event handlers for the various components of the casting system.
    /// </summary>
    private void RegisterEvents()
    {
        RegisterCasterEvents();
        RegisterTundishEvents();
        RegisterMoldEvents();
        RegisterStrandEvent();
        RegisterTorchEvents();
    }

    /// <summary>
    /// Unregisters all event handlers to prevent memory leaks during disposal.
    /// </summary>
    private void UnregisterEvents()
    {
        Caster.CastingFinished -= _castingFinishedHandler;
        Caster.Strand.Advanced -= _strandAdvancedHandler;
        Caster.Torch.CutDone -= _torchCutDoneHandler;
        Caster.Ladle.HeatOut -= _ladleOpenedHandler;
        Caster.Ladle.ContainerEmptied -= _ladleClosedHandler;
        Caster.Tundish.WeightThresholdReached -= _tundishWeightThresholdHandler;
        Caster.Tundish.HeatOut -= _tundishHeatOut;
        Caster.Mold.ContainerEmptied -= _moldEmptyHandler;
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