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
