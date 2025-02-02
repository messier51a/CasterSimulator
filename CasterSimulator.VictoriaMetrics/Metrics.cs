using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Metrics
{
    public List<MetricEntry> MetricsList { get; set; } = new List<MetricEntry>();

    public string ToPrometheusFormat()
    {
        var sb = new StringBuilder();
        foreach (var entry in MetricsList)
        {
            sb.AppendLine(entry.ToPrometheusString());
        }
        return sb.ToString();
    }
}

public class MetricEntry
{
    public string Metric { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string> Labels { get; set; }
    
    public MetricEntry(string metric, double value, Dictionary<string, string>? labels = null)
    {
        Metric = metric;
        Value = value;
        Labels = labels ?? new Dictionary<string, string>();
    }

    public string ToPrometheusString()
    {
        var labelsString = Labels.Any()
            ? "{" + string.Join(",", Labels.Select(kv => $"{kv.Key}=\"{kv.Value}\"")) + "}"
            : "";
        return $"{Metric}{labelsString} {Value}";
    }
}