using System;

namespace CasterSimulator.Components
{
    public class Mold
    {
        private readonly Random _random; // For simulating mold level fluctuations
        public string MoldId { get; }
        public double Width { get; }

        public double Thickness { get; }

        public double MoldLevel { get; private set; }

        /// <summary>
        /// Represents a mold used in the continuous casting process.
        /// </summary>
        /// <param name="id">The unique identifier for the mold. Cannot be null, empty, or whitespace.</param>
        /// <param name="width">The width of the mold in meters. Must be greater than zero.</param>
        /// <param name="thickness">The thickness of the mold in millimeters. Must be greater than zero.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="id"/> is invalid or if <paramref name="width"/> or <paramref name="thickness"/> are not greater than zero.</exception>
        public Mold(string id, double width, double thickness)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Mold ID must be a valid string.", nameof(id));

            if (width <= 0 || thickness <= 0)
                throw new ArgumentException("Width and thickness must be greater than zero.");

            MoldId = id;
            Width = width;
            Thickness = thickness;
            MoldLevel = -182; // Default starting level in millimeters
            _random = new Random();
        }


        // Simulates fluctuations in the mold level
        public void Update(double deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0)
                return;

            // Randomly fluctuate the mold level by ±1 mm
            var fluctuation = (_random.NextDouble() * 2 - 1); // Random value between -1 and +1
            MoldLevel += fluctuation;

            // Clamp the mold level within realistic bounds
            MoldLevel = Math.Clamp(MoldLevel, -185, -175);
        }

        /// <summary>
        /// Calculates the cross-sectional area of the mold.
        /// </summary>
        /// <returns>The cross-sectional area in square meters.</returns>
        public double GetCrossSectionalArea()
        {
            return Width * Thickness;
        }

    }
}
