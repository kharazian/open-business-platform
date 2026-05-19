using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class RoleFormPermission : Entity<Guid>
{
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public string Action { get; set; } = string.Empty;
}
