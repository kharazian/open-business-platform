using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public sealed class PrintTemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public PrintTemplateService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PrintTemplateSummaryDto>> ListAsync(
        Guid formId,
        string? type,
        Guid? reportId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PrintTemplates
            .AsNoTracking()
            .Include(template => template.CurrentVersion)
            .Where(template => template.FormId == formId && !template.IsDeleted);

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(template => template.Type == type.Trim());
        }

        if (reportId is not null)
        {
            query = query.Where(template => template.ReportId == reportId);
        }

        var templates = await query
            .OrderBy(template => template.Type)
            .ThenBy(template => template.Name)
            .ToArrayAsync(cancellationToken);

        return templates.Select(ToSummaryDto).ToArray();
    }

    public async Task<PrintTemplateDetailDto> GetAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var template = await dbContext.PrintTemplates
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == templateId && !candidate.IsDeleted, cancellationToken);

        if (template is null)
        {
            throw new PrintTemplateException(StatusCodes.Status404NotFound, "Print template was not found.");
        }

        return ToDetailDto(template);
    }

    public async Task<PrintTemplateDetailDto> CreateAsync(
        Guid formId,
        CreatePrintTemplateRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var normalized = await ValidateRequestAsync(formId, request.Name, request.Type, request.ReportId, request.Config, cancellationToken);
        var template = new PrintTemplate
        {
            Id = Guid.NewGuid(),
            FormId = formId,
            ReportId = normalized.ReportId,
            Name = normalized.Name,
            Description = NormalizeOptionalText(request.Description),
            Type = normalized.Type,
            ConfigJson = Serialize(normalized.Config),
            CreatedById = createdById
        };

        dbContext.PrintTemplates.Add(template);
        AddAudit(template.Id, "print_template_created", createdById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(template);
    }

    public async Task<PrintTemplateDetailDto> UpdateAsync(
        Guid templateId,
        UpdatePrintTemplateRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.PrintTemplates
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == templateId && !candidate.IsDeleted, cancellationToken);

        if (template is null)
        {
            throw new PrintTemplateException(StatusCodes.Status404NotFound, "Print template was not found.");
        }

        if (!string.Equals(template.ConcurrencyStamp, request.ConcurrencyStamp, StringComparison.Ordinal))
        {
            throw new PrintTemplateException(StatusCodes.Status409Conflict, "Print template was updated by someone else. Refresh and try again.");
        }

        var normalized = await ValidateRequestAsync(template.FormId, request.Name, request.Type, request.ReportId, request.Config, cancellationToken);
        template.ReportId = normalized.ReportId;
        template.Name = normalized.Name;
        template.Description = NormalizeOptionalText(request.Description);
        template.Type = normalized.Type;
        template.ConfigJson = Serialize(normalized.Config);
        template.UpdatedById = updatedById;
        AddAudit(template.Id, "print_template_updated", updatedById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(template);
    }

    public async Task<IReadOnlyCollection<PrintTemplateVersionSummaryDto>> ListVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var templateExists = await dbContext.PrintTemplates
            .AsNoTracking()
            .AnyAsync(candidate => candidate.Id == templateId && !candidate.IsDeleted, cancellationToken);

        if (!templateExists)
        {
            throw new PrintTemplateException(StatusCodes.Status404NotFound, "Print template was not found.");
        }

        var versions = await dbContext.PrintTemplateVersions
            .AsNoTracking()
            .Where(version => version.PrintTemplateId == templateId)
            .OrderByDescending(version => version.VersionNumber)
            .ToArrayAsync(cancellationToken);

        return versions.Select(ToVersionSummaryDto).ToArray();
    }

    public async Task<PrintTemplateVersionDetailDto> GetVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.PrintTemplateVersions
            .AsNoTracking()
            .Include(candidate => candidate.PrintTemplate)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == versionId
                    && candidate.PrintTemplate != null
                    && !candidate.PrintTemplate.IsDeleted,
                cancellationToken);

        if (version is null)
        {
            throw new PrintTemplateException(StatusCodes.Status404NotFound, "Print template version was not found.");
        }

        return ToVersionDetailDto(version);
    }

    public async Task<PrintTemplateDetailDto> PublishAsync(
        Guid templateId,
        PublishPrintTemplateRequest request,
        Guid? publishedById,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.PrintTemplates
            .Include(candidate => candidate.CurrentVersion)
            .Include(candidate => candidate.Versions)
            .FirstOrDefaultAsync(candidate => candidate.Id == templateId && !candidate.IsDeleted, cancellationToken);

        if (template is null)
        {
            throw new PrintTemplateException(StatusCodes.Status404NotFound, "Print template was not found.");
        }

        EnsureConcurrencyStamp(template.ConcurrencyStamp, request.ConcurrencyStamp);

        var config = Deserialize<PrintTemplateConfig>(template.ConfigJson) ?? EmptyConfig(template.Type);
        var normalized = await ValidateRequestAsync(template.FormId, template.Name, template.Type, template.ReportId, config, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var versionNumber = template.Versions.Count == 0 ? 1 : template.Versions.Max(version => version.VersionNumber) + 1;
        var version = new PrintTemplateVersion
        {
            Id = Guid.NewGuid(),
            PrintTemplateId = template.Id,
            PrintTemplate = template,
            FormId = template.FormId,
            ReportId = normalized.ReportId,
            Name = normalized.Name,
            Description = template.Description,
            Type = normalized.Type,
            VersionNumber = versionNumber,
            ConfigJson = Serialize(normalized.Config),
            CreatedById = publishedById,
            PublishedById = publishedById,
            PublishedAt = now
        };

        dbContext.PrintTemplateVersions.Add(version);
        template.CurrentVersionId = version.Id;
        template.CurrentVersion = version;
        template.UpdatedById = publishedById;

        AddAudit(template.Id, "print_template_published", publishedById, new { versionId = version.Id, versionNumber });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(template);
    }

    public async Task LogPdfGeneratedAsync(
        Guid templateId,
        Guid versionId,
        Guid? generatedById,
        object metadata,
        CancellationToken cancellationToken)
    {
        AddAudit(templateId, "print_template_pdf_generated", generatedById, new { versionId, metadata });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid templateId, Guid? deletedById, CancellationToken cancellationToken)
    {
        var template = await dbContext.PrintTemplates
            .FirstOrDefaultAsync(candidate => candidate.Id == templateId && !candidate.IsDeleted, cancellationToken);

        if (template is null)
        {
            throw new PrintTemplateException(StatusCodes.Status404NotFound, "Print template was not found.");
        }

        template.DeletedById = deletedById;
        dbContext.PrintTemplates.Remove(template);
        AddAudit(template.Id, "print_template_deleted", deletedById);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<NormalizedPrintTemplateRequest> ValidateRequestAsync(
        Guid formId,
        string name,
        string type,
        Guid? reportId,
        PrintTemplateConfig config,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var normalizedType = type.Trim();
        var normalizedConfig = NormalizeConfig(config);
        FormSchemaDefinition? schema = null;
        var errors = new List<PrintTemplateValidationError>();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add(new PrintTemplateValidationError("name", "print_template.name.required", "Template name is required."));
        }

        if (!PrintTemplateTypes.Supported.Contains(normalizedType))
        {
            errors.Add(new PrintTemplateValidationError("type", "print_template.type.invalid", "Template type must be record or report."));
        }

        if (normalizedType == PrintTemplateTypes.Report && reportId is null)
        {
            errors.Add(new PrintTemplateValidationError("reportId", "print_template.report.required", "Report templates must target a saved report."));
        }

        if (normalizedType == PrintTemplateTypes.Record && reportId is not null)
        {
            errors.Add(new PrintTemplateValidationError("reportId", "print_template.report.record_scope", "Record templates cannot target a report."));
        }

        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            errors.Add(new PrintTemplateValidationError("formId", "print_template.form.not_found", "Form was not found."));
        }
        else if ((schema = ResolveAuthoringSchema(form)) is null)
        {
            errors.Add(new PrintTemplateValidationError("formId", "print_template.form.schema_unavailable", "Form schema is not available for print template validation."));
        }

        if (reportId is not null)
        {
            var reportMatchesForm = await dbContext.Reports
                .AsNoTracking()
                .AnyAsync(report => report.Id == reportId && report.FormId == formId && !report.IsDeleted, cancellationToken);

            if (!reportMatchesForm)
            {
                errors.Add(new PrintTemplateValidationError("reportId", "print_template.report.not_found", "Report was not found for this form."));
            }
        }

        errors.AddRange(PrintTemplateValidator.Validate(normalizedConfig, normalizedType, schema).Errors);

        if (errors.Count > 0)
        {
            throw new PrintTemplateException(StatusCodes.Status400BadRequest, "Print template is invalid.", errors);
        }

        return new NormalizedPrintTemplateRequest(normalizedName, normalizedType, normalizedType == PrintTemplateTypes.Report ? reportId : null, normalizedConfig);
    }

    private static PrintTemplateSummaryDto ToSummaryDto(PrintTemplate template)
    {
        var config = Deserialize<PrintTemplateConfig>(template.ConfigJson);

        return new PrintTemplateSummaryDto(
            template.Id,
            template.FormId,
            template.ReportId,
            template.Name,
            template.Description,
            template.Type,
            config?.Sections.Count ?? 0,
            template.ConcurrencyStamp,
            template.CreatedAt,
            template.CreatedById,
            template.UpdatedAt,
            template.UpdatedById,
            template.CurrentVersionId,
            template.CurrentVersion?.VersionNumber,
            template.CurrentVersion?.PublishedAt);
    }

    private static PrintTemplateDetailDto ToDetailDto(PrintTemplate template)
    {
        return new PrintTemplateDetailDto(
            template.Id,
            template.FormId,
            template.ReportId,
            template.Name,
            template.Description,
            template.Type,
            Deserialize<PrintTemplateConfig>(template.ConfigJson) ?? EmptyConfig(template.Type),
            template.ConcurrencyStamp,
            template.CreatedAt,
            template.CreatedById,
            template.UpdatedAt,
            template.UpdatedById,
            template.CurrentVersionId,
            template.CurrentVersion?.VersionNumber,
            template.CurrentVersion?.PublishedAt);
    }

    private static PrintTemplateVersionSummaryDto ToVersionSummaryDto(PrintTemplateVersion version)
    {
        var config = Deserialize<PrintTemplateConfig>(version.ConfigJson);

        return new PrintTemplateVersionSummaryDto(
            version.Id,
            version.PrintTemplateId,
            version.FormId,
            version.ReportId,
            version.Name,
            version.Description,
            version.Type,
            version.VersionNumber,
            config?.Sections.Count ?? 0,
            version.PublishedAt,
            version.PublishedById,
            version.CreatedAt,
            version.CreatedById);
    }

    private static PrintTemplateVersionDetailDto ToVersionDetailDto(PrintTemplateVersion version)
    {
        return new PrintTemplateVersionDetailDto(
            version.Id,
            version.PrintTemplateId,
            version.FormId,
            version.ReportId,
            version.Name,
            version.Description,
            version.Type,
            version.VersionNumber,
            Deserialize<PrintTemplateConfig>(version.ConfigJson) ?? EmptyConfig(version.Type),
            version.PublishedAt,
            version.PublishedById,
            version.CreatedAt,
            version.CreatedById);
    }

    private void AddAudit(Guid entityId, string action, Guid? userId = null, object? metadata = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "PrintTemplate",
            EntityId = entityId,
            Action = action,
            UserId = userId,
            MetadataJson = metadata is null ? null : Serialize(metadata)
        });
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string requestedStamp)
    {
        if (string.IsNullOrWhiteSpace(requestedStamp)
            || !string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new PrintTemplateException(StatusCodes.Status409Conflict, "Print template was updated by someone else. Refresh and try again.");
        }
    }

    private static JsonDocument Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static T? Deserialize<T>(JsonDocument jsonDocument)
    {
        return jsonDocument.RootElement.Deserialize<T>(JsonOptions);
    }

    private static T? DeserializeOptional<T>(JsonDocument? jsonDocument)
    {
        if (jsonDocument is null)
        {
            return default;
        }

        return jsonDocument.RootElement.Deserialize<T>(JsonOptions);
    }

    private static PrintTemplateConfig EmptyConfig(string type)
    {
        return new PrintTemplateConfig(
            1,
            type,
            new PrintTemplateHeaderConfig("Print template", null, null, true),
            Array.Empty<PrintTemplateSectionConfig>(),
            new PrintTemplateFooterConfig("Open Business Platform"),
            DefaultLayout());
    }

    private static PrintTemplateConfig NormalizeConfig(PrintTemplateConfig config)
    {
        var header = config.Header ?? new PrintTemplateHeaderConfig(string.Empty, null, null, true);
        var footer = config.Footer ?? new PrintTemplateFooterConfig(null);
        var layout = config.Layout ?? DefaultLayout();

        return new PrintTemplateConfig(
            config.SchemaVersion,
            config.Type?.Trim() ?? string.Empty,
            new PrintTemplateHeaderConfig(
                header.Title?.Trim() ?? string.Empty,
                NormalizeOptionalText(header.Subtitle),
                NormalizeOptionalText(header.LogoUrl),
                header.ShowGeneratedAt),
            (config.Sections ?? Array.Empty<PrintTemplateSectionConfig>())
                .Select(section => new PrintTemplateSectionConfig(
                    section.Id?.Trim() ?? string.Empty,
                    section.Kind?.Trim() ?? string.Empty,
                    section.Title?.Trim() ?? string.Empty,
                    (section.FieldIds ?? Array.Empty<string>())
                        .Select(fieldId => fieldId.Trim())
                        .ToArray(),
                    (section.SignatureLabels ?? Array.Empty<string>())
                        .Select(label => label.Trim())
                        .Where(label => !string.IsNullOrWhiteSpace(label))
                        .ToArray(),
                    section.Pagination ?? DefaultSectionPagination(),
                    (section.Conditions ?? Array.Empty<PrintTemplateSectionConditionConfig>())
                        .Select(condition => new PrintTemplateSectionConditionConfig(
                            condition.FieldId?.Trim() ?? string.Empty,
                            condition.Operator?.Trim() ?? string.Empty,
                            NormalizeOptionalText(condition.Value)))
                        .ToArray()))
                .ToArray(),
            new PrintTemplateFooterConfig(NormalizeOptionalText(footer.Text)),
            new PrintTemplateLayoutConfig(
                layout.PageSize?.Trim() ?? string.Empty,
                layout.Orientation?.Trim() ?? string.Empty,
                layout.Margin?.Trim() ?? string.Empty,
                layout.RepeatTableHeaders));
    }

    private static PrintTemplateLayoutConfig DefaultLayout()
    {
        return new PrintTemplateLayoutConfig(
            PrintTemplatePageSizes.Letter,
            PrintTemplateOrientations.Portrait,
            PrintTemplateMargins.Normal,
            RepeatTableHeaders: true);
    }

    private static PrintTemplateSectionPaginationConfig DefaultSectionPagination()
    {
        return new PrintTemplateSectionPaginationConfig(PageBreakBefore: false, AvoidBreakInside: true);
    }

    private static FormSchemaDefinition? ResolveAuthoringSchema(FormDefinition form)
    {
        return DeserializeOptional<FormSchemaDefinition>(form.DraftSchemaJson)
            ?? DeserializeOptional<FormSchemaDefinition>(form.CurrentVersion?.SchemaJson);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record NormalizedPrintTemplateRequest(
        string Name,
        string Type,
        Guid? ReportId,
        PrintTemplateConfig Config);
}
