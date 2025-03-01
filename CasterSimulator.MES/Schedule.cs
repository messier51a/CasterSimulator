using System.Text.Json;
using System.Text.Json.Serialization;
using CasterSimulator.Models;

namespace CasterSimulator.MES;

public static class Schedule
{
    public static List<SteelGrade> SteelGrades { get; }
    
    private static double _steelDensity;

    static Schedule()
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "steel_grades.json");
        Console.WriteLine($"Loading steel grades...path {filePath}.");
        var steelGradesDefinition = File.ReadAllText(filePath); 
        SteelGrades = JsonSerializer.Deserialize<List<SteelGrade>>(steelGradesDefinition);
    }

    public static Sequence GetSquence(double width, double thickness, double steelDensity)
    {
       
        // Format the date and time as an integer in yyyyMMddHHmm
        _steelDensity = steelDensity;
        var sequenceId = long.Parse(DateTime.Now.ToString("yyMMddHHmm"));
        var sequence = new Sequence(sequenceId, width, thickness, steelDensity);
        var totalHeats = new Random().Next(3, 3);
        var cutCount = 0;
        var heatId = GetMinutesSince2024();
        for (var i = 0; i < totalHeats; i++)
        {
            var heatCount = i.ToString("D2");
            var heatName = $"{sequenceId}-{heatCount}";
            //var heatWeight = new Random().Next(100000, 150000);
            var heatWeight = new Random().Next(30000, 50000);

            var heat = new Heat(heatId, heatName, heatWeight, SteelGrades[new Random().Next(SteelGrades.Count)].SteelGradeId);
            sequence.Heats.TryAdd(heatId, heat);
            var productAverageLength = GetRandomProductLength(5, 8);
            var totalEstimatedSlabs = CalculateNumberOfSlabs(
                heat.NetWeight,
                width,
                thickness,
                productAverageLength);

            Console.WriteLine($"Total Estimated Slabs: {totalEstimatedSlabs}");

            for (var j = 0; j < totalEstimatedSlabs; j++)
            {
                var productId = sequenceId + cutCount.ToString("D2");

                var product = new Product(sequenceId, cutCount, productId, productAverageLength,
                    productAverageLength - (productAverageLength * 0.1),
                    productAverageLength + (productAverageLength * 0.1));

                sequence.Products.Enqueue(product);

                cutCount++;
            }

            heatId++;

            Thread.Sleep(100);
        }

        return sequence;
    }

    static int CalculateNumberOfSlabs(double steelInKgs, double slabWidth, double slabThickness, double slabLength)
    {
        // Calculate the volume of one slab in cubic meters
        var slabVolume = slabWidth * slabThickness * slabLength; // Dimensions are in meters

        // Steel density (assumed to be 7850 kg/m³)


        // Calculate the mass of one slab in kgs
        var slabMass = slabVolume * _steelDensity;

        // Calculate the number of slabs that can be produced
        return (int)(steelInKgs / slabMass);
    }

    static double GetRandomProductLength(double min, double max)
    {
        var random = new Random();
        var randomValue = random.NextDouble() * (max - min) + min;
        return Math.Ceiling(randomValue * 2) / 2;
    }

    private static int GetMinutesSince2024()
    {
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var now = DateTime.Now;
        return (int)(now - startDate).TotalMinutes;
    }
}