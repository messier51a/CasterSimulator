using System;
using System.Collections.Generic;
using System.IO;
using CasterSimulator.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CasterSimulator.Engine
{
    public class Configuration
    {
        // Singleton instance
        [JsonIgnore] private static Configuration? _instance;
        [JsonIgnore] private static readonly object _lock = new object();

        [JsonIgnore]
        private static string _configFilePath = Path.Combine(Environment.CurrentDirectory, "configuration.json");

        public string GrafanaLiveUrl { get; set; }
        public string GrafanaLiveToken { get; set; }
        public string WebApiUrl { get; set; }
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

        // This constructor is used only by JSON deserializer
        // It's protected by a convention that it should not be called directly by user code
        [JsonConstructor]
        public Configuration()
        {
        }

        // Public method to get the singleton instance
        public static Configuration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadFromJson(_configFilePath);
                        }
                    }
                }

                return _instance;
            }
        }

        // Optional method to explicitly initialize with a specific file path
        public static void Initialize(string filePath)
        {
            lock (_lock)
            {
                _configFilePath = filePath;
                _instance = LoadFromJson(filePath);
            }
        }

        // Load configuration from JSON file
        private static Configuration LoadFromJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Configuration>(json) ??
                   throw new InvalidOperationException("Failed to deserialize configuration.");
        }

        // Method to reset the singleton (useful for testing)
        public static void Reset()
        {
            lock (_lock)
            {
                _instance = null;
                // Reset to default path
                _configFilePath = Path.Combine(Environment.CurrentDirectory, "configuration.json");
            }
        }

        // Get the current configuration file path
        public static string ConfigFilePath => _configFilePath;
    }

    public class CoolingSectionConfiguration
    {
        public double BaseFlowLps { get; set; }
        public double FlowPerSpeedLps { get; set; }
        public List<CoolingSection> Sections { get; set; }
    }
}