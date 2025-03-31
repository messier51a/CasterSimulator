using System.Text.Json;

namespace CasterSimulator.Telemetry;

public class SplunkHecEndpoint : HttpSender, IMetricsEndpoint
{
    private readonly string _url;
    private readonly string _token;

    public SplunkHecEndpoint(string endpoint, string token)
    {
        _url = endpoint;
        _token = token;
    }

    public Task SendAsync(Dictionary<string, object> metrics, string area)
    {
        var fields = new Dictionary<string, object>();

        foreach (var (key, value) in metrics)
        {
            if (value is double or float or int or long or decimal)
            {
                var metricKey = $"metric_name:{key}";
                fields[metricKey] = value;
            }
            else if (value is string s &&
                     !string.IsNullOrWhiteSpace(s) &&
                     s.All(char.IsLetterOrDigit))
            {
                fields[key] = s;
            }
        }

        var payload = new Dictionary<string, object>
        {
            ["time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["event"] = "metric",
            ["source"] = "caster-metrics",
            ["sourcetype"] = "caster-sim",
            ["host"] = "caster-simulator",
            ["fields"] = fields
        };

        var json = JsonSerializer.Serialize(payload);
        return SendAsync(_url, _token, json, "application/json", "Splunk");
    }
}