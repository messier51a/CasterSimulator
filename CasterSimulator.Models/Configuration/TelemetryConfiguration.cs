namespace CasterSimulator.Models;

public class TelemetryConfiguration
{
    public string GrafanaLiveUrl { get; set; }
    public string GrafanaLiveToken { get; set; } //TODO: remove, add to env variable or vault.
    public string SplunkHecUrl { get; set; }
    public string SplunkHecToken { get; set; } //TODO: remove, add to env variable or vault.
    public string OtlpEndpoint { get; set; }
    public string OpcUaUrl { get; set; }
    public string OpcUaMappingPath { get; set; }
    public string ServiceName { get; set; }
}