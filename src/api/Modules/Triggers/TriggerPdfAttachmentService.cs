using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Printing;
using OpenBusinessPlatform.Api.Modules.Records;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerPdfAttachmentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly PrintPdfService pdfService;

    public TriggerPdfAttachmentService(OpenBusinessPlatformDbContext dbContext, PrintPdfService pdfService)
    {
        this.dbContext = dbContext;
        this.pdfService = pdfService;
    }

    public async Task<IReadOnlyList<EmailAttachment>> BuildEmailAttachmentsAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        if (action.PrintTemplateId is null)
        {
            return Array.Empty<EmailAttachment>();
        }

        if (context.RecordId == Guid.Empty || TriggerEvents.IsScheduled(context.EventName))
        {
            throw new InvalidOperationException("Email PDF attachments require a current record context.");
        }

        var template = await dbContext.PrintTemplates
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == action.PrintTemplateId.Value && !candidate.IsDeleted,
                cancellationToken);

        if (template is null)
        {
            throw new InvalidOperationException("Email PDF attachment template was not found.");
        }

        if (!string.Equals(template.Type, PrintTemplateTypes.Record, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Email PDF attachments require a record print template.");
        }

        if (template.FormId != context.FormId)
        {
            throw new InvalidOperationException("Email PDF attachment template must belong to the trigger form.");
        }

        if (template.CurrentVersion is null)
        {
            throw new InvalidOperationException("Email PDF attachment template must have a published version.");
        }

        var record = await dbContext.Records
            .AsNoTracking()
            .Include(candidate => candidate.FormVersion)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == context.RecordId && !candidate.IsDeleted,
                cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("Email PDF attachment record was not found.");
        }

        if (record.FormId != context.FormId || record.FormId != template.FormId)
        {
            throw new InvalidOperationException("Email PDF attachment record form does not match the trigger form.");
        }

        if (record.FormVersion is null)
        {
            throw new InvalidOperationException("Email PDF attachment record form version was not found.");
        }

        var version = template.CurrentVersion;
        var recordDto = ToRecordDetailDto(record);
        var versionDto = ToVersionDetailDto(version);
        var pdfBytes = pdfService.BuildRecordPdf(versionDto, recordDto);

        AddAudit(template.Id, version.Id, context.ActorUserId, triggerId, triggerLogId, action.Id, record.Id);

        return new[]
        {
            new EmailAttachment(
                CreatePdfFileName(version.Name, version.VersionNumber),
                PrintPdfDocumentBuilder.ContentType,
                pdfBytes)
        };
    }

    private static FormRecordDetailDto ToRecordDetailDto(FormRecord record)
    {
        var schema = record.FormVersion?.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions)
            ?? throw new InvalidOperationException("Email PDF attachment record form schema was not found.");

        return new FormRecordDetailDto(
            record.Id,
            record.FormId,
            record.FormVersionId,
            record.Status,
            record.OwnerId,
            record.DepartmentId,
            record.AssignedToUserId,
            record.AssignedGroupId,
            DeserializeValues(record.ValuesJson),
            schema,
            Array.Empty<string>(),
            record.ConcurrencyStamp,
            record.CreatedAt,
            record.CreatedById,
            record.UpdatedAt,
            record.UpdatedById);
    }

    private static PrintTemplateVersionDetailDto ToVersionDetailDto(PrintTemplateVersion version)
    {
        var config = version.ConfigJson.RootElement.Deserialize<PrintTemplateConfig>(JsonOptions)
            ?? throw new InvalidOperationException("Email PDF attachment print template config was invalid.");

        return new PrintTemplateVersionDetailDto(
            version.Id,
            version.PrintTemplateId,
            version.FormId,
            version.ReportId,
            version.Name,
            version.Description,
            version.Type,
            version.VersionNumber,
            config,
            version.PublishedAt,
            version.PublishedById,
            version.CreatedAt,
            version.CreatedById);
    }

    private void AddAudit(
        Guid templateId,
        Guid versionId,
        Guid? actorUserId,
        Guid triggerId,
        Guid? triggerLogId,
        string actionId,
        Guid recordId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "PrintTemplate",
            EntityId = templateId,
            Action = "print_template_pdf_email_attached",
            UserId = actorUserId,
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                versionId,
                recordId,
                triggerId,
                triggerLogId,
                actionId
            }, JsonOptions)
        });
    }

    private static IReadOnlyDictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static string CreatePdfFileName(string templateName, int versionNumber)
    {
        var safeName = new string(templateName
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
                .ToArray())
            .Trim('-');

        return $"{(string.IsNullOrWhiteSpace(safeName) ? "print-template" : safeName)}-v{versionNumber}.pdf";
    }
}
