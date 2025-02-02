using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public class Metrics
{
    public Metrics()
    {
    }

    public StrandMode StrandMode { get; set; }
    public Heat[]? Heats { get; set; }
    public double? CastSpeed { get; set; }
    public double? CastLength { get; set; }
    public double? TundishWeight { get; set; }
    public double? LadlePourRate { get; set; }
    public double? LadleWeight { get; set; }
    public double? MeasuredCutLength { get; set; }
    public Product? NextProduct { get; set; }
    
}