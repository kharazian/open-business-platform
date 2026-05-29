namespace OpenBusinessPlatform.Api.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string Name { get; init; } = "Open Business Platform";

    public string EnvironmentName { get; init; } = "Development";
}

public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    public string LogoText { get; init; } = "OBP";

    public string CompanyName { get; init; } = "Your Company";

    public string LogoUrl { get; init; } = "/logo.svg";
}

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Email { get; init; } = "";

    public string Password { get; init; } = "";
}

public sealed class LocalAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string CookieName { get; init; } = "obp.auth";

    public bool RequireSecureCookies { get; init; } = true;
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = "no-reply@company.test";

    public string FromName { get; init; } = "Open Business Platform";

    public string SmtpHost { get; init; } = "";

    public int SmtpPort { get; init; } = 587;

    public string SmtpUsername { get; init; } = "";

    public string SmtpPassword { get; init; } = "";

    public bool UseStartTls { get; init; } = true;
}

public sealed class PasswordRecoveryOptions
{
    public const string SectionName = "PasswordRecovery";

    public string ResetPasswordUrl { get; init; } = "http://localhost:5174/reset-password";

    public int TokenLifetimeMinutes { get; init; } = 60;
}
