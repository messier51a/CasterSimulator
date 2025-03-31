using System;

namespace CasterSimulator.Components;

/// <summary>
/// Represents a mold in the continuous casting machine.
/// This class defines the mold dimensions and threshold levels but does not manage temperature tracking.
/// </summary>
public class Mold : SteelContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Mold"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for the mold.</param>
    /// <param name="width">Mold width in meters.</param>
    /// <param name="depth">Mold depth (thickness) in meters.</param>
    /// <param name="height">Mold height in meters.</param>
    /// <param name="thresholdLevel">Maximum steel level inside the mold in millimeters.</param>
    public Mold(string id, double width, double depth, double height, double thresholdLevel)
        : base(new ContainerDetails(id)
        {
            Width = width,
            Depth = depth,
            Height = height,
            MaxLevel = thresholdLevel,
            ThresholdLevelMm = 800,
            InitialFlowRate = 0
        })
    {
    }
}