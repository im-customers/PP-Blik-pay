using System;

public class AppConfig
{
    public string NODE_ENV { get; private set; }
    public string CLIENT_ID { get; private set; }
    public string CLIENT_SECRET { get; private set; }
    public bool IsProd { get; private set; }
    public string PAYPAL_API_BASE { get; private set; }

    public AppConfig()
    {
        NODE_ENV = Environment.GetEnvironmentVariable("NODE_ENV");
        CLIENT_ID = Environment.GetEnvironmentVariable("CLIENT_ID");
        CLIENT_SECRET = Environment.GetEnvironmentVariable("CLIENT_SECRET");

        IsProd = NODE_ENV == "production";

        PAYPAL_API_BASE = IsProd
            ? "https://api.paypal.com"
            : "https://api.sandbox.paypal.com";
    }

    public static void Main(string[] args)
    {
        // Create an instance of AppConfig to access the configuration
        AppConfig config = new AppConfig();

        // You can use the values as needed
        Console.WriteLine("isProd: " + config.IsProd);
        Console.WriteLine("PAYPAL_API_BASE: " + config.PAYPAL_API_BASE);
        Console.WriteLine("CLIENT_ID: " + config.CLIENT_ID);
        Console.WriteLine("CLIENT_SECRET: " + config.CLIENT_SECRET);
    }
}
