using System;

namespace CasterSimulator.Components
{
    public class Mold
    {
        private readonly string moldId; // Unique identifier for the mold
        private readonly double width; // Mold width in meters
        private readonly double thickness; // Mold thickness in meters
        private readonly double height = 1.8; // Fixed mold height in meters
        private readonly Random random; // For simulating mold level fluctuations

        private double moldLevel; // Current steel level in the mold (in mm)

        public string MoldId => moldId;
        public double Width => width; // Expose width
        public double Thickness => thickness; // Expose thickness
        public double Height => height; // Expose height
        public double MoldLevel => moldLevel; // Expose current mold level

        public Mold(string id, double width, double thickness)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Mold ID must be a valid string.", nameof(id));

            if (width <= 0 || thickness <= 0)
                throw new ArgumentException("Width and thickness must be greater than zero.");

            moldId = id;
            this.width = width;
            this.thickness = thickness;
            moldLevel = -182; // Default starting level in millimeters
            random = new Random();
        }

        // Simulates fluctuations in the mold level
        public void Update(double deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0)
                return;

            // Randomly fluctuate the mold level by ±1 mm
            double fluctuation = (random.NextDouble() * 2 - 1); // Random value between -1 and +1
            moldLevel += fluctuation;

            // Clamp the mold level within realistic bounds
            moldLevel = Math.Clamp(moldLevel, -185, -175);
        }

        // Calculates the cross-sectional area of the mold
        public double GetCrossSectionalArea()
        {
            return width * thickness; // Area in square meters
        }
    }
}
