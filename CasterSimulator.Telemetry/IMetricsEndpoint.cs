namespace CasterSimulator.Telemetry;

public interface IMetricsEndpoint
{
    Task SendAsync(Dictionary<string, object> metrics, string area);
}