using CasterSimulator.Models;

namespace CasterSimulator.MES;

public static class Schedule
{
    private static string[] _grades = new string[] { "X1000", "X1020", "T601", "T602" };

    public static Sequence GetSquence()
    {
        // Format the date and time as an integer in yyyyMMddHHmm

        var sequenceId = long.Parse(DateTime.Now.ToString("yyMMddHHmm"));
        var sequence = new Sequence(sequenceId);
        var totalHeats = new Random().Next(3, 8);
        var cutCount = 0;
        var heatId = GetSecondsSince2024();
        for (var i = 0; i < totalHeats; i++)
        {
            var heatCount = i.ToString("D2");
            var heatName = $"{sequenceId}-{heatCount}";
            var heatWeight = new Random().Next(160000, 200000);

            var heat = new Heat(heatId, heatName, heatWeight, _grades[0]);
            sequence.Heats.Enqueue(heat);
            var productAverageLength = GetRandomProductLength(16, 20);
            var totalEstimatedSlabs = CalculateNumberOfSlabs(
                heat.NetWeight,
                1.56d,
                0.103d,
                productAverageLength);

            Console.WriteLine($"Total Estimated Slabs: {totalEstimatedSlabs}");

            for (var j = 0; j < totalEstimatedSlabs; j++)
            {
                var productId = sequenceId + cutCount.ToString("D2");

                var product = new Product(cutCount, productId, productAverageLength,
                    productAverageLength - (productAverageLength * 0.1),
                    productAverageLength + (productAverageLength * 0.1));

                sequence.Products.Enqueue(product);

                cutCount++;
            }

            heatId++;
        }

        return sequence;
    }

    static int CalculateNumberOfSlabs(double steelInKgs, double slabWidth, double slabThickness, double slabLength)
    {
        // Calculate the volume of one slab in cubic meters
        var slabVolume = slabWidth * slabThickness * slabLength; // Dimensions are in meters

        // Steel density (assumed to be 7850 kg/m³)
        double steelDensity = 7850;

        // Calculate the mass of one slab in kgs
        var slabMass = slabVolume * steelDensity;

        // Calculate the number of slabs that can be produced
        return (int)(steelInKgs / slabMass);
    }

    static double GetRandomProductLength(double min, double max)
    {
        var random = new Random();
        var randomValue = random.NextDouble() * (max - min) + min;
        return Math.Ceiling(randomValue * 2) / 2;
    }

    private static int GetSecondsSince2024()
    {
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var now = DateTime.Now;
        return (int)(now - startDate).TotalSeconds;
    }
}