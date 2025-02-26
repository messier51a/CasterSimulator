using System;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Represents a tundish, which receives molten steel from the ladle and transfers it to the mold.
    /// </summary>
    public class Tundish : SteelContainer
    {
        public double StopperRodPositionPercent => Math.Clamp((FlowRateKgSec / ContainerDetails.MaxFlowRateKgSec) * 100.0, 0, 100);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Tundish"/> class.
        /// </summary>
        /// <param name="id">Unique identifier for the tundish.</param>
        /// <param name="width">Tundish width in meters.</param>
        /// <param name="depth">Tundish depth in meters.</param>
        /// <param name="maxLevel">Maximum steel level inside the tundish in millimeters.</param>
        /// <param name="thresholdLevel">Weight at which pouring starts, in kilograms.</param>
        /// <param name="autoPour">Determines whether the tundish automatically pours when the threshold is reached.</param>
        /// <param name="flowRate">Steel pouring rate in kilograms per second.</param>
        public Tundish(string id, double thresholdLevel)
            : base(new ContainerDetails(id)
            {
                Width = 3.876,
                Depth = 1.550,
                MaxLevel = 1.181,
                ThresholdLevelMm = 127,
                InitialFlowRate = 30,
                MaxFlowRateKgSec = 150,
            })
        {
        }
        
    }
}