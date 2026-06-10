using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class RecordImportJob : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public string IntegrationKey { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public string Status { get; set; } = string.Empty;

    public int TotalRows { get; set; }

    public int SucceededRows { get; set; }

    public int FailedRows { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public JsonDocument MappingJson { get; set; } = JsonSerializer.SerializeToDocument(new { fieldMappings = Array.Empty<object>() });

    public ICollection<RecordImportJobRow> Rows { get; } = new List<RecordImportJobRow>();

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public sealed class RecordImportJobRow : Entity<Guid>
{
    public Guid ImportJobId { get; set; }

    public RecordImportJob? ImportJob { get; set; }

    public int RowNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid? RecordId { get; set; }

    public FormRecord? Record { get; set; }

    public JsonDocument? ErrorsJson { get; set; }
}
