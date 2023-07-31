using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>();

            var host = builder.Build();
            await host.RunAsync();
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddHttpClient();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var filePath = Path.Combine(env.WebRootPath, "../client/index.html");
                    await context.Response.SendFileAsync(filePath);
                });

                endpoints.MapPost("/capture/{orderId}", CaptureOrderHandler);
                endpoints.MapPost("/webhook", WebhookHandler);
            });
        }

        private async Task CaptureOrderHandler(HttpContext context)
        {
            var orderId = context.Request.RouteValues["orderId"].ToString();
            var access_token = await GetAccessToken();

            var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var captureUrl = $"{Sample_API_BASE}/v2/checkout/orders/{orderId}/capture";
            var response = await httpClient.PostAsync(captureUrl, new StringContent("", Encoding.UTF8, "application/json"));
            var data = await response.Content.ReadAsStringAsync();

            Console.WriteLine("💰 Payment captured!");
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(data);
        }

        private async Task WebhookHandler(HttpContext context)
        {
            var access_token = await GetAccessToken();

            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            dynamic requestBody = Newtonsoft.Json.JsonConvert.DeserializeObject(body);
            var event_type = requestBody.event_type;
            var resource = requestBody.resource;
            var orderId = resource.id;

            Console.WriteLine("🪝 Received Webhook Event");

            try
            {
                var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

                var verifyWebhookUrl = $"{Sample_API_BASE}/v1/notifications/verify-webhook-signature";
                var verificationData = new
                {
                    transmission_id = context.Request.Headers["Sample-transmission-id"],
                    transmission_time = context.Request.Headers["Sample-transmission-time"],
                    cert_url = context.Request.Headers["Sample-cert-url"],
                    auth_algo = context.Request.Headers["Sample-auth-algo"],
                    transmission_sig = context.Request.Headers["Sample-transmission-sig"],
                    webhook_id = WEBHOOK_ID, // Replace with the actual webhook ID
                    webhook_event = requestBody
                };

                var verificationContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(verificationData), Encoding.UTF8, "application/json");
                var verifyResponse = await httpClient.PostAsync(verifyWebhookUrl, verificationContent);
                dynamic verificationResult = Newtonsoft.Json.JsonConvert.DeserializeObject(await verifyResponse.Content.ReadAsStringAsync());

                var verification_status = verificationResult.verification_status;

                if (verification_status != "SUCCESS")
                {
                    Console.WriteLine("⚠️ Webhook signature verification failed.");
                    context.Response.StatusCode = 400;
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Webhook signature verification failed.");
                context.Response.StatusCode = 400;
                return;
            }

            if (event_type == "CHECKOUT.ORDER.APPROVED")
            {
                try
                {
                    var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

                    var captureUrl = $"{Sample_API_BASE}/v2/checkout/orders/{orderId}/capture";
                    var captureResponse = await httpClient.PostAsync(captureUrl, new StringContent("", Encoding.UTF8, "application/json"));

                    Console.WriteLine("💰 Payment captured!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Payment failed.");
                    context.Response.StatusCode = 400;
                    return;
                }
            }

            context.Response.StatusCode = 200;
        }
    }
}
