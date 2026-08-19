using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class ProcessingJobDefinition : WorkspaceFullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public JsonDocument ConfigJson { get; set; } = JsonSerializer.SerializeToDocument(new { });
    public JsonDocument? ScheduleJson { get; set; }
    public JsonDocument RetryPolicyJson { get; set; } = JsonSerializer.SerializeToDocument(new { });
    public bool IsEnabled { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset? ScheduleLockedAt { get; set; }
    public Guid? ScheduleClaimId { get; set; }
    public Guid OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public Guid FormId { get; set; }
    public FormDefinition? Form { get; set; }
    public Guid? ReportId { get; set; }
    public ReportDefinition? Report { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
    public ICollection<ProcessingJobRun> Runs { get; } = new List<ProcessingJobRun>();
}

public sealed class ProcessingJobRun : WorkspaceAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid DefinitionId { get; set; }
    public ProcessingJobDefinition? Definition { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public int MaxAttempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public Guid? ClaimId { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? InputFileName { get; set; }
    public long InputSizeBytes { get; set; }
    public string? InputChecksum { get; set; }
    public string? InputContent { get; set; }
    public Guid? RecordImportJobId { get; set; }
    public RecordImportJob? RecordImportJob { get; set; }
    public Guid? ExternalExportJobId { get; set; }
    public ExternalExportJob? ExternalExportJob { get; set; }
    public Guid? RetrySourceRunId { get; set; }
    public ProcessingJobRun? RetrySourceRun { get; set; }
    public JsonDocument? ResultJson { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public JsonDocument? ExtraPropertiesJson { get; set; }
}
