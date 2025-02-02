using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public class Tracking : IDisposable
{
    private bool _disposed;
    public Caster Caster { get; private set; }
    private Dictionary<int, Heat> _heats = new();
    private Sequence _sequence;
    private TaskCompletionSource<bool> _castingFinishedSignal;
    public List<Product> CutProducts { get; private set; } = [];
    public Heat[] Heats => _heats.Values.ToArray();

    private EventHandler _castingFinishedHandler;
    private EventHandler _strandAdvancedHandler;
    private EventHandler<Product> _torchCutDoneHandler;
    private EventHandler<int> _ladleOpenedHandler;
    private EventHandler<int> _ladleClosedHandler;
    private EventHandler _tundishWeightThresholdHandler;
    private EventHandler<int> _tundishHeatOnStrandHandler;

    public async Task<long> StartSequence(Sequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence, nameof(sequence));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Id, nameof(sequence.Id));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Heats.Count, nameof(sequence.Heats));
        ArgumentOutOfRangeException.ThrowIfZero(sequence.Products.Count, nameof(sequence.Products));
        
        _sequence = sequence;
       
        _castingFinishedSignal = new TaskCompletionSource<bool>();

        Caster = new Caster(
            new Configuration(),
            new Tundish("Tundish001", 6000),
            new Mold("Mold001", 1.56, 0.103));
        
        RegisterEvents();

        Console.WriteLine(sequence.Heats.Count);
        
        while (sequence.Heats.Count > 0)
        {
            var nextHeat = sequence.Heats.Dequeue();
            nextHeat.Status = HeatStatus.Next;
            _heats.Add(nextHeat.Id, nextHeat);
            var ladle = new Ladle("L001", nextHeat);
            Caster.Turret.AddLadle(ladle);
            await Caster.Turret.Rotate();
            if (Caster.Turret.LadleInCastPosition.Id != ladle.Id)
                throw new Exception($"Ladle {ladle.Id} is not in cast position.");
            RegisterLadleEvents();
            var heatId = await Caster.Turret.LadleInCastPosition.PourSteel(300);
        }

        await _castingFinishedSignal.Task;
        return sequence.Id;
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
            foreach (var heat in _heats.Where(heat => heat.Value.Status == HeatStatus.Cutting))
            {
                heat.Value.Status = HeatStatus.Cast;
            }

            foreach (var heat in _heats.Where(heat =>
                         heat.Value.Status == HeatStatus.Casting &&
                         Caster.Strand.TotalCastLength - heat.Value.CastLengthAtStartMeters >
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
            product.Weight = product.CutLength * product.Width * product.Thickness * _sequence.SteelDensity;
            CutProducts.Add(product.Clone());
            var nextProduct = _sequence.Products.Dequeue();
            Caster.Torch.SetNextProduct(nextProduct);
        };

        Caster.Torch.CutDone += _torchCutDoneHandler;
    }

    private void RegisterLadleEvents()
    {
        if (_ladleOpenedHandler is not null) Caster.Ladle.LadleOpened -= _ladleOpenedHandler;
        if (_ladleClosedHandler is not null) Caster.Ladle.LadleClosed -= _ladleClosedHandler;
        
        _ladleOpenedHandler = (s, heatId) =>
        {
            _heats[heatId].Open();
            Caster.Tundish.AddHeat(_heats[heatId]);
        };

        _ladleClosedHandler = (s, heatId) => { _heats[heatId].Close(); };

        Caster.Ladle.LadleOpened += _ladleOpenedHandler;
        Caster.Ladle.LadleClosed += _ladleClosedHandler;
    }

    private void RegisterTundishEvents()
    {
        _tundishWeightThresholdHandler = (s, e) => { Caster.Torch.SetNextProduct(_sequence.Products.Dequeue()); };

        _tundishHeatOnStrandHandler = (s, heatId) =>
        {
            _heats[heatId].CastLengthAtStartMeters = Caster.Strand.TotalCastLength;
            _heats[heatId].Status = HeatStatus.Casting;
        };

        Caster.Tundish.WeightThresholdReached += _tundishWeightThresholdHandler;
        Caster.Tundish.HeatOnStrand += _tundishHeatOnStrandHandler;
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
        Caster.Ladle.LadleOpened -= _ladleOpenedHandler;
        Caster.Ladle.LadleClosed -= _ladleClosedHandler;
        Caster.Tundish.WeightThresholdReached -= _tundishWeightThresholdHandler;
        Caster.Tundish.HeatOnStrand -= _tundishHeatOnStrandHandler;
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