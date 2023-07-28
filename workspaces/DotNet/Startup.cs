using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StaticFiles
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    string indexHtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", "index.html");
                    await context.Response.SendFileAsync(indexHtmlPath);
                });

                endpoints.MapPost("/capture/{orderId}", CaptureOrderHandler);
                endpoints.MapPost("/webhook", WebhookHandler);
            });
            #region Static File Setup
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles")),
                RequestPath = "/StaticFiles",
                EnableDefaultFiles = true
            });
            #endregion
        }
        private async Task CaptureOrderHandler(HttpContext context)
        {
            string orderId = context.Request.RouteValues["orderId"].ToString();
            string access_token = await OAuth.GetAccessTokenAsync();

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(Config.PAYPAL_API_BASE); // Replace with your PayPal API base URL

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var captureUrl = $"/v2/checkout/orders/{orderId}/capture";

            try
            {
                var response = await httpClient.PostAsync(captureUrl, null);
                Console.WriteLine("response: " + response);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("💰 Payment captured!");
                    await context.Response.WriteAsync(responseContent);
                }
                else
                {
                    Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                    await context.Response.WriteAsync("Failed to capture payment.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request exception: {ex.Message}");
                await context.Response.WriteAsync("An error occurred while capturing payment.");
            }

            // using var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();

            // var captureUrl = $"{Config.PAYPAL_API_BASE}/v2/checkout/orders/{orderId}/capture";
            // var request = new HttpRequestMessage(HttpMethod.Post, captureUrl);
            // request.Headers.Add("Content-Type", "application/json");
            // request.Headers.Add("Accept", "application/json");
            // request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

            // var response = await httpClient.SendAsync(request);
            // Console.WriteLine("response: " + response);
            // if (!response.IsSuccessStatusCode)
            // {
            //     Console.WriteLine("❌ Payment failed.");
            //     context.Response.StatusCode = 400;
            //     return;
            // }

            // Console.WriteLine("💰 Payment captured!");
            // context.Response.StatusCode = 200;
        }

        private async Task WebhookHandler(HttpContext context)
        {
            string access_token = await OAuth.GetAccessTokenAsync();

            string body;
            using (var reader = new System.IO.StreamReader(context.Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            dynamic requestBody = JsonConvert.DeserializeObject<dynamic>(body);
            string event_type = requestBody?.event_type;
            dynamic resource = requestBody?.resource;
            string orderId = resource?.id;

            if (string.IsNullOrEmpty(event_type) || string.IsNullOrEmpty(orderId))
            {
                Console.WriteLine("⚠️  Invalid Webhook data.");
                context.Response.StatusCode = 400;
                return;
            }

            /* Verify the webhook signature */
            try
            {
                using var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();

                var verifySignatureUrl = $"{Config.PAYPAL_API_BASE}/v1/notifications/verify-webhook-signature";
                var request = new HttpRequestMessage(HttpMethod.Post, verifySignatureUrl);
                request.Headers.Add("Content-Type", "application/json");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

                var verificationData = new
                {
                    transmission_id = context.Request.Headers["paypal-transmission-id"],
                    transmission_time = context.Request.Headers["paypal-transmission-time"],
                    cert_url = context.Request.Headers["paypal-cert-url"],
                    auth_algo = context.Request.Headers["paypal-auth-algo"],
                    transmission_sig = context.Request.Headers["paypal-transmission-sig"],
                    webhook_id = Config.WEBHOOK_ID,
                    webhook_event = requestBody,
                };

                string verificationJson = JsonConvert.SerializeObject(verificationData);
                request.Content = new StringContent(verificationJson, Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("⚠️  Webhook signature verification failed.");
                    context.Response.StatusCode = 400;
                    return;
                }

                dynamic verificationResult = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                string verification_status = verificationResult?.verification_status;

                if (verification_status != "SUCCESS")
                {
                    Console.WriteLine("⚠️  Webhook signature verification failed.");
                    context.Response.StatusCode = 400;
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️  Webhook signature verification failed.");
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = 400;
                return;
            }

            /* Capture the order */
            if (event_type == "CHECKOUT.ORDER.APPROVED")
            {
                await CaptureOrderHandler(context);
            }

            context.Response.StatusCode = 200;
        }
    }
}