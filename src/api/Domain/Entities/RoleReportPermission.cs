using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class RoleReportPermission : Entity<Guid>
{
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public Guid ReportId { get; set; }

    public ReportDefinition? Report { get; set; }

    public string Action { get; set; } = string.Empty;
}
