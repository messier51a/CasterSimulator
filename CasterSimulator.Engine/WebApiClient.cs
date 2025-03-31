using System.Net.Http.Json;
using CasterSimulator.Models;

namespace CasterSimulator.Engine
{
    public class WebApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public WebApiClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<bool> UpdateHeatScheduleAsync(List<Heat> heatSchedule)
        {
            try
            {
                string url = $"{_baseUrl}/api/heatschedule";
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, heatSchedule);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> UpdateCutScheduleAsync(List<Product?> cutSchedule)
        {
            try
            {
                string url = $"{_baseUrl}/api/cutschedule";
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, cutSchedule);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<List<Product>?> GetCutScheduleAsync()
        {
            try
            {
                string url = $"{_baseUrl}/api/cutschedule";
                return await _httpClient.GetFromJsonAsync<List<Product>>(url);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        public async Task<bool> UpdateProductsAsync(List<Product> products)
        {
            try
            {
                string url = $"{_baseUrl}/api/products";
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, products);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}