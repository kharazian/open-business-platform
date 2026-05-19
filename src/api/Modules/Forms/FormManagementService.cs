using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OpenBusinessPlatform.Api.Modules.Forms;

public sealed class FormManagementService
{
    private readonly OpenBusinessPlatformDbContext dbContext;

    public FormManagementService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<FormSummaryDto>> ListFormsAsync(CancellationToken cancellationToken)
    {
        var forms = await dbContext.Forms
            .AsNoTracking()
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

    private static FormSummaryDto ToSummaryDto(FormDefinition form)
    {
        return new FormSummaryDto(
            form.Id,
            form.Name,
            form.Description,
            form.Status,
            FieldCount: 0,
            form.CurrentVersionId,
            form.ConcurrencyStamp,
            form.CreatedAt,
            form.CreatedById,
            form.UpdatedAt,
            form.UpdatedById);
    }

    private void AddAudit(string entityType, Guid entityId, string action)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action
        });
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
