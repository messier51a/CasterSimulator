using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CasterSimulator.Streaming
{
    public class LiveDataSender
    {
        private static readonly HttpClient _client = new HttpClient();
        private readonly string _streamUrl;

        public LiveDataSender(string streamUrl, string token)
        {
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _streamUrl = streamUrl;
        }

        public async Task Send(string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/octet-stream");
            var response = await _client.PostAsync(_streamUrl, content);
            Console.WriteLine(
                $"Sent: {payload}, Url: {_streamUrl}, Response: {await response.Content.ReadAsStringAsync()}");
        }
    }
}