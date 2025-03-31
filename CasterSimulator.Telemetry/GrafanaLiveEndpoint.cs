using System.Text;

namespace CasterSimulator.Telemetry;

public class GrafanaLiveEndpoint : HttpSender, IMetricsEndpoint
{
    private readonly string _baseUrl;
    private readonly string _token;
    private readonly string _measurement;

    public GrafanaLiveEndpoint(string baseUrl, string token, string channelName)
    {
        _baseUrl = $"{baseUrl}/{channelName}";
        _token = token;
    }
    
    public Task SendAsync(Dictionary<string, object> metrics, string area)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;

        var line = string.Join(",", metrics
            .Where(kv => kv.Value != null)
            .Select(kv =>
                $"{kv.Key}={(kv.Value is string s ? $"\"{s}\"" : kv.Value.ToString())}"));

        var payload = $"{area} {line} {timestamp}";

        return SendAsync(_baseUrl, _token, payload, "application/octet-stream", "Bearer");

    }

}