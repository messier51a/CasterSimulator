namespace CasterSimulator.Models;
public class HeatMin(int id, double weight)
{
    public int Id { get; init; } = id;
    public double Weight { get; set; } = weight;
    public string SteelGradeId { get; set; } = string.Empty;
    public double LiquidusTemperatureC { get; set; } 
    public double TargetSuperheatC { get; init; }

    public HeatMin(HeatMin other) : this(other.Id, other.Weight)
    {
        SteelGradeId = other.SteelGradeId;
        LiquidusTemperatureC = other.LiquidusTemperatureC;
        TargetSuperheatC = other.TargetSuperheatC;
    }
}