
using System.Reactive.Linq;
using CasterSimulator.Components;
using CasterSimulator.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace CasterSimulator.Engine
{
    class Program
    {
        static async Task Main(string[] args)
        {

            Configuration.Load();
            
            var metricsPublisher = new MetricsPublisher("caster");
            
            var apiClient = new WebApiClient(Configuration.WebApi.BaseUrl);

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

                // Use the metrics publisher instance for all registrations
                metricsPublisher.RegisterMetric("ladle_weight", () => tracking.Caster.Ladle.NetWeightKgs, "overview");
                metricsPublisher.RegisterMetric("ladle_flow", () => tracking.Caster.Ladle.FlowRateKgSec, "overview");
                metricsPublisher.RegisterMetric("tundish_weight", () => tracking.Caster.Tundish.NetWeightKgs, "overview");
                metricsPublisher.RegisterMetric("tundish_level", () => tracking.Caster.Tundish.LevelMm, "overview");
                metricsPublisher.RegisterMetric("tundish_temperature", () => tracking.Caster.Tundish.Temperature, "overview");
                metricsPublisher.RegisterMetric("tundish_superheat", () => tracking.Caster.Tundish.SuperheatC, "overview");
                metricsPublisher.RegisterMetric("tundish_superheat_target", () => tracking.Caster.Tundish.SuperheatTargetC, "overview");
                metricsPublisher.RegisterMetric("tundish_flow", () => tracking.Caster.Tundish.FlowRateKgSec, "overview");
                metricsPublisher.RegisterMetric("tundish_mixed_steel_pct", () => tracking.Caster.Tundish.MixedSteelPercent, "overview");
                metricsPublisher.RegisterMetric("tundish_mixed_steel", () => tracking.Caster.Tundish.MixedSteelPercent > 0 ? 1 : 0, "overview");
                metricsPublisher.RegisterMetric("tundish_rod_pos", () => tracking.Caster.Tundish.StopperRodPositionPercent, "overview");
                metricsPublisher.RegisterMetric("mold_level", () => tracking.Caster.Mold.LevelMm, "overview");
                metricsPublisher.RegisterMetric("mold_flow", () => tracking.Caster.Mold.FlowRateKgSec, "overview");
                metricsPublisher.RegisterMetric("total_cast_length", () => tracking.Caster.Strand.TotalCastLengthMeters, "overview");
                metricsPublisher.RegisterMetric("cast_speed", () => tracking.Caster.Strand.CastSpeedMetersMin, "overview");
                metricsPublisher.RegisterMetric("heat_id", () => tracking.Caster.Ladle.HeatId, "overview");
                metricsPublisher.RegisterMetric("steel_grade", () =>tracking?.Caster?.Ladle?.Heats.SingleOrDefault()?.SteelGradeId ?? "NA", "overview");
                metricsPublisher.RegisterMetric("next_cut_id", () => tracking?.Caster?.Torch.NextProduct?.ProductId ?? "NA", "overview");
                metricsPublisher.RegisterMetric("next_cut_length", () => tracking.Caster.Torch.NextProduct?.LengthAimMeters ?? 0.0, "overview");
                metricsPublisher.RegisterMetric("measured_cut_length", () => tracking.Caster.Torch.MeasCutLengthMeters, "overview");
                metricsPublisher.RegisterMetric("head_position", () => tracking.Caster.Strand.HeadFromMoldMeters, "overview");
                metricsPublisher.RegisterMetric("tail_position", () => tracking.Caster.Strand.TailFromMoldMeters, "overview");

                foreach (var idx in Enumerable.Range(1, 2))
                {
                    metricsPublisher.RegisterMetric($"heat_{idx}_id", () =>
                    {
                        var heat = tracking.Caster.Tundish.Heats.ElementAtOrDefault(idx - 1);
                        return heat?.Id ?? 0;
                    }, "overview");

                    metricsPublisher.RegisterMetric($"heat_{idx}_weight", () =>
                    {
                        var heat = tracking.Caster.Tundish.Heats.ElementAtOrDefault(idx - 1);
                        return heat?.Weight ?? 0;
                    }, "overview");
                }

                foreach (var section in tracking.Caster.CoolingSections)
                {
                    metricsPublisher.RegisterMetric($"cooling_section_{section.Key}", () => section.Value.CurrentFlowRate, "overview");
                }

                using var periodicLogger = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                    .Subscribe(async _ => await metricsPublisher.Push());


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