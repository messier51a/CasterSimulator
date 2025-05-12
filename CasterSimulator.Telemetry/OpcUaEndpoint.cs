using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using CasterSimulator.Models;

namespace CasterSimulator.Telemetry;

public class OpcUaEndpoint : IMetricsEndpoint, IDisposable
{
    private Session _session;
    private readonly Dictionary<string, OpcMetric> _metricMap;

    public OpcUaEndpoint(string endpointUrl, string mappingPath)
    {
        _metricMap = LoadMapping(mappingPath);
        Connect(endpointUrl).GetAwaiter().GetResult();
    }

    private Dictionary<string, OpcMetric> LoadMapping(string filePath)
    {
        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        var metrics = doc.RootElement.GetProperty("opcUa").GetProperty("metrics");

        var list = JsonSerializer.Deserialize<List<OpcMetric>>(metrics.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return list?.ToDictionary(m => m.Metric, m => m, StringComparer.OrdinalIgnoreCase)
               ?? new Dictionary<string, OpcMetric>();
    }

    private async Task Connect(string endpointUrl)
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "CasterOpcUaClient",
            ApplicationUri = Utils.Format("urn:{0}:CasterOpcUaClient", System.Net.Dns.GetHostName()),
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier(),
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
        };

        await config.Validate(ApplicationType.Client);

        var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: false);
        var endpointConfig = EndpointConfiguration.Create(config);
        var configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfig);

        _session = await Session.Create(
            config,
            configuredEndpoint,
            false,
            "Caster OPC UA Client",
            60000,
            new UserIdentity(new AnonymousIdentityToken()),
            null);

        Console.WriteLine($"Connected to OPC UA: {endpointUrl}");
    }

    public async Task SendAsync(Dictionary<string, object> metrics, string area)
    {
        if (_session == null || !_session.Connected)
        {
            Console.WriteLine("OPC UA session is not connected.");
            return;
        }

        var nodesToWrite = new WriteValueCollection();

        foreach (var (metricName, value) in metrics)
        {
            if (!_metricMap.TryGetValue(metricName, out var tag))
                continue;

            if (value is not (double or int or float or long or bool or string))
                continue;

            var writeValue = new WriteValue
            {
                NodeId = new NodeId(tag.OpcItemName),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            nodesToWrite.Add(writeValue);
        }

        if (nodesToWrite.Count == 0)
            return;

        var results = await _session.WriteAsync(
            new RequestHeader { Timestamp = DateTime.UtcNow },
            nodesToWrite,
            CancellationToken.None);

        for (int i = 0; i < results.Results.Count; i++)
        {
            if (StatusCode.IsBad(results.Results[i]))
            {
                Console.WriteLine($"[OPC UA] Write failed: {nodesToWrite[i].NodeId} - {results.Results[i]}");
            }
        }
    }

    public void Dispose()
    {
        _session?.Close();
        _session?.Dispose();
    }
}
