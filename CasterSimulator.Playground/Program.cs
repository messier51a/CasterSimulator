using CasterSimulator.Components;
using CasterSimulator.MES;
using CasterSimulator.Models;
using Microsoft.Win32.SafeHandles;

class Program
{
    static void Main(string[] args)
    {
        var sequence = Schedule.GetSquence(1.56,0.103,7850);

        foreach (var heat in sequence.Heats)
        {
            Console.WriteLine($"{heat.Id}-{heat.Name}-{heat.NetWeight}");
        }

        foreach (var product in sequence.Products)
        {
            Console.WriteLine(
                $"{product.ProductId}-{product.CutNumber}-{product.LengthAim}-{product.LengthMin}-{product.LengthMax}");
        }
        
        Console.ReadKey();
        return;

        // Define different scenarios for steel length and cut schedule lengths
        var scenarios = new List<(double steelLength, List<Product> cutSchedule, string description)>
        {
            (127,
                new List<Product>
                {
                    new Product(0, "0001", 200, 100, 300),
                    new Product(1, "0002", 200, 100, 300)
                },
                "Not enough steel for initial schedule"
            ),
            (300,
                new List<Product>
                {
                    new Product(0, "0001", 200, 100, 300),
                    new Product(1, "0002", 200, 100, 300)
                },
                "Not enough steel for initial schedule"
            ),
            (600,
                new List<Product>
                {
                    new Product(0, "0001", 200, 100, 300),
                    new Product(1, "0002", 200, 100, 300),
                    new Product(2, "0003", 200, 100, 300)
                },
                "Exactly enough steel for initial schedule"
            ),
            (802,
                new List<Product>
                {
                    new Product(0, "0001", 200, 100, 300),
                    new Product(1, "0002", 200, 100, 300),
                    new Product(2, "0003", 200, 100, 300),
                    new Product(3, "0004", 200, 100, 300)
                },
                "Small excess of steel"
            ),
            (1500,
                new List<Product>
                {
                    new Product(0, "0001", 200, 100, 300),
                    new Product(1, "0002", 200, 100, 300),
                    new Product(2, "0003", 200, 100, 300),
                    new Product(3, "0004", 200, 100, 300),
                    new Product(4, "0005", 200, 100, 300)
                },
                "Larger excess of steel"
            ),
            (1747,
                new List<Product>
                {
                    new Product(0, "0001", 200, 100, 300),
                    new Product(1, "0002", 200, 100, 300),
                    new Product(2, "0003", 200, 100, 300),
                    new Product(3, "0004", 200, 100, 300),
                    new Product(4, "0005", 200, 100, 300),
                    new Product(5, "0006", 200, 100, 300)
                },
                "Even larger excess of steel"
            ),
        };

        // Test each scenario
        foreach (var (steelLength, cutSchedule, description) in scenarios)
        {
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine($"Scenario: {description}"); // Print the description
            Console.WriteLine($"Steel Length: {steelLength}");
            Console.WriteLine("Cut Schedule:");
            foreach (var product in cutSchedule)
            {
                Console.WriteLine($"{product.ProductId}-{product.LengthAim}");
            }

            var cutScheduleOptimizer = new CutScheduler();

            try
            {
                var newCutSchedule = cutScheduleOptimizer.Optimize(steelLength, cutSchedule);

                Console.WriteLine("Optimized cutting schedule:");
                foreach (var cut in newCutSchedule)
                {
                    Console.WriteLine($"{cut.ProductId}-{cut.LengthAim}");
                }

                Console.WriteLine($"Remaining length: {steelLength - newCutSchedule.Sum(x => x.LengthAim)}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("------------------------------------------------------\n");
        }

        Console.ReadKey();
    }
}