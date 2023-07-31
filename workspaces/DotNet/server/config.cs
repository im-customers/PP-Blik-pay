using System;

public class Config
{
    public static string NODE_ENV => Environment.GetEnvironmentVariable("NODE_ENV");
    public static string CLIENT_ID => Environment.GetEnvironmentVariable("CLIENT_ID");
    public static string APP_SECRET => Environment.GetEnvironmentVariable("APP_SECRET");

    public static string WEBHOOK_ID => "test";
    public static bool IsProd => NODE_ENV == "production";

    public static string PAYPAL_API_BASE => IsProd
        ? "https://api.paypal.com"
        : "https://api.sandbox.paypal.com";
}
