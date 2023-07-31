using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var accessToken = await GetAccessToken();
            Console.WriteLine($"Access Token: {accessToken}");
        }

        public static async Task<string> GetAccessToken()
        {
            var PAYPAL_API_BASE = "https://api.paypal.com"; // Replace with the actual base URL
            var CLIENT_ID = "YOUR_CLIENT_ID"; // Replace with your actual client ID
            var CLIENT_SECRET = "YOUR_CLIENT_SECRET"; // Replace with your actual client secret

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(PAYPAL_API_BASE);

            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.PostAsync("/v1/oauth2/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse the JSON response to extract the access token
                var accessToken = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent)?.access_token;
                return accessToken;
            }
            else
            {
                // Handle error scenarios
                // For example, you may throw an exception or return null if the access token retrieval fails
                throw new Exception("Failed to get access token.");
            }
        }
    }

