namespace OpenBusinessPlatform.Api.Modules.Workspaces;

public static class WorkspaceDefaults
{
    public static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static readonly Guid WorkspaceId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public const string TenantName = "Open Business Platform";

    public const string TenantSlug = "default-tenant";

    public const string WorkspaceName = "Default Workspace";

    public const string WorkspaceSlug = "default-workspace";
}
