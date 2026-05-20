namespace OpenBusinessPlatform.Api.Configuration;

public static class EnvironmentConfiguration
{
    public static void ApplyDerivedValues()
    {
        CopyIfPresent("APP_NAME", "Application__Name");
        CopyIfPresent("APP_ENV", "Application__EnvironmentName");
        CopyIfPresent("BRAND_LOGO_TEXT", "Branding__LogoText");
        CopyIfPresent("DEFAULT_COMPANY_NAME", "Branding__CompanyName");
        CopyIfPresent("DEFAULT_COMPANY_LOGO_URL", "Branding__LogoUrl");
        CopyIfPresent("BOOTSTRAP_ADMIN_EMAIL", "BootstrapAdmin__Email");
        CopyIfPresent("BOOTSTRAP_ADMIN_PASSWORD", "BootstrapAdmin__Password");
        CopyIfPresent("AUTH_COOKIE_NAME", "Authentication__CookieName");

        SetIfMissing("ConnectionStrings__Postgres", BuildPostgresConnectionString());
        SetIfMissing("ConnectionStrings__Redis", BuildRedisConnectionString());
        SetIfMissing("ASPNETCORE_URLS", $"http://localhost:{GetValue("API_PORT", "5080")}");

        var frontendHost = GetValue("VITE_APP_HOST", "127.0.0.1");
        var frontendPort = GetValue("VITE_APP_PORT", "5174");
        SetIfMissing("Cors__AllowedOrigins__0", $"http://{frontendHost}:{frontendPort}");
        SetIfMissing("Cors__AllowedOrigins__1", $"http://{GetFallbackFrontendHost(frontendHost)}:{frontendPort}");
    }

    private static string BuildPostgresConnectionString()
    {
        var host = GetValue("POSTGRES_HOST", "localhost");
        var port = GetValue("POSTGRES_PORT", "5432");
        var database = GetValue("POSTGRES_DB", "open_business_platform");
        var username = GetValue("POSTGRES_USER", "obp");
        var password = GetValue("POSTGRES_PASSWORD", "obp_dev_password");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    private static string BuildRedisConnectionString()
    {
        var host = GetValue("REDIS_HOST", "localhost");
        var port = GetValue("REDIS_PORT", "6379");

        return $"{host}:{port}";
    }

    private static void CopyIfPresent(string sourceKey, string targetKey)
    {
        var value = Environment.GetEnvironmentVariable(sourceKey);

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        SetIfMissing(targetKey, value);
    }

    private static void SetIfMissing(string key, string value)
    {
        if (Environment.GetEnvironmentVariable(key) is null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string GetValue(string key, string fallback)
    {
        return Environment.GetEnvironmentVariable(key) ?? fallback;
    }

    private static string GetFallbackFrontendHost(string frontendHost)
    {
        return string.Equals(frontendHost, "localhost", StringComparison.OrdinalIgnoreCase)
            ? "127.0.0.1"
            : "localhost";
    }
}
