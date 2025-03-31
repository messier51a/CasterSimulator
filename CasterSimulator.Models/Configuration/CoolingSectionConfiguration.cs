namespace CasterSimulator.Models;

public class CoolingSectionConfiguration
{
    public double BaseFlowLps { get; set; }
    public double FlowPerSpeedLps { get; set; }
    public List<CoolingSection> Sections { get; set; }
}