/// <summary>
/// Represents the configuration details of a steel container.
/// </summary>
public class ContainerDetails
{
    public ContainerDetails(string id)
    {
        Id = id;
    }
    public string Id { get; init; }
    public double Width { get; set; }
    public double Depth { get; set; }
    public double Height { get; set; }
    public double MaxLevel { get; set; }
    public double ThresholdLevelMm { get; set; }
    public double InitialFlowRate { get; set; }
    public double MaxFlowRateKgSec { get; set; }
    public double SteelDensity { get; set; } = 7850;
    
}