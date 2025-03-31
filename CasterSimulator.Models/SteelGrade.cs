namespace CasterSimulator.Models;

public class SteelGrade
{
    public string SteelGradeId { get; set; }
    
    public string SteelGradeGroup { get; set; }
    public double LiquidusTemperatureC { get; set; }
    
    public string Description { get; set; }
    
    public double TargetSuperheatC  { get; set; }
    
    public List<ChemicalElement> Chemistry { get; set; }
}

public class ChemicalElement
{
    public string ElementName { get; set; }
    
    public double Percentage { get; set; }
}