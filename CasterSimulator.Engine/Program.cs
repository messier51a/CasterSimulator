using System;
using System.Reactive.Linq;
using CasterSimulator.Components;
using CasterSimulator.Engine;
using CasterSimulator.Models;
using System.Threading.Tasks;

namespace CasterSimulator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Steel Casting Simulation ===");

            try
            {
                // Retrieve sequence and initialize simulation engine
                var sequence = MES.Schedule.GetSquence();
                Console.WriteLine($"Total heats {sequence.Heats.Count}");
                var simulationEngine = new CastingSequence(sequence);

                // Observable interval for periodic logging
                using var periodicLogger = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                    .Subscribe(_ =>
                    {
                        Console.WriteLine("[Process State]");
                        Console.WriteLine($"Current Ladle: {simulationEngine.Ladle?.HeatId}");
                        Console.WriteLine($"Current Ladle Weight: {simulationEngine.Ladle?.RemainingSteelWeight:F2} kg");
                        Console.WriteLine($"Ladle Flow Rate: {simulationEngine.Ladle?.PouringRate:F2} kg/s");
                        Console.WriteLine($"Tundish Weight: {simulationEngine.Tundish.CurrentSteelWeight:F2} kg");
                        Console.WriteLine($"Cast Speed: {simulationEngine.Strand.CastSpeed:F2} m/min");
                        Console.WriteLine($"Cast Length: {simulationEngine.Strand.TotalCastLength:F2} m");
                        Console.WriteLine($"Strand Length: {simulationEngine.Strand.HeadDistanceFromMold:F2} m");
                        Console.WriteLine($"Tail Offset: {simulationEngine.Strand.TailDistanceFromMold:F2} m");
                        Console.WriteLine($"Next Product: {simulationEngine.NextProduct?.ProductId ?? "None"}");
                        Console.WriteLine($"Strand Mode: {simulationEngine.StrandMode}");
                        
                        foreach (var heat in simulationEngine.Heats)
                        {
                            Console.WriteLine($"Heat {heat.Key}: Boundary: {heat.Value.HeatBoundary:F2} m, Mix Zone: Start: {heat.Value.MixZoneStart:F2} m to End: {heat.Value.MixZoneEnd:F2} m");
                        }
                    });

                // Start the simulation
                Console.WriteLine("\n=== Starting Simulation ===");
                await simulationEngine.StartAsync();

                Console.WriteLine("\n=== Simulation Completed ===");
            }
            catch (Exception ex)
            {
                // Log error with stack trace for debugging purposes
                Console.WriteLine($"\n[Error] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
