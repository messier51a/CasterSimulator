using System;
using CasterSimulator.Components;
using CasterSimulator.Engine;

namespace CasterSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Steel Casting Simulation...");

            // Define mold dimensions
            var mold = new Mold("Mold20250109", 1.5, 0.2);

            // Define ladles for the sequence
            var ladles = new Ladle[]
            {
                new Ladle(30000, "Heat1"),
                new Ladle(28000, "Heat2"),
                new Ladle(32000, "Heat3")
            };

            // Create the simulation engine
            var simulationEngine = new SimulationEngine(ladles, mold, "Tundish20250109");

            // Run the simulation
            simulationEngine.Run();

            Console.WriteLine("Simulation completed.");
        }
    }
}