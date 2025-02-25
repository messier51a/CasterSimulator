using CasterSimulator.Models;
using System;

namespace CasterSimulator.Components;

public class Ladle : SteelContainer
{
    private readonly Random _random = new Random();
    private bool _isClogged = false;
    private int _clogDuration = 0;

    public LadleState State { get; private set; } = LadleState.New;

    public double MaxFlowRate => ContainerDetails.MaxFlowRate;
    public string Id => ContainerDetails.Id;

    public Ladle(string id)
        : base(new ContainerDetails(id)
        {
            Width = 0,
            Depth = 0,
            Height = 0,
            MaxLevel = 0,
            ThresholdWeight = 0,
            InitialFlowRate = 300,
            MaxFlowRate = 300,
        })
    {
    }

    public override void SetFlowRate(double baseFlowRate)
    {
        var adjustedFlow = GetAdjustedFlowRate(baseFlowRate);
        base.SetFlowRate(adjustedFlow);
    }

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
            adjustedFlow *= 0.3 + _random.NextDouble() * 0.5; // Reduce to 30-80% of normal flow
            _clogDuration--;
            if (_clogDuration <= 0)
                _isClogged = false;
        }
        else if (_random.NextDouble() < 0.02) // 2% chance per step
        {
            _isClogged = true;
            _clogDuration = _random.Next(3, 7); // Clog lasts 3-7 time steps
        }

        return Math.Max(adjustedFlow, 10); // Ensure a minimum flow
    }
}