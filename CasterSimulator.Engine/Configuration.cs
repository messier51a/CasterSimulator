using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CasterSimulator.Models;

namespace CasterSimulator.Engine
{
    public class Configuration
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
        public CoolingSectionConfiguration CoolingSectionConfiguration { get; set; }

        public static Configuration LoadFromJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Configuration>(json) ?? throw new InvalidOperationException("Failed to deserialize configuration.");
        }
    }

    public class CoolingSectionConfiguration
    {
        public double BaseFlowLps { get; set; }
        public double FlowPerSpeedLps { get; set; }
        public List<CoolingSection> Sections { get; set; }
    }
 
}