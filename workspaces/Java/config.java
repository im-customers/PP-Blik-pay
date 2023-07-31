public class ConfigProperties {
    private final String NODE_ENV = System.getenv("NODE_ENV");
    private final String CLIENT_ID = System.getenv("CLIENT_ID");
    private final String CLIENT_SECRET = System.getenv("CLIENT_SECRET");
    private final boolean isProd = "production".equals(NODE_ENV);

    public String getNodeId() {
        return NODE_ENV;
    }

    public String getClientId() {
        return CLIENT_ID;
    }

    public String getClientSecret() {
        return CLIENT_SECRET;
    }

    public boolean isProd() {
        return isProd;
    }

    public String getSampleApiBase() {
        return isProd ? "https://api.SAMPLE.com" : "https://api.sandbox.SAMPLE.com";
    }
}

public class Main {
    public static void main(String[] args) {
        ConfigProperties config = new ConfigProperties();

        // Access the properties
        String nodeEnv = config.getNodeId();
        String clientId = config.getClientId();
        String clientSecret = config.getClientSecret();
        boolean isProd = config.isProd();
        String sampleApiBase = config.getSampleApiBase();

        System.out.println("NODE_ENV: " + nodeEnv);
        System.out.println("CLIENT_ID: " + clientId);
        System.out.println("CLIENT_SECRET: " + clientSecret);
        System.out.println("isProd: " + isProd);
        System.out.println("SAMPLE_API_BASE: " + sampleApiBase);
    }
}
