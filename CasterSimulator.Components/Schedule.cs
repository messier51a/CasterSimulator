using System.Text.Json;
using CasterSimulator.Models;

namespace CasterSimulator.Components;

/// <summary>
/// Static class that provides scheduling functionality for the casting process.
/// Loads steel grade definitions from JSON configuration file and creates sequences
/// of heats and products for casting simulation.
/// </summary>
/// <remarks>
/// The Schedule class has two important constraints:
/// 
/// 1. Minimum product length: No product can be shorter than 4 meters, which is the minimum 
///    allowable cut length in the simulation.
///    
/// 2. Maximum product length: The maximum product length must be less than (torch position - 4 meters),
///    where 4 meters is the minimum allowable cut length. This ensures that after cutting a product,
///    there is always enough remaining steel in the strand for at least one more cut if the last cut cannot be optimized.
///    For example, with a torch positioned at 15 meters from the mold, the maximum
///    product length cannot exceed 11 meters.
/// </remarks>
public static class Schedule
{
    private static readonly List<SteelGrade>? _steelGrades;
    
    /// <summary>
    /// Gets a dictionary of all available steel grades, indexed by steel grade ID.
    /// Loaded from the steel_grades.json configuration file during static initialization.
    /// </summary>
    public static Dictionary<string,SteelGrade> SteelGrades { get; }
    
    private static double _steelDensity;

    /// <summary>
    /// Static constructor that loads steel grade definitions from the steel_grades.json file.
    /// Initializes the SteelGrades dictionary for lookup by steel grade ID.
    /// </summary>
    static Schedule()
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, "steel_grades.json");
        Console.WriteLine($"Loading steel grades...path {filePath}.");
        var steelGradesDefinition = File.ReadAllText(filePath); 
        _steelGrades = JsonSerializer.Deserialize<List<SteelGrade>>(steelGradesDefinition);
        SteelGrades = _steelGrades.ToDictionary(x => x.SteelGradeId);
    }

    /// <summary>
    /// Creates a new casting sequence with simulated heats and products.
    /// Generates a unique sequence ID based on the current timestamp and populates
    /// the sequence with heats and products according to specified dimensions.
    /// </summary>
    /// <param name="width">The width of products in the sequence, in meters.</param>
    /// <param name="thickness">The thickness of products in the sequence, in meters.</param>
    /// <param name="steelDensity">The density of steel to be used, in kg/m³.</param>
    /// <returns>
    /// A new Sequence object populated with heats and products for simulation.
    /// Includes randomly selected steel grades and calculated product lengths.
    /// </returns>
    public static Sequence GetSquence(double width, double thickness, double steelDensity)
    {
       
        // Format the date and time as an integer in yyyyMMddHHmm
        _steelDensity = steelDensity;
        var sequenceId = long.Parse(DateTime.Now.ToString("yyMMddHHmm"));
        var sequence = new Sequence(sequenceId, width, thickness, steelDensity);
        var totalHeats = 3;//new Random().Next(3, 3);
        var cutCount = 1;
        var heatId = GetMinutesSince2025();
        for (var i = 0; i < totalHeats; i++)
        {
            var heatCount = i.ToString("D2");
            var heatName = $"{sequenceId}-{heatCount}";
            //var heatWeight = new Random().Next(100000, 150000);
            var heatWeight = 20000; //new Random().Next(30000, 50000);

            var heat = new Heat(heatId, heatName, heatWeight, _steelGrades[new Random().Next(_steelGrades.Count)].SteelGradeId);
            sequence.Heats.TryAdd(heatId, heat);
            var productAverageLength = GetRandomProductLength(4, 6);
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

    /// <summary>
    /// Calculates the number of slabs that can be produced from a given amount of steel.
    /// Uses the specified dimensions and density to determine how many complete slabs
    /// can be cut from the heat's weight of steel.
    /// </summary>
    /// <param name="steelInKgs">The total weight of steel available, in kilograms.</param>
    /// <param name="slabWidth">The width of each slab, in meters.</param>
    /// <param name="slabThickness">The thickness of each slab, in meters.</param>
    /// <param name="slabLength">The length of each slab, in meters.</param>
    /// <returns>
    /// The number of slabs that can be produced, rounded up to the nearest integer.
    /// </returns>
    static int CalculateNumberOfSlabs(double steelInKgs, double slabWidth, double slabThickness, double slabLength)
    {
        var slabVolume = slabWidth * slabThickness * slabLength; 
        var slabMass = slabVolume * _steelDensity;

        return (int)Math.Ceiling(steelInKgs / slabMass);
    }

    /// <summary>
    /// Generates a random product length within the specified range.
    /// The length is rounded to the nearest half-meter for practical production values.
    /// </summary>
    /// <param name="min">The minimum length in meters.</param>
    /// <param name="max">The maximum length in meters.</param>
    /// <returns>
    /// A random length value between min and max, rounded to the nearest 0.5 meters.
    /// </returns>
    static double GetRandomProductLength(double min, double max)
    {
        var random = new Random();
        var randomValue = random.NextDouble() * (max - min) + min;
        return Math.Ceiling(randomValue * 2) / 2;
    }

    /// <summary>
    /// Calculates the number of minutes elapsed since January 1, 2025.
    /// Used to generate unique, incrementing heat IDs based on the current time.
    /// </summary>
    /// <returns>
    /// The number of minutes elapsed since the start of 2025.
    /// </returns>
    private static int GetMinutesSince2025()
    {
        var startDate = new DateTime(2025, 1, 1, 0, 0, 0);
        var now = DateTime.Now;
        return (int)(now - startDate).TotalMinutes;
    }
}