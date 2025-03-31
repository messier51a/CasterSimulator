using CasterSimulator.Models;
using Microsoft.Extensions.Configuration;

namespace CasterSimulator.Engine;
public static class Configuration
{
    public static CasterConfiguration Caster { get; private set; }
    public static CoolingSectionConfiguration Cooling { get; private set; }
    public static WebApiConfiguration WebApi { get; private set; }

    public static void Load()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings-caster.json")
            .Build();

        Caster = config.GetSection("CasterConfiguration").Get<CasterConfiguration>();
        Cooling = config.GetSection("CoolingSectionConfiguration").Get<CoolingSectionConfiguration>();
        WebApi = config.GetSection("WebAPI").Get<WebApiConfiguration>();
        
    }
}