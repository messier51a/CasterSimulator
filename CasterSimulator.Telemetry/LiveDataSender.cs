using System.Text;

namespace CasterSimulator.Telemetry
{
    public class LiveDataSender
    {
        private static readonly HttpClient _client = new();

        public async Task SendAsync(string url, string token, string payload)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/octet-stream");
            
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}