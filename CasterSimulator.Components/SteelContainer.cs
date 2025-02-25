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
    private TaskCompletionSource<bool> _pouringCompletionSource = new();
    private bool _isHeatOut;

    protected readonly ContainerDetails ContainerDetails = containerDetails;
    public event Action<int, double>? SteelPoured;
    public event EventHandler<int>? ContainerEmptied;
    public event EventHandler? WeightThresholdReached;
    public event EventHandler<int>? HeatOut;
    public double FlowRate { get; private set; } 
    public double NetWeight => _heats.Sum(h => h.Weight);
    public double CrossSectionalArea => ContainerDetails.Width * ContainerDetails.Depth;
    public int HeatId => _heats.FirstOrDefault()?.Id ?? 0;
    
    public void AddSteel(int heatId, double weight)
    {
        if (weight > 0)
        {
            if (_heats.All(x => x.Id != heatId))
                _heats.Enqueue(new HeatMin(heatId, weight));
            else
                _heats.First(x => x.Id == heatId).Weight += weight;
        }

        if (!_thresholdReached && NetWeight >= ContainerDetails.ThresholdWeight)
        {
            _thresholdReached = true;
            WeightThresholdReached?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task PourAsync()
    {
       
        FlowRate = ContainerDetails.InitialFlowRate;
        _pouringCompletionSource = new TaskCompletionSource<bool>();

        while (NetWeight > 0)
        {
            RemoveSteel(FlowRate);
            await Task.Delay(1000);
        }

        _pouringCompletionSource.TrySetResult(true);
    }

    public void RemoveSteel(double weight)
    {
        var lastHeatId = 0;
        FlowRate = weight;
        
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
                SteelPoured?.Invoke(frontHeat.Id, frontHeat.Weight);
                _heats.TryDequeue(out _);
                weight -= frontHeat.Weight;
                _isHeatOut = false;
            }
            else
            {
                frontHeat.Weight -= weight;
                SteelPoured?.Invoke(frontHeat.Id, weight);
                break;
            }
        }

        if (NetWeight == 0)
        {
            ContainerEmptied?.Invoke(this, lastHeatId);
        }
    }

    public virtual void SetFlowRate(double flowRate)
    {
        FlowRate = flowRate;
    }

    public double GetLevel()
    {
        var volume = NetWeight / ContainerDetails.SteelDensity;
        return (volume / (CrossSectionalArea)) * 1_000;
    }

    public void Dispose()
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