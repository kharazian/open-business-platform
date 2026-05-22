using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public sealed class FormManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public FormManagementService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<FormSummaryDto>> ListFormsAsync(CancellationToken cancellationToken)
    {
        var forms = await dbContext.Forms
            .AsNoTracking()
            .Include(form => form.CurrentVersion)
            .Where(form => !form.IsDeleted)
            .OrderByDescending(form => form.UpdatedAt ?? form.CreatedAt)
            .ThenBy(form => form.Name)
            .ToArrayAsync(cancellationToken);

        return forms.Select(ToSummaryDto).ToArray();
    }

    public async Task<FormSummaryDto> CreateFormAsync(CreateFormRequest request, CancellationToken cancellationToken)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var description = NormalizeOptionalText(request.Description);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new FormManagementException(StatusCodes.Status400BadRequest, "Form name is required.");
        }

        var form = new FormDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Status = FormStatuses.Draft,
            CurrentVersionId = null
        };

        dbContext.Forms.Add(form);
        AddAudit("Form", form.Id, "form_created");
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToSummaryDto(form);
    }

    public async Task<FormDetailDto> GetFormAsync(Guid formId, CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new FormManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        return ToDetailDto(form);
    }

    public async Task<PublishedFormSubmissionDto> GetPublishedFormForSubmissionAsync(
        Guid formId,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new FormManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        if (!string.Equals(form.Status, FormStatuses.Published, StringComparison.Ordinal)
            || form.CurrentVersionId is null
            || form.CurrentVersion is null)
        {
            throw new FormManagementException(StatusCodes.Status409Conflict, "Only published forms can be submitted.");
        }

        var schema = DeserializeSchema(form.CurrentVersion.SchemaJson);
        if (schema is null)
        {
            throw new FormManagementException(StatusCodes.Status409Conflict, "Published form version schema is invalid.");
        }

        var validation = FormSchemaValidator.ValidateSchema(schema);
        if (!validation.Valid)
        {
            throw new FormManagementException(
                StatusCodes.Status409Conflict,
                "Published form version schema is invalid.",
                validation.Errors);
        }

        return new PublishedFormSubmissionDto(
            form.Id,
            form.Name,
            form.Description,
            form.CurrentVersion.Id,
            form.CurrentVersion.VersionNumber,
            schema);
    }

    public async Task<FormDetailDto> UpdateDraftAsync(
        Guid formId,
        UpdateFormDraftRequest request,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new FormManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        if (string.Equals(form.Status, FormStatuses.Archived, StringComparison.Ordinal))
        {
            throw new FormManagementException(StatusCodes.Status409Conflict, "Archived forms cannot be edited.");
        }

        var validation = FormSchemaValidator.ValidateDraftSchema(request.Schema);
        if (!validation.Valid)
        {
            throw new FormManagementException(StatusCodes.Status400BadRequest, "Draft schema is invalid.", validation.Errors);
        }

        form.DraftSchemaJson = SerializeSchema(request.Schema);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(form);
    }

    public async Task<PublishFormResponse> PublishFormAsync(
        Guid formId,
        Guid? publishedById,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var form = await dbContext.Forms
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new FormManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        if (string.Equals(form.Status, FormStatuses.Archived, StringComparison.Ordinal))
        {
            throw new FormManagementException(StatusCodes.Status409Conflict, "Archived forms cannot be published.");
        }

        if (form.DraftSchemaJson is null)
        {
            throw new FormManagementException(StatusCodes.Status400BadRequest, "Save a backend draft before publishing.");
        }

        var schema = DeserializeSchema(form.DraftSchemaJson);
        if (schema is null)
        {
            throw new FormManagementException(StatusCodes.Status400BadRequest, "Draft schema is invalid.");
        }

        var validation = FormSchemaValidator.ValidateSchema(schema);
        if (!validation.Valid)
        {
            throw new FormManagementException(StatusCodes.Status400BadRequest, "Draft schema is not publishable.", validation.Errors);
        }

        var latestVersionNumber = await dbContext.FormVersions
            .Where(version => version.FormId == form.Id)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var publishedAt = DateTimeOffset.UtcNow;
        var version = new FormVersion
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            VersionNumber = latestVersionNumber + 1,
            SchemaJson = SerializeSchema(schema),
            PublishedById = publishedById,
            PublishedAt = publishedAt
        };

        dbContext.FormVersions.Add(version);
        form.CurrentVersionId = version.Id;
        form.Status = FormStatuses.Published;
        form.CurrentVersion = version;
        AddAudit("Form", form.Id, "form_published", publishedById);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PublishFormResponse(
            ToDetailDto(form),
            new PublishedFormVersionDto(
                version.Id,
                version.FormId,
                version.VersionNumber,
                schema,
                version.PublishedById,
                version.PublishedAt ?? publishedAt));
    }

    private static FormSummaryDto ToSummaryDto(FormDefinition form)
    {
        return new FormSummaryDto(
            form.Id,
            form.Name,
            form.Description,
            form.Status,
            GetFieldCount(form),
            form.CurrentVersionId,
            form.ConcurrencyStamp,
            form.CreatedAt,
            form.CreatedById,
            form.UpdatedAt,
            form.UpdatedById);
    }

    private static FormDetailDto ToDetailDto(FormDefinition form)
    {
        var draftSchema = DeserializeSchema(form.DraftSchemaJson);

        return new FormDetailDto(
            form.Id,
            form.Name,
            form.Description,
            form.Status,
            draftSchema?.Fields.Count ?? GetFieldCount(form),
            form.CurrentVersionId,
            draftSchema,
            form.ConcurrencyStamp,
            form.CreatedAt,
            form.CreatedById,
            form.UpdatedAt,
            form.UpdatedById);
    }

    private void AddAudit(string entityType, Guid entityId, string action, Guid? userId = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId
        });
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int GetFieldCount(FormDefinition form)
    {
        return CountFields(form.DraftSchemaJson) ?? CountFields(form.CurrentVersion?.SchemaJson) ?? 0;
    }

    private static int? CountFields(JsonDocument? schemaJson)
    {
        if (schemaJson is null)
        {
            return null;
        }

        var hasFields = schemaJson.RootElement.TryGetProperty("fields", out var fields)
            || schemaJson.RootElement.TryGetProperty("Fields", out fields);

        return hasFields && fields.ValueKind == JsonValueKind.Array
            ? fields.GetArrayLength()
            : null;
    }

    private static JsonDocument SerializeSchema(FormSchemaDefinition schema)
    {
        return JsonSerializer.SerializeToDocument(schema, JsonOptions);
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }
}
