using System.Collections.Concurrent;
using CasterSimulator.Models;

namespace CasterSimulator.Components;

/// <summary>
/// Base class for all steel containers (Ladle, Tundish, Mold).
/// Handles steel storage, pouring, and event management.
/// Provides functionality for tracking heats, managing flow rates,
/// and calculating steel levels based on container dimensions.
/// 
/// Implements the "tundish 50% rule" for mixed steel tracking - a simplified
/// model where when a new heat enters a container with existing steel, 50% of the
/// existing steel is considered "mixed" with the new heat. This simulates the 
/// real-world phenomenon where heats partially mix at interfaces rather than
/// remaining perfectly separated. Mixed steel tracking is important for quality 
/// control and predicting transition zones between heats in the final product.
/// </summary>
public abstract class SteelContainer(ContainerDetails containerDetails) : IDisposable
{
    private bool _disposed;
    private bool _thresholdReached;
    private readonly ConcurrentQueue<HeatMin> _heats = new();

    /// <summary>
    /// Gets all heats currently in the container, ordered by ID.
    /// </summary>
    public HeatMin[] Heats => _heats.OrderBy(x => x.Id).ToArray();

    private TaskCompletionSource<bool> _pouringCompletionSource = new();
    private bool _isHeatOut;

    /// <summary>
    /// Container specification details including dimensions and flow rate limits.
    /// </summary>
    protected readonly ContainerDetails ContainerDetails = containerDetails;

    private double _mixedSteelWeightKgs;

    /// <summary>
    /// Percentage of mixed steel in the container based on the 50% tundish weight rule.
    /// Represents the amount of steel that is a mixture of different heats.
    /// </summary>
    public double MixedSteelPercent => NetWeightKgs > 0 ? (_mixedSteelWeightKgs / NetWeightKgs) * 100 : 0;

    /// <summary>
    /// Event triggered when steel is poured from this container.
    /// Provides the heat information that was poured.
    /// </summary>
    public event Action<HeatMin>? SteelPoured;

    /// <summary>
    /// Event triggered when the container becomes empty.
    /// Provides the ID of the last heat that was in the container.
    /// </summary>
    public event EventHandler<int>? ContainerEmptied;

    /// <summary>
    /// Event triggered when new steel is added to the container.
    /// Provides the ID of the heat that was added.
    /// </summary>
    public event EventHandler<int>? NewSteelAdded;

    /// <summary>
    /// Event triggered when the steel level reaches the threshold level defined in ContainerDetails.
    /// </summary>
    public event EventHandler? WeightThresholdReached;

    /// <summary>
    /// Event triggered when a heat starts flowing out of the container.
    /// Provides the ID of the heat that is flowing out.
    /// </summary>
    public event EventHandler<int>? HeatOut;

    /// <summary>
    /// Gets the current flow rate of steel out of the container in kilograms per second.
    /// </summary>
    public double FlowRateKgSec { get; private set; }

    /// <summary>
    /// Gets the total weight of all steel currently in the container in kilograms.
    /// </summary>
    public double NetWeightKgs => _heats.Sum(h => h.Weight);

    /// <summary>
    /// Gets the unique identifier for this container.
    /// </summary>
    public string Id => ContainerDetails.Id;

    /// <summary>
    /// Gets the maximum allowable flow rate for this container in kilograms per second.
    /// </summary>
    public double MaxFlowRateKgSec => ContainerDetails.MaxFlowRateKgSec;

    /// <summary>
    /// Gets the steel level in millimeters based on net weight, steel density, and cross-sectional area.
    /// Converts volumetric measurements to a linear height measurement.
    /// </summary>
    public double LevelMm => (NetWeightKgs / ContainerDetails.SteelDensity) / CrossSectionalArea * 1_000;

    /// <summary>
    /// Gets the cross-sectional area of the container in square meters.
    /// Used for level calculations based on volume.
    /// </summary>
    public double CrossSectionalArea => ContainerDetails.Width * ContainerDetails.Depth;

    /// <summary>
    /// Gets the ID of the first heat in the container, or 0 if the container is empty.
    /// </summary>
    public int HeatId => _heats.FirstOrDefault()?.Id ?? 0;

    /// <summary>
    /// Adds a new heat (including weight and heat id) to the container.
    /// Handles mixed steel tracking and triggers appropriate events.
    /// </summary>
    /// <param name="heat">The heat to add to the container.</param>
    /// <exception cref="ArgumentNullException">Thrown if heat is null.</exception>
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

    /// <summary>
    /// Asynchronously pours steel at the initial flow rate until the container is empty.
    /// Steel is removed at 1-second intervals based on the current flow rate.
    /// </summary>
    /// <returns>A task that completes when the container is empty.</returns>
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

    /// <summary>
    /// Removes a specified amount of steel from the container.
    /// Handles heat tracking, mixed steel calculations, and triggers appropriate events.
    /// Steel is removed from the first heat in the queue first, then subsequent heats if needed.
    /// </summary>
    /// <param name="weight">The amount of steel to remove in kilograms.</param>
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

    /// <summary>
    /// Sets the steel flow rate out of the container.
    /// Has no effect if the container is empty.
    /// </summary>
    /// <param name="flowRate">The new flow rate in kilograms per second.</param>
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