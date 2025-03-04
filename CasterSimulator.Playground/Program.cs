using CasterSimulator.Components;
using CasterSimulator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var testResults = new List<string>(); // Store both passed and failed tests

        // Define a single list of products to be cut
        var baseCutSchedule = new List<Product>
        {
            CreateProduct(1, 0, 200, 100, 300),
            CreateProduct(1, 1, 200, 100, 300),
            CreateProduct(1, 2, 200, 100, 300),
            CreateProduct(1, 3, 200, 100, 300),
            CreateProduct(1, 4, 200, 100, 300),
            CreateProduct(1, 5, 200, 100, 300)
        };

        // Define different strand lengths with expected outcomes
        var testScenarios = new List<(double steelLength, string description, int expectedCuts, double expectedSteelUsed)>
        {
            (127, "Not enough steel - should cut only one slab within available steel", 1, 127),
            (300, "Not enough steel - should fit one full cut and part of another", 2, 300),
            (600, "Exactly enough steel for three full cuts", 3, 600),
            (802, "Small excess of steel - should fit 4 slabs and adjust the last one", 4, 800),
            (1500, "Larger excess - should fit all slabs and adjust the last one", 6, 1500),
            (1747, "Even larger excess - should fit all slabs and use remaining steel efficiently", 6, 1747)
        };

        // Run tests with different strand lengths
        foreach (var (steelLength, description, expectedCuts, expectedSteelUsed) in testScenarios)
        {
            // Clone the base cut schedule to avoid modifying the original list
            var cutSchedule = baseCutSchedule.Select(p => new Product(p)).ToList();

            try
            {
                var newCutSchedule = CutScheduler.Optimize(steelLength, cutSchedule);

                double usedSteel = newCutSchedule.Sum(x => x.LengthAimMeters);
                double remainingSteel = Math.Max(0, steelLength - usedSteel);

                bool cutsMatch = newCutSchedule.Count == expectedCuts;
                bool steelMatch = Math.Abs(usedSteel - expectedSteelUsed) < 1e-6;

                if (cutsMatch && steelMatch)
                {
                    testResults.Add($"✅ PASSED: {description} - Expected Cuts: {expectedCuts}, Actual Cuts: {newCutSchedule.Count}, Steel Used: {expectedSteelUsed}m, Remaining: {remainingSteel}m.");
                }
                else
                {
                    string failMessage = $"❌ FAILED: {description} - ";
                    if (!cutsMatch) failMessage += $"Expected {expectedCuts} cuts, but got {newCutSchedule.Count}. ";
                    if (!steelMatch) failMessage += $"Expected {expectedSteelUsed}m steel used, but got {usedSteel}m.";
                    testResults.Add(failMessage);
                }
            }
            catch (Exception ex)
            {
                testResults.Add($"⚠️ ERROR: {description} - Unexpected error: {ex.Message}");
            }
        }

        // Emit final test report
        Console.WriteLine("\n========== TEST SUMMARY ==========");
        foreach (var result in testResults)
        {
            Console.WriteLine(result);
        }
        Console.WriteLine("==================================");

        Console.ReadKey();
    }

    // Helper method to create a product with all required properties
    static Product CreateProduct(long sequenceId, int cutNumber, double lengthAim, double lengthMin, double lengthMax)
    {
        return new Product(sequenceId, cutNumber, Guid.NewGuid().ToString(), lengthAim, lengthMin, lengthMax)
        {
            CutLength = lengthAim,
            Width = 1.56,
            Thickness = 0.103,
            Weight = lengthAim * 1.56 * 0.103 * 7850 // Calculate weight based on density
        };
    }
}
