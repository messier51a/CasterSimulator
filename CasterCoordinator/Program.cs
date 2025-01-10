using System;
using System.Threading;

namespace SteelCastingSimulation
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting Steel Casting Simulation...");
            
            double castWidth = 1.56; // meters
            double castThickness = 0.103; // meters

            var ladles = new LadleSimulator[]
            {
                new LadleSimulator(30000, "20250109-Heat1"),
                new LadleSimulator(28000, "20250109-Heat2"),
                new LadleSimulator(32000, "20250109-Heat3")
            };
            Console.WriteLine($"Initialized {ladles.Length} ladles.");

            var coordinator = new CastingCoordinator(ladles, castWidth, castThickness);

            coordinator.StartSpeedRamp(0.0, 3.0 / 60.0, 30.0); // Ramp to 3 m/min in 30 seconds
            coordinator.StartCasting();
            Console.WriteLine("Coordinator started. First ladle is pouring...");

            while (coordinator.IsRunning)
            {
                Console.WriteLine("Advancing simulation by 1 second...");

                Console.WriteLine($"Ladle: {coordinator.GetActiveLadle().HeatId}, " +
                                  $"Remaining Weight: {coordinator.GetActiveLadle().RemainingSteelWeight:F2} lbs, " +
                                  $"Tundish Weight: {coordinator.GetTundishWeight():F2} lbs, " +
                                  $"Cast Speed: {coordinator.CastSpeed * 60:F2} m/min, " +
                                  $"Cast Length: {coordinator.Strand.CastLength:F2} m, " +
                                  $"Strand Length: {coordinator.Strand.StrandLength:F2} m");

                var status = coordinator.Tick(1.0);
                
                VerifyCalculations(coordinator, 1.0); // Verify calculations for this tick

                if (status == CoordinatorStatus.RotateTurret)
                {
                    Console.WriteLine("Active ladle is empty. Preparing to rotate turret...");
                    int rotateTime = RandomRotationTime();
                    ShowProgressBar(rotateTime);

                    coordinator.CompleteRotation();
                    Console.WriteLine("New ladle is pouring steel into the tundish.");
                }
                else if (status == CoordinatorStatus.Finished)
                {
                    Console.WriteLine("All ladles are used, and the tundish is empty. Simulation finished.");
                    break;
                }

                Thread.Sleep(1000);
            }

            Console.WriteLine("Simulation completed successfully.");
        }

        static int RandomRotationTime()
        {
            Random rnd = new Random();
            return rnd.Next(30, 91); // Random time between 30 and 90 seconds
        }

        static void ShowProgressBar(int totalSeconds)
        {
            Console.WriteLine("Turret is rotating...");
            int barLength = 40;

            for (int i = 1; i <= totalSeconds; i++)
            {
                double progress = (double)i / totalSeconds;
                int filled = (int)(progress * barLength);

                Console.CursorLeft = 0;
                Console.Write("[");
                Console.Write(new string('#', filled));
                Console.Write(new string('-', barLength - filled));
                Console.Write($"] {i}s/{totalSeconds}s");

                Thread.Sleep(1000);
            }
            Console.WriteLine("\nRotation complete.");
        }
        
        private static void VerifyCalculations(CastingCoordinator coordinator, double deltaTimeSeconds)
        {
            var ladle = coordinator.GetActiveLadle();
            double expectedLadleWeight = ladle.InitialWeight - coordinator.GetTundishWeight();
            double calculatedCastLength = coordinator.GetCastLength();
            double calculatedDrainRate = coordinator.CastSpeed * coordinator.CastWidth * coordinator.CastThickness * 7850; // Density of steel

            Console.WriteLine("\n[VERIFICATION]");
            Console.WriteLine($"Expected Ladle Weight: {expectedLadleWeight:F2} lbs, Actual: {ladle.RemainingSteelWeight:F2} lbs");
            Console.WriteLine($"Expected Tundish Weight: {ladle.LastPoured - calculatedDrainRate * deltaTimeSeconds:F2} lbs, Actual: {coordinator.GetTundishWeight():F2} lbs");
            Console.WriteLine($"Expected Cast Speed: {coordinator.CastSpeed * 60:F2} m/min");
            Console.WriteLine($"Expected Cast Length Increase: {(coordinator.CastSpeed * deltaTimeSeconds):F2} m, Actual Cast Length: {calculatedCastLength:F2} m");
        }

    }
}
