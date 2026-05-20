using PropertyManagement.MVC.Models;
using System.Text.Json;

namespace PropertyManagement.MVC.Services
{
    public class MaintenanceApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MaintenanceApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<MaintenanceLookupDto?> LookupMaintenanceRequest(string ticketNumber, string phoneNumber)
        {
            var client = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            // URL-encode parameters so phone numbers containing '+' (international format)
            // are transmitted correctly and not misinterpreted as spaces by the server.
            var encodedTicket = Uri.EscapeDataString(ticketNumber);
            var encodedPhone  = Uri.EscapeDataString(phoneNumber);
            var url = $"{apiBaseUrl}/api/Maintenance/lookup?ticketNumber={encodedTicket}&phoneNumber={encodedPhone}";

            try
            {
                var response = await client.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MaintenanceLookupDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (HttpRequestException ex)
            {
                // Return a sentinel with ApiDown=true so the controller can show a
                // helpful "service unavailable" message instead of a generic 404.
                return new MaintenanceLookupDto { ApiDown = true, ErrorMessage = ex.Message };
            }
        }
    }
}