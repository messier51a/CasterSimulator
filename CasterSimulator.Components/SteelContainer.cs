using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CasterSimulator.Models;

namespace CasterSimulator.Components;

public abstract class SteelContainer(ContainerDetails containerDetails) : IDisposable
{
    private bool _disposed;
    private bool _thresholdReached;
    private readonly ConcurrentQueue<HeatMin> _heats = new();
    public HeatMin[] Heats => _heats.OrderBy(x=>x.Id).ToArray();
    private TaskCompletionSource<bool> _pouringCompletionSource = new();
    private bool _isHeatOut;

    protected readonly ContainerDetails ContainerDetails = containerDetails;
    private double _mixedSteelWeightKgs;
    public double MixedSteelPercent => NetWeightKgs > 0 ? (_mixedSteelWeightKgs / NetWeightKgs) * 100 : 0;

    public event Action<HeatMin>? SteelPoured;
    public event EventHandler<int>? ContainerEmptied;

    public event EventHandler<int>? NewSteelAdded;
    public event EventHandler? WeightThresholdReached;
    public event EventHandler<int>? HeatOut;
    public double FlowRateKgSec { get; private set; }
    public double NetWeightKgs => _heats.Sum(h => h.Weight);
    public string Id => ContainerDetails.Id;
    public double MaxFlowRateKgSec => ContainerDetails.MaxFlowRateKgSec;

    public double LevelMm
    {
        get
        {
            var volume = NetWeightKgs / ContainerDetails.SteelDensity;
            return (volume / (CrossSectionalArea)) * 1_000;
        }
    }

    public double CrossSectionalArea => ContainerDetails.Width * ContainerDetails.Depth;
    public int HeatId => _heats.FirstOrDefault()?.Id ?? 0;

    public virtual void AddSteel(HeatMin heat)
    {
        ArgumentNullException.ThrowIfNull(heat);
        
        var weight = heat.Weight;
        if (weight > 0)
        {
            if (_heats.All(x => x.Id != heat.Id))
            {
                if (!_heats.IsEmpty)
                    _mixedSteelWeightKgs = NetWeightKgs * 0.5;
                _heats.Enqueue(new HeatMin(heat));
                NewSteelAdded?.Invoke(this, heat.Id);
            }

            else
            {
                _heats.First(x => x.Id == heat.Id).Weight += weight;
            }
        }

        if (!_thresholdReached && LevelMm >= ContainerDetails.ThresholdLevelMm)
        {
            _thresholdReached = true;
            WeightThresholdReached?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task PourAsync()
    {
        FlowRateKgSec = ContainerDetails.InitialFlowRate;
        _pouringCompletionSource = new TaskCompletionSource<bool>();

        while (NetWeightKgs > 0)
        {
            RemoveSteel(FlowRateKgSec);
            await Task.Delay(1000);
        }

        _pouringCompletionSource.TrySetResult(true);
    }

    public void RemoveSteel(double weight)
    {
        var lastHeatId = 0;
        FlowRateKgSec = weight;
        var initialWeight = NetWeightKgs;
        while (weight > 0 && _heats.TryPeek(out var frontHeat))
        {
            lastHeatId = frontHeat.Id;

            if (!_isHeatOut)
            {
                HeatOut?.Invoke(this, frontHeat.Id);
                _isHeatOut = true;
            }

            if (frontHeat.Weight <= weight)
            {
                SteelPoured?.Invoke(frontHeat);
                _heats.TryDequeue(out _);
                weight -= frontHeat.Weight;
                _isHeatOut = false;
            }
            else
            {
                frontHeat.Weight -= weight;
                SteelPoured?.Invoke(new HeatMin(frontHeat) { Weight = weight });
                break;
            }
        }

        if (_mixedSteelWeightKgs > 0)
        {
            var weightRemoved = initialWeight - NetWeightKgs;
            _mixedSteelWeightKgs = Math.Max(0, _mixedSteelWeightKgs - weightRemoved);
        }

        if (NetWeightKgs == 0)
        {
            FlowRateKgSec = 0;
            ContainerEmptied?.Invoke(this, lastHeatId);
        }
    }

    public virtual void SetFlowRate(double flowRate)
    {
        if (NetWeightKgs == 0) return;
        FlowRateKgSec = flowRate;
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~SteelContainer()
    {
        Dispose();
    }
}