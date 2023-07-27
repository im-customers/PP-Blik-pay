using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class OAuth
{
    public static async Task<string> GetAccessTokenAsync()
    {
        string payPalApiBase = Config.PAYPAL_API_BASE;
        string clientId = Config.CLIENT_ID;
        string appSecret = Config.APP_SECRET;
        Console.WriteLine("payPalApiBase: " + payPalApiBase);
        Console.WriteLine("clientId: " + clientId);
        Console.WriteLine("appSecret: " + appSecret);

        string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{appSecret}"));

        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await httpClient.PostAsync($"{payPalApiBase}/v1/oauth2/token", content);
        string responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine("responseContent: " + responseContent);
        return responseContent;
    }
}
