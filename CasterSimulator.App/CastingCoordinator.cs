using System;

namespace SteelCastingSimulation
{
    public class CastingCoordinator
    {
        private TundishSimulator tundish;
        private TurretSimulator turret;
        private StrandSimulator strand;
        private LadleSimulator[] ladles;
        private int currentLadleIndex;
        private bool allLadlesDone;

        private readonly double steelDensity = 7850; // kg/m³
        public double CastWidth { get; private set; } // meters
        public double CastThickness { get; private set; } // meters
        public double CastSpeed { get; private set; } // meters/second (variable)

        private double rampStartSpeed;
        private double rampTargetSpeed;
        private double rampDuration;
        private double rampElapsedTime;

        public bool IsRunning { get; private set; } = true;
        public StrandSimulator Strand => strand;

        public CastingCoordinator(LadleSimulator[] ladlesArray, double castWidth, double castThickness)
        {
            tundish = new TundishSimulator();
            strand = new StrandSimulator();
            ladles = ladlesArray;
            turret = new TurretSimulator(ladles[0]);
            CastWidth = castWidth;
            CastThickness = castThickness;
            CastSpeed = 0.0; // Start at zero speed
            currentLadleIndex = 0;

            foreach (var ladle in ladles)
            {
                ladle.LadleOpened += OnLadleOpened;
                ladle.LadleClosed += OnLadleClosed;
            }

            strand.SlabCut += OnSlabCut;
            tundish.CastingStart += OnCastingStart;
            tundish.CastingEnd += OnCastingEnd;
        }

        public void StartCasting()
        {
            turret.ActiveLadle.OpenLadle();
            strand.StartCasting();
        }

        public CoordinatorStatus Tick(double deltaTimeSeconds)
        {
            // Update speed ramp
            UpdateSpeedRamp(deltaTimeSeconds);

            // Update strand
            strand.Update(deltaTimeSeconds, CastSpeed);

            var activeLadle = turret.ActiveLadle;

            // Update ladle pouring steel into the tundish
            activeLadle.Update(deltaTimeSeconds);

            // Add steel from ladle to tundish
            tundish.AddSteel(activeLadle.LastPoured);

            // Calculate and drain tundish
            double drainRate = CalculateMassFlowRate();
            tundish.Drain(deltaTimeSeconds, drainRate);

            if (activeLadle.RemainingSteelWeight <= 0 && !activeLadle.IsOpen)
            {
                if (currentLadleIndex < ladles.Length - 1)
                {
                    currentLadleIndex++;
                    return CoordinatorStatus.NeedsRotation;
                }
                else
                {
                    allLadlesDone = true;
                }
            }

            if (!IsRunning)
                return CoordinatorStatus.Finished;

            return CoordinatorStatus.Normal;
        }

        public void CompleteRotation()
        {
            turret.LoadLadle(ladles[currentLadleIndex]);
            turret.CompleteRotation();
            turret.ActiveLadle.OpenLadle();
        }

        public LadleSimulator GetActiveLadle() => turret.ActiveLadle;

        public double GetTundishWeight() => tundish.CurrentSteelWeight;

        public void StartSpeedRamp(double startSpeed, double targetSpeed, double duration)
        {
            rampStartSpeed = startSpeed;
            rampTargetSpeed = targetSpeed;
            rampDuration = duration;
            rampElapsedTime = 0.0;
        }

        private void UpdateSpeedRamp(double deltaTimeSeconds)
        {
            if (rampElapsedTime < rampDuration)
            {
                rampElapsedTime += deltaTimeSeconds;
                double t = Math.Min(rampElapsedTime / rampDuration, 1.0);
                CastSpeed = rampStartSpeed + t * (rampTargetSpeed - rampStartSpeed);
            }
        }

        private double CalculateMassFlowRate()
        {
            return CastWidth * CastThickness * CastSpeed * steelDensity;
        }

        private void OnSlabCut(object sender, EventArgs e)
        {
            Console.WriteLine($"Slab cut performed. Cast Length: {strand.CastLength:F2} meters, Strand Length reset to {strand.StrandLength:F2} meters.");
        }

        private void OnLadleOpened(object sender, EventArgs e)
        {
            var ladle = (LadleSimulator)sender;
            Console.WriteLine($"Ladle {ladle.HeatId} opened.");
        }

        private void OnLadleClosed(object sender, EventArgs e)
        {
            var ladle = (LadleSimulator)sender;
            Console.WriteLine($"Ladle {ladle.HeatId} closed.");
        }

        private void OnCastingStart(object sender, EventArgs e)
        {
            Console.WriteLine("Tundish CastingStart event fired (threshold reached).");
        }

        private void OnCastingEnd(object sender, EventArgs e)
        {
            Console.WriteLine("Tundish CastingEnd event fired (tundish emptied).");
            strand.StopCasting();
            IsRunning = false;
        }
    }
}
