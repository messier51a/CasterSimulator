using System;
using CasterSimulator.Components;
using CasterSimulator.Engine;

namespace CasterSimulator
{
    class Program
    {
        private const string MoldId = "Mold20250109";
        private const string TundishId = "Tundish20250109";

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Initializing Steel Casting Simulation...");

                var mold = new Mold(MoldId, 1.56, 0.103);
                var tundish = new Tundish(TundishId);
                var ladles = new Ladle[]
                {
                    new Ladle(30000, "Heat1"),
                    new Ladle(28000, "Heat2"),
                    new Ladle(32000, "Heat3")
                };

                // Create the simulation engine
                var simulationEngine = new CastingSequence(ladles, mold, tundish);

                // Run the simulation
                simulationEngine.Run();

                Console.WriteLine("Simulation completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during the simulation: {ex.Message}");
            }
        }
    }
}