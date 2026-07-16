using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class WorkspaceMembership : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string Role { get; set; } = WorkspaceMembershipRoles.Member;

    public string Status { get; set; } = WorkspaceMembershipStatuses.Invited;

    public bool IsDefault { get; set; }

    public Guid? InvitedById { get; set; }

    public User? InvitedBy { get; set; }

    public DateTimeOffset InvitedAt { get; set; }

    public DateTimeOffset? ActivatedAt { get; set; }

    public DateTimeOffset? SuspendedAt { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public static class WorkspaceMembershipRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Member = "member";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Owner,
        Admin,
        Member
    };
}

public static class WorkspaceMembershipStatuses
{
    public const string Invited = "invited";
    public const string Active = "active";
    public const string Suspended = "suspended";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Invited,
        Active,
        Suspended
    };
}
