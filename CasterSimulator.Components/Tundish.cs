using System;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Represents a tundish, which receives molten steel from the ladle and transfers it to the mold.
    /// </summary>
    public class Tundish : SteelContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tundish"/> class.
        /// </summary>
        /// <param name="id">Unique identifier for the tundish.</param>
        /// <param name="width">Tundish width in meters.</param>
        /// <param name="depth">Tundish depth in meters.</param>
        /// <param name="maxLevel">Maximum steel level inside the tundish in millimeters.</param>
        /// <param name="thresholdWeight">Weight at which pouring starts, in kilograms.</param>
        /// <param name="autoPour">Determines whether the tundish automatically pours when the threshold is reached.</param>
        /// <param name="flowRate">Steel pouring rate in kilograms per second.</param>
        public Tundish(string id, double thresholdWeight)
            : base(new ContainerDetails(id, false)
            {
                Width = 3,
                Depth = 1.5,
                MaxLevel = 1.35,
                ThresholdWeight = thresholdWeight,
                InitialFlowRate = 30
            })
        {
        }
    }
}