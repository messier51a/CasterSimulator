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
using Newtonsoft.Json;

namespace CasterSimulator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            var configuration = Configuration.Instance;
            using var casterChannel = new LiveDataChannel("caster", configuration.GrafanaLiveUrl, configuration.GrafanaLiveToken);
            var apiClient = new WebApiClient(configuration.WebApiUrl);
            var overviewSignals = casterChannel.GetSignals("overview");

            Console.WriteLine("=== Steel Casting Simulation ===");

            try
            {
                // Retrieve sequence and initialize simulation engine
                var sequence = Schedule.GetSquence(1.56d, 0.103d, 7850);
                Console.WriteLine($"Total heats {sequence.Heats.Count}");
                using var tracking = new Tracking(sequence);

                tracking.Caster.Tundish.WeightThresholdReached += async (sender, i) =>
                {
                    var success = await apiClient.UpdateCutScheduleAsync(tracking.Products.ToList());
                };

                tracking.HeatStatusChanged += async () =>
                {
                    var success = await apiClient.UpdateHeatScheduleAsync(tracking.Heats.Values.ToList());
                };

                tracking.Products.CollectionChanged += async () =>
                {
                    Console.WriteLine($"📢 CollectionChanged triggered for Products at {DateTime.Now.ToLongTimeString()}. Products count: {tracking.Products.Count}");
                    var success = await apiClient.UpdateCutScheduleAsync(tracking.Products.ToList());
                };


                tracking.CutProducts.CollectionChanged += async () =>
                {
                    var success = await apiClient.UpdateProductsAsync(tracking.CutProducts.ToList());
                };


                // Observable interval for periodic logging
                using var periodicLogger = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                    .Subscribe(async _ =>
                    {
                        overviewSignals.Set("ladle_weight", tracking?.Caster?.Ladle?.NetWeightKgs ?? 0.0);
                        overviewSignals.Set("ladle_flow", tracking?.Caster?.Ladle?.FlowRateKgSec ?? 0.0);
                        overviewSignals.Set("tundish_weight", tracking?.Caster?.Tundish?.NetWeightKgs ?? 0.0);
                        overviewSignals.Set("tundish_level", tracking?.Caster?.Tundish?.LevelMm ?? 0.0);
                        overviewSignals.Set("tundish_temperature", tracking?.Caster?.Tundish?.Temperature ?? 0.0);
                        overviewSignals.Set("tundish_superheat", tracking?.Caster?.Tundish?.SuperheatC ?? 0.0);
                        overviewSignals.Set("tundish_superheat_target", tracking?.Caster?.Tundish?.SuperheatTargetC ?? 0.0);
                        overviewSignals.Set("tundish_flow", tracking?.Caster?.Tundish?.FlowRateKgSec ?? 0.0);
                        overviewSignals.Set("tundish_mixed_steel_pct", tracking?.Caster?.Tundish?.MixedSteelPercent ?? 0.0);
                        overviewSignals.Set("tundish_mixed_steel", (tracking?.Caster?.Tundish?.MixedSteelPercent ?? 0) > 0 ? 1 : 0);
                        overviewSignals.Set("tundish_rod_pos", tracking?.Caster?.Tundish?.StopperRodPositionPercent ?? 0.0);
                        overviewSignals.Set("mold_level", tracking?.Caster?.Mold?.LevelMm ?? 0.0);
                        overviewSignals.Set("mold_flow", tracking?.Caster?.Mold?.FlowRateKgSec ?? 0.0);
                        overviewSignals.Set("total_cast_length", tracking?.Caster?.Strand?.TotalCastLengthMeters ?? 0.0);
                        overviewSignals.Set("cast_speed", tracking?.Caster?.Strand?.CastSpeedMetersMin ?? 0.0);
                        overviewSignals.Set("heat_id", tracking?.Caster?.Ladle?.HeatId ?? 0);
                        overviewSignals.Set("steel_grade", tracking?.Caster?.Ladle?.Heats.SingleOrDefault()?.SteelGradeId ?? string.Empty);
                        overviewSignals.Set("next_cut_id", tracking?.Caster?.Torch.NextProduct?.ProductId ?? string.Empty);
                        overviewSignals.Set("next_cut_length", tracking?.Caster?.Torch.NextProduct?.LengthAimMeters ?? 0.0);
                        overviewSignals.Set("measured_cut_length", tracking?.Caster?.Torch.MeasCutLengthMeters ?? 0.0);
                        overviewSignals.Set("head_position", tracking?.Caster?.Strand?.HeadFromMoldMeters ?? 0.0);
                        overviewSignals.Set("tail_position", tracking?.Caster?.Strand?.TailFromMoldMeters ?? 0.0);

                        var heatsInTundish = tracking.Caster.Tundish.Heats;

                        Enumerable.Range(1, 2).ToList().ForEach(idx =>
                        {
                            var heat = heatsInTundish.ElementAtOrDefault(idx - 1);
                            overviewSignals.Set($"heat_{idx}_id", heat?.Id ?? 0);
                            overviewSignals.Set($"heat_{idx}_weight", heat?.Weight ?? 0);
                        });

                        foreach (var section in tracking.Caster.CoolingSections)
                        {
                            overviewSignals.Set($"cooling_section_{section.Key}", section.Value.CurrentFlowRate);
                        }

                        casterChannel.Push();

                        /*Console.WriteLine($"Strand Mode: {tracking?.Caster?.Strand?.Mode}, " +
                                          $"Heat: {tracking?.Caster?.Ladle?.HeatId}, " +
                                          $"Ladle weight: {tracking?.Caster?.Ladle?.NetWeightKgs:F2}, " +
                                          $"Ladle pour rate: {tracking?.Caster?.Ladle?.FlowRateKgSec:F2}, " +
                                          $"Tundish weight: {tracking?.Caster?.Tundish?.NetWeightKgs:F2}, " +
                                          $"Cast speed: {tracking?.Caster?.Strand?.CastSpeedMetersMin:F2}, " +
                                          $"Cast length inc: {tracking?.Caster?.Strand?.CastLengthIncrement:F2}, " +
                                          $"Cast length: {tracking?.Caster?.Strand?.TotalCastLengthMeters:F2}, " +
                                          $"Measured Length: {tracking?.Caster?.Torch.MeasCutLengthMeters:F2} m. " +
                                          $"Next product {tracking?.Caster?.Torch.NextProduct?.ProductId} length {tracking.Caster?.Torch?.NextProduct?.LengthAimMeters:F2}");

                        foreach (var product in tracking?.CutProducts.ToArray())
                        {
                            Console.WriteLine($"Product {product.ProductId} length {product.CutLength}");
                        }*/
                    });

                // Start the simulation
                Console.WriteLine("\n=== Starting Simulation ===");
                await tracking.StartSequence();
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