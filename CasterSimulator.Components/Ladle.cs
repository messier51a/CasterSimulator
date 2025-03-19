using CasterSimulator.Models;
using System;

namespace CasterSimulator.Components;

/// <summary>
/// Represents a ladle in the continuous steel casting machine.
/// Simulates turbulence, and models clogging events.
/// </summary>
public class Ladle : SteelContainer
{
    private readonly Random _random = new Random();
    private bool _isClogged = false;
    private int _clogDuration = 0;

    /// <summary>
    /// Gets the current state of the ladle.
    /// </summary>
    public LadleState State { get; private set; } = LadleState.New;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Ladle"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for the ladle.</param>
    public Ladle(string id)
        : base(new ContainerDetails(id)
        {
            Width = 0,
            Depth = 0,
            Height = 0,
            MaxLevel = 0,
            ThresholdLevelMm = 0,
            InitialFlowRate = 300,
            MaxFlowRateKgSec = 300,
        })
    {
    }

    /// <summary>
    /// Sets the flow rate while applying turbulence and clogging effects.
    /// </summary>
    /// <param name="baseFlowRate">The base flow rate in kg/sec.</param>
    public override void SetFlowRate(double baseFlowRate)
    {
        var adjustedFlow = GetAdjustedFlowRate(baseFlowRate);
        base.SetFlowRate(adjustedFlow);
    }

    /// <summary>
    /// Adjusts the flow rate with turbulence, overcorrections, and clogging.
    /// </summary>
    /// <param name="baseFlowRate">The base flow rate before adjustments.</param>
    /// <returns>The adjusted flow rate in kg/sec.</returns>
    private double GetAdjustedFlowRate(double baseFlowRate)
    {
        // Small continuous turbulence (±5%)
        double turbulenceFactor = 1 + (_random.NextDouble() * 0.1 - 0.05);
        double adjustedFlow = baseFlowRate * turbulenceFactor;

        // Occasionally add larger spikes (20-30%) simulating slide gate overcorrections
        if (_random.NextDouble() < 0.05) // 5% chance per step
        {
            adjustedFlow *= 1 + (_random.NextDouble() * 0.3 - 0.15); // ±15% variation
        }

        // Simulate occasional clogging events (temporary 50-80% flow reduction)
        if (_isClogged)
        {
            adjustedFlow *= 0.3 + _random.NextDouble() * 0.5;
            _clogDuration--;
            if (_clogDuration <= 0)
                _isClogged = false;
        }
        else if (_random.NextDouble() < 0.02) // 2% chance per step
        {
            _isClogged = true;
            _clogDuration = _random.Next(3, 7); // Clog lasts 3-7 time steps
        }

        return Math.Max(adjustedFlow, 10); 
    }
}
