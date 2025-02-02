namespace CasterSimulator.Engine;

public class Configuration
{
    public double TundishWeightFluctuationTolerance { get; set; } = 2000.0; // Allowable weight fluctuation
    public double TundishWeightCorrectionFactor { get; set; } = 0.05; // Scaling factor for rate adjustments
    public double MaxTundishWeight { get; set; } = 22000.0; // Maximum tundish weight in kg
    
    public double RampUpThreshold { get; set; } = 20000.0; // Ramp-up threshold in kg
    public double LowPouringRate { get; set; } = 50.0; // Low flow rate in kg/s
    public double HighPouringRate { get; set; } = 200.0; // Ramp-up flow rate in kg/s
    public double SteadyStateRate { get; set; } = 84.0; // Steady-state flow rate in kg/s
    public double TorchLocation { get; set; } = 10; //Distance from mold to torch machine in meters.
    public double SteelDensity { get; set; } = 7850;
    public double TargetCastSpeed { get; set; } = 5.0;
    public double SpeedRampDuration { get; set; } = 15;
}