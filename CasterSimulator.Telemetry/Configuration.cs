using CasterSimulator.Models;
using Microsoft.Extensions.Configuration;

namespace CasterSimulator.Telemetry;

public static class Configuration
{
    private static bool _loaded = false;
    public static TelemetryConfiguration Telemetry { get; private set; }
    
    public static void Load()
    {
        if (_loaded)
            return;
        
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings-telemetry.json")
            .AddUserSecrets("caster-simulator-telemetry")
            .Build();

        Telemetry = config.GetSection("Telemetry").Get<TelemetryConfiguration>();
        _loaded = true;
    }
}