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
                    new Ladle(16000, "Heat1"),
                    new Ladle(18000, "Heat2"),
                    new Ladle(14500, "Heat3")
                };
                foreach (var ladle in ladles)
                {
                    Console.WriteLine($"Ladle initialized: {ladle.HeatId}, Initial Weight: {ladle.RemainingSteelWeight} kg");
                }

                // Initialize simulation engine
                Console.WriteLine("\n[Initialization] Setting up casting sequence...");
                var simulationEngine = new CastingSequence(ladles, mold, tundish);

                // Observable interval for periodic output
                var periodicLogger = Observable.Interval(TimeSpan.FromSeconds(3))
                    .Subscribe(_ =>
                    {
                        Console.WriteLine("\n[Process State]");
                        Console.WriteLine($"Current Ladle: {simulationEngine.CurrentLadle.HeatId}");
                        Console.WriteLine($"Current Ladle Weight: {simulationEngine.CurrentLadle.RemainingSteelWeight:F2} kg");
                        Console.WriteLine($"Ladle Flow Rate: {simulationEngine.CurrentLadle.PouringRate:F2} kg/s"); // Approx flow rate
                        Console.WriteLine($"Tundish Weight: {simulationEngine.TundishWeight:F2} kg");
                        Console.WriteLine($"Cast Speed: {simulationEngine.CastSpeed:F2} m/min");
                        Console.WriteLine($"Cast Length: {simulationEngine.CastLength:F2} m");
                        Console.WriteLine($"Strand Length: {simulationEngine.StrandLength:F2} m");
                        Console.WriteLine($"Next Product: {(simulationEngine.NextProduct != null ? simulationEngine.NextProduct.ProductId : "None")}");
                        Console.WriteLine($"Casting Status: {simulationEngine.Status}");

                        foreach (var segment in simulationEngine.HeatSegments)
                        {
                            Console.WriteLine($"Heat {segment.HeatId}: Boundary: {segment.HeatBoundary:F2} m, Mix Zone: Start: {segment.MixZoneStart:F2} m to End: {segment.MixZoneEnd:F2} m");
                        }
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
