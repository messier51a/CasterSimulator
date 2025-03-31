using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasterSimulator.Telemetry
{
    public class MetricsPublisher : IDisposable
    {
        private readonly List<IMetricsEndpoint> _endpoints = new();
        private readonly Dictionary<string, (Func<object> Provider, string Area)> _valueSources = new();
        private string _channelName;
        public MetricsPublisher(string channelName)
        {
            Configuration.Load();
            
            Console.WriteLine($"Grafana Token: {Configuration.Telemetry.GrafanaLiveToken}");
            Console.WriteLine($"Splunk Token: {Configuration.Telemetry.SplunkHecToken}");
            
            _channelName = channelName;

            _endpoints.Add(new GrafanaLiveEndpoint(
                Configuration.Telemetry.GrafanaLiveUrl,
                Configuration.Telemetry.GrafanaLiveToken, 
                channelName));

            _endpoints.Add(new SplunkHecEndpoint(
                Configuration.Telemetry.SplunkHecUrl,
                Configuration.Telemetry.SplunkHecToken));
        }

        public void RegisterMetric(string name, Func<object> valueProvider, string area)
        {
            _valueSources[name] = (valueProvider, area);
        }

        public async Task Push()
        {
            var groupedByArea = _valueSources
                .GroupBy(kv => kv.Value.Area)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(
                            x => x.Key,
                            x =>
                            {
                                try { return x.Value.Provider(); }
                                catch { return null; }
                            })
                        .Where(kv => kv.Value is double or string)
                        .ToDictionary(kv => kv.Key, kv => kv.Value)
                );

            foreach (var (area, metrics) in groupedByArea)
            {
                foreach (var endpoint in _endpoints)
                {
                    await endpoint.SendAsync(metrics, area);
                }
            }
        }
        
        public void Dispose()
        {
            foreach (var endpoint in _endpoints.OfType<IDisposable>())
            {
                endpoint.Dispose();
            }
        }
    }
}