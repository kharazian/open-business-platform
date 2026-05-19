using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class RolePermission : Entity<Guid>
{
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public string Permission { get; set; } = string.Empty;
}
