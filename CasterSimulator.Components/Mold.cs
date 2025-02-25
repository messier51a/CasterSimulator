using System;

namespace CasterSimulator.Components;

/// <summary>
/// Represents a mold where molten steel solidifies into slabs.
/// </summary>
public class Mold : SteelContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Mold"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for the mold.</param>
    /// <param name="width">Mold width in meters.</param>
    /// <param name="depth">Mold depth (thickness) in meters.</param>
    /// <param name="thresholdLevel">Maximum steel level inside the mold in millimeters.</param>
    /// <param name="autoPour">Determines whether the mold automatically pours when full.</param>
    /// <param name="flowRate">Steel pouring rate in kilograms per second.</param>
    public Mold(string id, double width, double depth, double height, double thresholdLevel)
        : base(new ContainerDetails(id)
        {
            Width = width,
            Depth = depth,
            Height = height,
            MaxLevel = thresholdLevel,
            ThresholdWeight = thresholdLevel * width * depth * 7850, // Steel density assumption
            InitialFlowRate = 0
        })
    {
    }
}