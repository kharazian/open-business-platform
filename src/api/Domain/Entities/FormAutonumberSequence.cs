using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class FormAutonumberSequence : WorkspaceEntity<Guid>
{
    public Guid FormId { get; set; }
    public FormDefinition? Form { get; set; }
    public string FieldId { get; set; } = string.Empty;
    public long NextValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
