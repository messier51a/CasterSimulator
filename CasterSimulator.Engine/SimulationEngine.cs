using System;
using System.Threading;
using CasterSimulator.Components;

namespace CasterSimulator.Engine
{
    public class SimulationEngine
    {
        private readonly Tundish tundish;
        private readonly Turret turret;
        private readonly Strand strand;
        private readonly Mold mold;
        private readonly Ladle[] ladles;
        private int currentLadleIndex;

        private readonly double tickIntervalSeconds;
        private readonly double steelDensity = 7850; // kg/m³ (density of steel)
        private bool isRunning;

        private double rampStartSpeed;
        private double rampTargetSpeed;
        private double rampDuration;
        private double rampElapsedTime;
        private bool isRamping;

        private double castSpeed; // Internal field for cast speed
        public double CastSpeed
        {
            get => castSpeed;
            private set => castSpeed = value;
        }

        public SimulationEngine(Ladle[] ladlesArray, Mold moldSimulator, string tundishId, double tickIntervalSeconds = 1.0)
        {
            if (ladlesArray == null || ladlesArray.Length == 0)
                throw new ArgumentException("At least one ladle is required for the simulation.", nameof(ladlesArray));

            ladles = ladlesArray;
            mold = moldSimulator ?? throw new ArgumentNullException(nameof(moldSimulator));
            tundish = new Tundish(tundishId);
            tundish.AddSteel(12000); // Set an initial weight for the tundish
            turret = new Turret(ladles[0]);
            strand = new Strand(mold);
            currentLadleIndex = 0;
            this.tickIntervalSeconds = tickIntervalSeconds;
            isRunning = true;

            RegisterEvents();
        }

        public void Run()
        {
            Console.WriteLine("Starting Steel Casting Simulation...");
            StartSpeedRamp(0.0, 3.0 / 60.0, 30.0); // Ramp from 0 to 3 m/min over 30 seconds
            turret.ActiveLadle.OpenLadle();
            strand.StartCasting();

            while (isRunning)
            {
                Console.WriteLine("\n[SIMULATION TICK]");
                Console.WriteLine("Advancing simulation by 1 second...");

                Tick(tickIntervalSeconds);
                LogState();
                Thread.Sleep((int)(tickIntervalSeconds * 1000)); // Simulate real-time ticks
            }

            Console.WriteLine("Simulation completed successfully.");
        }

        private void Tick(double deltaTimeSeconds)
        {
            // Update speed ramp
            UpdateSpeedRamp(deltaTimeSeconds);

            // Update all components
            mold.Update(deltaTimeSeconds);
            strand.Update(deltaTimeSeconds, CastSpeed);
            turret.Update(deltaTimeSeconds);

            // Pour steel from active ladle into the tundish
            var activeLadle = turret.ActiveLadle;
            activeLadle.Update(deltaTimeSeconds);
            tundish.AddSteel(activeLadle.LastPoured);

            // Drain steel from tundish into the strand
            double drainRate = CalculateMassFlowRate();
            tundish.Drain(deltaTimeSeconds, drainRate);

            // Handle ladle switching
            if (!activeLadle.IsOpen && activeLadle.RemainingSteelWeight <= 0)
            {
                SwitchLadle();
            }

            // Handle second ramp-up based on cast length
            if (strand.CastLength >= 7.0 && CastSpeed < 4.0 / 60.0)
            {
                StartSpeedRamp(3.0 / 60.0, 4.0 / 60.0, 15.0); // Ramp to 4 m/min in 15 seconds
            }

            // Stop simulation if all ladles are used and tundish is empty
            if (currentLadleIndex >= ladles.Length && tundish.CurrentSteelWeight <= 0)
            {
                isRunning = false;
            }
        }

        private void StartSpeedRamp(double startSpeed, double targetSpeed, double durationSeconds)
        {
            rampStartSpeed = startSpeed;
            rampTargetSpeed = targetSpeed;
            rampDuration = durationSeconds;
            rampElapsedTime = 0.0;
            isRamping = true;

            Console.WriteLine($"Starting speed ramp from {startSpeed * 60:F2} m/min to {targetSpeed * 60:F2} m/min over {durationSeconds:F2} seconds.");
        }

        private void UpdateSpeedRamp(double deltaTimeSeconds)
        {
            if (!isRamping)
                return;

            rampElapsedTime += deltaTimeSeconds;

            if (rampElapsedTime >= rampDuration)
            {
                CastSpeed = rampTargetSpeed; // Stabilize at the target speed
                isRamping = false;
                Console.WriteLine($"Speed ramp completed. Stabilized at {CastSpeed * 60:F2} m/min.");
            }
            else
            {
                // Interpolate speed based on elapsed time
                double progress = rampElapsedTime / rampDuration;
                CastSpeed = rampStartSpeed + (rampTargetSpeed - rampStartSpeed) * progress;
            }
        }

        private void SwitchLadle()
        {
            if (currentLadleIndex >= ladles.Length - 1)
                return;

            currentLadleIndex++;
            var nextLadle = ladles[currentLadleIndex];
            turret.LoadLadle(nextLadle);
            turret.StartRotation();
            turret.CompleteRotation();
            turret.ActiveLadle.OpenLadle();
            Console.WriteLine($"Switched to ladle {nextLadle.HeatId}.");
        }

        private double CalculateMassFlowRate()
        {
            double crossSectionalArea = mold.GetCrossSectionalArea();
            return crossSectionalArea * CastSpeed * steelDensity * 2.20462; // kg to lbs
        }

        private void LogState()
        {
            Console.WriteLine(
                $"Ladle Weight: {ladles[0].RemainingSteelWeight:F2} lbs, " +
                $"Mold Level: {mold.MoldLevel:F2} mm, " +
                              $"Tundish Weight: {tundish.CurrentSteelWeight:F2} lbs, " +
                              $"Cast Speed: {CastSpeed * 60:F2} m/min, " +
                              $"Cast Length: {strand.CastLength:F2} m, " +
                              $"Strand Length: {strand.StrandLength:F2} m");
        }

        private void RegisterEvents()
        {
            strand.SlabCut += (s, e) => Console.WriteLine($"Slab cut. Cast Length: {strand.CastLength:F2} m, Strand Length: {strand.StrandLength:F2} m");
            tundish.CastingStart += (s, e) => Console.WriteLine("Casting started as the tundish reached its threshold weight.");
            tundish.CastingEnd += (s, e) =>
            {
                Console.WriteLine("Casting ended as the tundish emptied.");
                strand.StopCasting();
                isRunning = false;
            };
        }
    }
}
