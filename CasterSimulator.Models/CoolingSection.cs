using System.Text.Json.Serialization;

namespace CasterSimulator.Models;

public class CoolingSection
{
    public int Id { get; set; }
    public double PositionFactor { get; set; }
    public double StartPosition { get; set; }
    
    public double EndPosition { get; set; }
    
    public List<Nozzle> Nozzles { get; set; } 
    
    [JsonIgnore]
    public double CurrentFlowRate { get; set; }
}

public class Nozzle
{
    public string Type { get; set; } = string.Empty;
        
    public double Position { get; set; }
}