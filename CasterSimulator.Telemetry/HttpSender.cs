using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CasterSimulator.Telemetry;

public abstract class HttpSender
{
    private static readonly HttpClientHandler _handler = new()
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };

    private static readonly HttpClient _client = new(_handler);

    protected async Task SendAsync(string url, string token, string payload, string contentType, string? authScheme = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(authScheme))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(authScheme, token);
        }

        // Use the simpler approach that was working before
        request.Content = new StringContent(payload, Encoding.UTF8, contentType);

        Console.WriteLine($"POST {url}");
        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(authScheme))
            Console.WriteLine($"Authorization: {authScheme} {token}");

        Console.WriteLine(payload);

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error {response.StatusCode}: {errorBody}");
        }

        response.EnsureSuccessStatusCode();
    }
}