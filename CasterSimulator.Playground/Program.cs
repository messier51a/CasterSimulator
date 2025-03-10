using CasterSimulator.Common.Collections;
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

        // Define a base list of products to be cut
        var baseCutSchedule = new List<Product?>
        {
            CreateProduct(1, 1, "P01", 15, 8, 20),
            CreateProduct(1, 2, "P02", 15, 8, 20),
            CreateProduct(1, 3, "P03", 15, 8, 20),
            CreateProduct(1, 4, "P04", 15, 8, 20),
            CreateProduct(1, 5, "P05", 15, 8, 20),
            CreateProduct(1, 6, "P06", 15, 8, 17)
        };

        // Define test scenarios
        var testScenarios = new List<(double steelLength, string description, int expectedCuts, double expectedSteelUsed, bool expectTailCut)>
        {
            (6.3, "Not enough steel, one slab scheduled - should cut only one slab with available steel", 1, 6.3, false),
            (16, "Not enough steel - should cut only one slab within available steel", 1, 16, false),
            (25, "Not enough steel - should fit one full cut and part of another", 2, 25, false),
            (45, "Exactly enough steel for three full cuts", 3, 45, false),
            (63, "Small excess of steel - should fit 4 slabs and adjust the last one", 4, 63, false),
            (92, "Larger excess - should fit all slabs and adjust the last one", 6, 92, false),
            (117, "Even larger excess - should fit all slabs and use remaining steel efficiently", 8, 117, false),
            (33, "Remaining steel less than 4m - should adjust last cut to prevent <4m left", 2, 33, false),
            (93, "Remaining steel less than 4m - cannot adjust last cut to prevent <4m left, tail cut", 7, 93, true),
            (96, "Remaining steel more than 4m - cannot adjust last cut", 7, 96, false)
        };

        // Run tests
        foreach (var (steelLength, description, expectedCuts, expectedSteelUsed, expectTailCut) in testScenarios)
        {
            // Clone the base cut schedule into a new ObservableConcurrentQueue for each test
            var cutSchedule = new ObservableConcurrentQueue<Product>(baseCutSchedule.Select(p => new Product(p)));

            try
            {
                // Optimize directly (modifies the queue in place)
                CutScheduler.Optimize(steelLength, cutSchedule);

                // Analyze the results
                double usedSteel = cutSchedule.Sum(x => x.LengthAimMeters);
                double remainingSteel = Math.Max(0, steelLength - usedSteel);
                bool cutsMatch = cutSchedule.Count == expectedCuts;
                bool steelMatch = Math.Abs(usedSteel - expectedSteelUsed) < 1e-6;

                // Check if tail cut exists when expected
                bool tailCutExists = cutSchedule.Any(p => p.ProductId.EndsWith("TAIL"));

                if (cutsMatch && steelMatch && tailCutExists == expectTailCut)
                {
                    testResults.Add($"✅ PASSED: {description} - Cuts: {expectedCuts}, Steel Used: {expectedSteelUsed}m, Remaining: {remainingSteel}m.");
                }
                else
                {
                    string failMessage = $"❌ FAILED: {description} - ";
                    if (!cutsMatch) failMessage += $"Expected {expectedCuts} cuts, but got {cutSchedule.Count}. ";
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
    static Product? CreateProduct(long sequenceId, int cutNumber, string productId, double lengthAim, double lengthMin, double lengthMax)
    {
        return new Product(sequenceId, cutNumber, productId, lengthAim, lengthMin, lengthMax)
        {
            ProductId = productId,
            CutLength = lengthAim,
            Width = 1.56,
            Thickness = 0.103,
            Weight = lengthAim * 1.56 * 0.103 * 7850 // Calculate weight based on density
        };
    }
}
