namespace CasterSimulator.Models
{
    public class CasterConfiguration
    {
        public double TundishWeightFluctuationTolerance { get; set; }
        public double TundishWeightCorrectionFactor { get; set; }
        public double MaxTundishWeight { get; set; }
        public double RampUpThreshold { get; set; }
        public double LowPouringRate { get; set; }
        public double HighPouringRate { get; set; }
        public double SteadyStateRate { get; set; }
        public double TorchLocation { get; set; }
        public double SteelDensity { get; set; }
        public double TargetCastSpeed { get; set; }
        public double SpeedRampDuration { get; set; }
    }
}