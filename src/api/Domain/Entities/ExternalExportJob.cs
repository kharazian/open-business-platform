using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class ExternalExportJob : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string SourceType { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string IntegrationKey { get; set; } = string.Empty;

    public Guid? FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public Guid? ReportId { get; set; }

    public ReportDefinition? Report { get; set; }

    public string Status { get; set; } = string.Empty;

    public int RowCount { get; set; }

    public string? ArtifactFileName { get; set; }

    public string? ArtifactContentType { get; set; }

    public long ArtifactSizeBytes { get; set; }

    public string? ArtifactContent { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public JsonDocument RequestJson { get; set; } = JsonSerializer.SerializeToDocument(new { });

    public JsonDocument? ArtifactMetadataJson { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}
