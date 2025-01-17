using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices.JavaScript;
using CasterSimulator.Components;
using CasterSimulator.Engine;
using CasterSimulator.Models;

namespace CasterSimulator
{
    class Program
    {
        private const double SimulationIntervalSeconds = 0.2;
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
                
                var heats = new Heat[]
                {
                    new Heat("Heat1", 18000, DateTime.UtcNow.AddHours(-8), "Grade1"),
                    new Heat("Heat2", 18000, DateTime.UtcNow.AddHours(-9), "Grade1"),
                    new Heat("Heat3", 18000, DateTime.UtcNow.AddHours(-10), "Grade1"),
                };
                foreach (var heat in heats)
                {
                    Console.WriteLine(
                        $"Ladle initialized: {heat.Id}, Initial Weight: {heat.NetWeight} kg");
                }

                // Initialize simulation engine
                Console.WriteLine("\n[Initialization] Setting up casting sequence...");
               
                var simulationEngine = new CastingSequence(ladles, mold, tundish);

                // Observable interval for periodic output
                var periodicLogger = Observable.Interval(TimeSpan.FromMilliseconds(200))
                    .Subscribe(_ =>
                    {
                        Console.WriteLine("\n[Process State]");
                        Console.WriteLine($"Current Ladle: {simulationEngine.CurrentLadle.HeatId}");
                        Console.WriteLine(
                            $"Current Ladle Weight: {simulationEngine.CurrentLadle.RemainingSteelWeight:F2} kg");
                        Console.WriteLine(
                            $"Ladle Flow Rate: {simulationEngine.CurrentLadle.PouringRate:F2} kg/s"); // Approx flow rate
                        Console.WriteLine($"Tundish Weight: {simulationEngine.TundishWeight:F2} kg");
                        Console.WriteLine($"Cast Speed: {simulationEngine.Strand.CastSpeed:F2} m/min");
                        Console.WriteLine($"Cast Length: {simulationEngine.Strand.TotalCastLength:F2} m");
                        Console.WriteLine($"Strand Length: {simulationEngine.StrandLength:F2} m");
                        Console.WriteLine($"Tail Offset: {simulationEngine.Strand.TailDistanceFromMold:F2} m");
                        Console.WriteLine(
                            $"Next Product: {(simulationEngine.NextProduct != null ? simulationEngine.NextProduct.ProductId : "None")}");
                        Console.WriteLine($"Casting Status: {simulationEngine.Status}");

                        foreach (var segment in simulationEngine.HeatSegments)
                        {
                            Console.WriteLine(
                                $"Heat {segment.HeatId}: Boundary: {segment.HeatBoundary:F2} m, Mix Zone: Start: {segment.MixZoneStart:F2} m to End: {segment.MixZoneEnd:F2} m");
                        }
                    });

                // Run simulation
                Console.WriteLine("\n=== Starting Simulation ===");
                simulationEngine.Run(SimulationIntervalSeconds);

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