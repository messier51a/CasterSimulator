using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using System.IO;

namespace VictoriaMetrics;
public class VictoriaMetricsClient
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _endpoint;
    private static readonly ILog _log = LogManager.GetLogger(typeof(VictoriaMetricsClient));

    static VictoriaMetricsClient()
    {
        XmlConfigurator.Configure(new FileInfo("log4net.config"));
    }

    public VictoriaMetricsClient(string endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task PushMetricsAsync(Metrics metrics)
    {
        try
        {
            string prometheusPayload = metrics.ToPrometheusFormat();
            var content = new StringContent(prometheusPayload, Encoding.UTF8, "text/plain");
            HttpResponseMessage response = await _httpClient.PostAsync(_endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Metrics pushed to {_endpoint}");
                _log.Info("Metrics sent successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to push metrics to {_endpoint}");
                _log.Error($"Failed to send metrics: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while pushing metrics to {_endpoint}: {ex.Message}");
            _log.Error($"Error sending metrics: {ex.Message}");
        }
    }
}