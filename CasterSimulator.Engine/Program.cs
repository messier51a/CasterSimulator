using System;
using System.Reactive.Linq;
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
                Console.WriteLine("=== Steel Casting Simulation ===");

                // Initialize mold
                Console.WriteLine("\n[Initialization] Setting up mold...");
                var mold = new Mold(MoldId, 1.56, 0.103);
                Console.WriteLine($"Mold initialized with ID: {MoldId}, Width: 1.56 m, Thickness: 0.103 m");

                // Initialize tundish
                Console.WriteLine("\n[Initialization] Setting up tundish...");
                var tundish = new Tundish(TundishId);
                Console.WriteLine($"Tundish initialized with ID: {TundishId}");

                // Initialize ladles
                Console.WriteLine("\n[Initialization] Setting up ladles...");
                var ladles = new Ladle[]
                {
                    new Ladle(30000, "Heat1"),
                    new Ladle(28000, "Heat2"),
                    new Ladle(32000, "Heat3")
                };
                foreach (var ladle in ladles)
                {
                    Console.WriteLine($"Ladle initialized: {ladle.HeatId}, Initial Weight: {ladle.RemainingSteelWeight} lbs");
                }

                // Initialize simulation engine
                Console.WriteLine("\n[Initialization] Setting up casting sequence...");
                var simulationEngine = new CastingSequence(ladles, mold, tundish);

                // Observable interval for periodic output
                var periodicLogger = Observable.Interval(TimeSpan.FromSeconds(3))
                    .Subscribe(_ =>
                    {
                        Console.WriteLine("\n[Process State]");
                        Console.WriteLine($"Current Ladle: {ladles[simulationEngine.CurrentLadleIndex].HeatId}");
                        Console.WriteLine($"Current Ladle Weight: {simulationEngine.CurrentLadleWeight:F2} lbs");
                        Console.WriteLine($"Tundish Weight: {simulationEngine.TundishWeight:F2} lbs");
                        Console.WriteLine($"Strand Length: {simulationEngine.StrandLength:F2} m");
                        Console.WriteLine($"Next Product: {(simulationEngine.NextProduct != null ? simulationEngine.NextProduct.ProductId : "None")}");
                        Console.WriteLine($"Casting Status: {simulationEngine.Status}");
                    });

                // Run simulation
                Console.WriteLine("\n=== Starting Simulation ===");
                simulationEngine.Run();

                periodicLogger.Dispose(); // Stop periodic logging when simulation ends

                Console.WriteLine("\n=== Simulation Completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] An error occurred during the simulation: {ex.Message}");
            }
        }
    }
}
