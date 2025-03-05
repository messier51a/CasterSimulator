using CasterSimulator.Components;
using CasterSimulator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var testResults = new List<string>(); // Store test outcomes

        // Define a single list of products to be cut
        var baseCutSchedule = new List<Product>
        {
            CreateProduct(1, 1, 15, 8, 20),
            CreateProduct(1, 2, 15, 8, 20),
            CreateProduct(1, 3, 15, 8, 20),
            CreateProduct(1, 4, 15, 8, 20),
            CreateProduct(1, 5, 15, 8, 20),
            CreateProduct(1, 6, 15, 8, 17)
            
        };

        // Define different strand lengths with expected outcomes
        var testScenarios = new List<(double steelLength, string description, int expectedCuts, double expectedSteelUsed, bool expectTailCut)>
        {
            (16, "Not enough steel - should cut only one slab within available steel", 1, 16, false),
            (25, "Not enough steel - should fit one full cut and part of another", 2, 25, false),
            (45, "Exactly enough steel for three full cuts", 3, 45, false),
            (63, "Small excess of steel - should fit 4 slabs and adjust the last one", 4, 63, false),
            (92, "Larger excess - should fit all slabs and adjust the last one", 6, 92, false),
            (117, "Even larger excess - should fit all slabs and use remaining steel efficiently", 8, 117, false),
            (33, "Remaining steel less than 4m - should adjust last cut to prevent <4m left", 2, 33, false),
            (93, "Remaining steel less than 4m - cannot adjust last cut to prevent <4m left, tail cut", 7, 93, true)
            
        };

        // Run tests
        foreach (var (steelLength, description, expectedCuts, expectedSteelUsed, expectTailCut) in testScenarios)
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

                // Check if tail cut exists when expected
                bool tailCutExists = newCutSchedule.Any(p => p.ProductId.EndsWith("TAIL"));

                if (cutsMatch && steelMatch && tailCutExists == expectTailCut)
                {
                    testResults.Add($"✅ PASSED: {description} - Cuts: {expectedCuts}, Steel Used: {expectedSteelUsed}m, Remaining: {remainingSteel}m.");
                }
                else
                {
                    string failMessage = $"❌ FAILED: {description} - ";
                    if (!cutsMatch) failMessage += $"Expected {expectedCuts} cuts, but got {newCutSchedule.Count}. ";
                    if (!steelMatch) failMessage += $"Expected {expectedSteelUsed}m steel used, but got {usedSteel}m. ";
                    if (tailCutExists != expectTailCut) failMessage += $"Expected Tail Cut: {expectTailCut}, Found: {tailCutExists}.";
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
        // Updated cut schedule with new LengthAim, LengthMin, and LengthMax
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
