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
using CasterSimulator.Simulator.Services;
using CasterSimulator.Streaming;
using CasterSimulator.Utils.Extensions;
using Newtonsoft.Json;

namespace CasterSimulator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var _url = "http://localhost:3000/api/live/push";
            var _token = "glsa_2fhbu1izgcxGkzjZO93ceJfMrLVtWPhf_771b924f"; // Replace this
            using var casterChannel = new LiveDataChannel("caster", _url, _token);

            var apiClient = new WebApiClient("http://localhost:5087");
            var overviewSignals = casterChannel.GetSignals("overview");

            Console.WriteLine("=== Steel Casting Simulation ===");

            try
            {
                // Retrieve sequence and initialize simulation engine
                var sequence = MES.Schedule.GetSquence(1.56d, 0.103d, 7850);
                Console.WriteLine($"Total heats {sequence.Heats.Count}");
                using var tracking = new Tracking();

                tracking.Caster.Tundish.WeightThresholdReached += async (sender, i) =>
                {
                    var success = await apiClient.UpdateCutScheduleAsync(tracking.Products.ToList());
                };

                tracking.HeatStatusChanged += async () =>
                {
                    var success = await apiClient.UpdateHeatScheduleAsync(tracking.Heats.Values.ToList());
                };

                sequence.Products.CollectionChanged += async () =>
                {
                    var success = await apiClient.UpdateCutScheduleAsync(sequence.Products.ToList());
                };

                // Observable interval for periodic logging
                using var periodicLogger = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                    .Subscribe(async _ =>
                    {
                        overviewSignals.Set("ladle_weight", tracking?.Caster?.Ladle?.NetWeight);
                        overviewSignals.Set("tundish_weight", tracking?.Caster?.Tundish?.NetWeight);
                        overviewSignals.Set("total_cast_length", tracking?.Caster?.Strand?.TotalCastLength);
                        overviewSignals.Set("cast_speed", tracking?.Caster?.Strand?.CastSpeed);
                        overviewSignals.Set("heat_id", tracking?.Caster?.Ladle?.Heat?.Id);
                        overviewSignals.Set("next_cut_id", tracking?.Caster?.Torch.NextProduct?.ProductId);
                        overviewSignals.Set("next_cut_length", tracking?.Caster?.Torch.NextProduct?.LengthAim);
                        overviewSignals.Set("measured_cut_length", tracking?.Caster?.Torch.MeasuredCutLength);
                        overviewSignals.Set("head_position", tracking?.Caster?.Strand.HeadDistanceFromMold);

                        var heats = tracking.Heats.Values.ToArray();
                        
                        for (int idx = 0; idx < 5; idx++)
                        {
                            
                            var heatBoundary = (idx < heats.Length) ? heats[idx]?.HeatBoundary : -999;
                            var heatId = (idx < heats.Length) ? heats[idx]?.Id : 0;
                            overviewSignals.Set($"heat_{idx}_boundary", heatBoundary);
                            overviewSignals.Set($"heat_{idx}_id", heatId);
                        }

                        casterChannel.Push();

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

                        foreach (var product in tracking?.CutProducts.ToArray())
                        {
                            Console.WriteLine($"Product {product.ProductId} length {product.CutLength}");
                        }
                    });

                // Start the simulation
                Console.WriteLine("\n=== Starting Simulation ===");
                await tracking.StartSequence(sequence);
                Console.WriteLine("\n=== Simulation Completed ===");
                foreach (var heat in tracking.Heats.Values.ToArray())
                {
                    Console.WriteLine(
                        $"Heat {heat.Id}, start: {heat.HeatOpenTimeUtc}, end: {heat.HeatCloseTimeUtc}, status: {heat.Status}");
                }

                Console.WriteLine(
                    $"Total heats {sequence.Heats.Count}, total weight {sequence.Heats.Values.ToArray().Sum(x => x.NetWeight):F2}");
                Console.WriteLine(
                    $"Total products: {tracking.CutProducts.Count}, total weight: {tracking.CutProducts.ToArray().Sum(x => x.Weight):F2} ");

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