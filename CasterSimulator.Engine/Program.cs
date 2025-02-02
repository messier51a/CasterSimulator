using System;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using CasterSimulator.Components;
using CasterSimulator.Engine;
using CasterSimulator.Models;
using System.Threading.Tasks;
using CasterSimulator.Streaming;
using Newtonsoft.Json;

namespace CasterSimulator
{
    class Program
    {
        static async Task Main(string[] args)
        {
      
            var _url = "http://localhost:3000/api/live/push";
            var _token = "glsa_2fhbu1izgcxGkzjZO93ceJfMrLVtWPhf_771b924f"; // Replace this
            var channel = new Channel("overview", _url, _token);
            var signals = channel.GetSignals("s1");

            Console.WriteLine("=== Steel Casting Simulation ===");

            try
            {
                // Retrieve sequence and initialize simulation engine
                var sequence = MES.Schedule.GetSquence(1.56d, 0.103d, 7850);
                Console.WriteLine($"Total heats {sequence.Heats.Count}");
                using var tracking = new Tracking();


                // Observable interval for periodic logging
                using var periodicLogger = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                    .Subscribe(async _ =>
                    {

                        
                        signals.Set($"{nameof(Ladle.NetWeight)}", tracking?.Caster?.Ladle?.NetWeight);
                        signals.Set($"{nameof(Tundish.NetWeight)}", tracking?.Caster?.Tundish?.NetWeight);
                        signals.Set($"{nameof(Strand.TotalCastLength)}", tracking?.Caster?.Strand?.TotalCastLength);
                        signals.Set($"{nameof(Strand.CastSpeed)}", tracking?.Caster?.Strand?.CastSpeed);
                        signals.Update();
                        
                        Console.WriteLine($"Strand Mode: {tracking?.Caster?.Strand?.Mode}, " +
                                          $"Heat: {tracking?.Caster?.Ladle?.Heat?.Id}, " +
                                          $"Ladle weight: {tracking?.Caster?.Ladle?.NetWeight:F2}, " +
                                          $"Ladle pour rate: {tracking?.Caster?.Ladle?.PouringRate:F2}, " +
                                          $"Tundish weight: {tracking?.Caster?.Tundish?.NetWeight:F2}, " +
                                          $"Cast speed: {tracking?.Caster?.Strand?.CastSpeed:F2}, " +
                                          $"Cast length inc: {tracking?.Caster?.Strand?.CastLengthIncrement:F2}, " +
                                          $"Cast length: {tracking?.Caster?.Strand?.TotalCastLength:F2}, " +
                                          $"Measured Length: {tracking?.Caster?.Torch.MeasuredCutLength:F2} m. " +
                                          $"Next product {tracking?.Caster?.Torch.NextProduct?.ProductId} length {tracking.Caster?.Torch?.NextProduct?.LengthAim:F2}");

                        foreach (var product in tracking?.CutProducts)
                        {
                            Console.WriteLine($"Product {product.ProductId} length {product.CutLength}");
                        }
                    });

                // Start the simulation
                Console.WriteLine("\n=== Starting Simulation ===");
                await tracking.StartSequence(sequence);
                Console.WriteLine("\n=== Simulation Completed ===");
                foreach (var heat in tracking.Heats)
                {
                    Console.WriteLine(
                        $"Heat {heat.Id}, start: {heat.HeatStartUtcTime}, end: {heat.HeatEndUtcTime}, status: {heat.Status}");
                }

                Console.WriteLine(
                    $"Total heats {sequence.Heats.Count}, total weight {sequence.Heats.Sum(x => x.NetWeight):F2}");
                Console.WriteLine(
                    $"Total products: {tracking.CutProducts.Count}, total weight: {tracking.CutProducts.Sum(x => x.Weight):F2} ");

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                // Log error with stack trace for debugging purposes
                Console.WriteLine($"\n[Error] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}