using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Workflows;

public sealed class WorkflowDefinitionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public WorkflowDefinitionService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<WorkflowSummaryDto>> ListWorkflowsAsync(Guid formId, CancellationToken cancellationToken)
    {
        var formExists = await dbContext.Forms
            .AsNoTracking()
            .AnyAsync(form => form.Id == formId && !form.IsDeleted, cancellationToken);

        if (!formExists)
        {
            throw new WorkflowManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var workflows = await dbContext.Workflows
            .AsNoTracking()
            .Include(workflow => workflow.CurrentVersion)
            .Where(workflow => workflow.FormId == formId && !workflow.IsDeleted)
            .OrderByDescending(workflow => workflow.UpdatedAt ?? workflow.CreatedAt)
            .ThenBy(workflow => workflow.Name)
            .ToArrayAsync(cancellationToken);

        return workflows.Select(ToSummaryDto).ToArray();
    }

    public async Task<WorkflowDetailDto> CreateWorkflowAsync(
        Guid formId,
        CreateWorkflowDefinitionRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new WorkflowManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var validation = await ValidateCreateRequestAsync(request, cancellationToken);

        if (!validation.Valid)
        {
            throw new WorkflowManagementException(StatusCodes.Status400BadRequest, "Workflow definition is invalid.", validation.Errors);
        }

        var config = WorkflowDefinitionValidator.NormalizeConfig(request.Config);
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            Status = WorkflowDefinitionStatuses.Draft,
            IsEnabled = request.IsEnabled,
            HasUnpublishedChanges = false,
            DraftConfigJson = Serialize(config),
            CreatedById = createdById
        };

        dbContext.Workflows.Add(workflow);
        AddAudit(workflow.Id, "workflow_created", createdById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(workflow);
    }

    public async Task<WorkflowDetailDto> GetWorkflowAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == workflowId && !candidate.IsDeleted, cancellationToken);

        if (workflow is null)
        {
            throw new WorkflowManagementException(StatusCodes.Status404NotFound, "Workflow was not found.");
        }

        return ToDetailDto(workflow);
    }

    public async Task<WorkflowDetailDto> UpdateWorkflowAsync(
        Guid workflowId,
        UpdateWorkflowDefinitionRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == workflowId && !candidate.IsDeleted, cancellationToken);

        if (workflow is null)
        {
            throw new WorkflowManagementException(StatusCodes.Status404NotFound, "Workflow was not found.");
        }

        EnsureConcurrencyStamp(workflow.ConcurrencyStamp, request.ConcurrencyStamp);

        var validation = await ValidateUpdateRequestAsync(request, cancellationToken);

        if (!validation.Valid)
        {
            throw new WorkflowManagementException(StatusCodes.Status400BadRequest, "Workflow definition is invalid.", validation.Errors);
        }

        var wasEnabled = workflow.IsEnabled;
        var config = WorkflowDefinitionValidator.NormalizeConfig(request.Config);
        workflow.Name = request.Name.Trim();
        workflow.Description = NormalizeOptionalText(request.Description);
        workflow.DraftConfigJson = Serialize(config);
        workflow.IsEnabled = request.IsEnabled;
        workflow.HasUnpublishedChanges = workflow.CurrentVersionId is not null;
        workflow.Status = workflow.CurrentVersionId is null ? WorkflowDefinitionStatuses.Draft : WorkflowDefinitionStatuses.Published;
        workflow.UpdatedById = updatedById;

        AddAudit(workflow.Id, "workflow_updated", updatedById);

        if (wasEnabled && !workflow.IsEnabled)
        {
            AddAudit(workflow.Id, "workflow_disabled", updatedById);
        }
        else if (!wasEnabled && workflow.IsEnabled)
        {
            AddAudit(workflow.Id, "workflow_enabled", updatedById);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(workflow);
    }

    public async Task<WorkflowDetailDto> PublishWorkflowAsync(
        Guid workflowId,
        WorkflowStateChangeRequest request,
        Guid? publishedById,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .Include(candidate => candidate.CurrentVersion)
            .Include(candidate => candidate.Versions)
            .FirstOrDefaultAsync(candidate => candidate.Id == workflowId && !candidate.IsDeleted, cancellationToken);

        if (workflow is null)
        {
            throw new WorkflowManagementException(StatusCodes.Status404NotFound, "Workflow was not found.");
        }

        EnsureConcurrencyStamp(workflow.ConcurrencyStamp, request.ConcurrencyStamp);

        var config = DeserializeConfig(workflow.DraftConfigJson);
        var validation = await ValidateConfigAsync(workflow.Name, config, cancellationToken);

        if (!validation.Valid)
        {
            throw new WorkflowManagementException(StatusCodes.Status400BadRequest, "Workflow definition is invalid.", validation.Errors);
        }

        var now = DateTimeOffset.UtcNow;
        var normalizedConfig = WorkflowDefinitionValidator.NormalizeConfig(config);
        var versionNumber = workflow.Versions.Count == 0 ? 1 : workflow.Versions.Max(version => version.VersionNumber) + 1;
        var version = new WorkflowDefinitionVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflow.Id,
            WorkflowDefinition = workflow,
            FormId = workflow.FormId,
            VersionNumber = versionNumber,
            ConfigJson = Serialize(normalizedConfig),
            CreatedById = publishedById,
            PublishedById = publishedById,
            PublishedAt = now
        };

        dbContext.WorkflowVersions.Add(version);

        workflow.Status = WorkflowDefinitionStatuses.Published;
        workflow.CurrentVersionId = version.Id;
        workflow.CurrentVersion = version;
        workflow.HasUnpublishedChanges = false;
        workflow.UpdatedById = publishedById;

        AddAudit(workflow.Id, "workflow_published", publishedById, new { versionId = version.Id, versionNumber });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(workflow);
    }

    public async Task<WorkflowDetailDto> EnableWorkflowAsync(
        Guid workflowId,
        WorkflowStateChangeRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        return await SetWorkflowEnabledAsync(workflowId, request.ConcurrencyStamp, true, updatedById, cancellationToken);
    }

    public async Task<WorkflowDetailDto> DisableWorkflowAsync(
        Guid workflowId,
        WorkflowStateChangeRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        return await SetWorkflowEnabledAsync(workflowId, request.ConcurrencyStamp, false, updatedById, cancellationToken);
    }

    public async Task<Guid?> GetWorkflowFormIdAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        return await dbContext.Workflows
            .AsNoTracking()
            .Where(workflow => workflow.Id == workflowId && !workflow.IsDeleted)
            .Select(workflow => (Guid?)workflow.FormId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<WorkflowDetailDto> SetWorkflowEnabledAsync(
        Guid workflowId,
        string concurrencyStamp,
        bool isEnabled,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var workflow = await dbContext.Workflows
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == workflowId && !candidate.IsDeleted, cancellationToken);

        if (workflow is null)
        {
            throw new WorkflowManagementException(StatusCodes.Status404NotFound, "Workflow was not found.");
        }

        EnsureConcurrencyStamp(workflow.ConcurrencyStamp, concurrencyStamp);

        if (workflow.IsEnabled != isEnabled)
        {
            workflow.IsEnabled = isEnabled;
            workflow.UpdatedById = updatedById;
            AddAudit(workflow.Id, isEnabled ? "workflow_enabled" : "workflow_disabled", updatedById);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToDetailDto(workflow);
    }

    private async Task<WorkflowValidationResult> ValidateCreateRequestAsync(
        CreateWorkflowDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var activeUsers = await GetActiveUserIdsAsync(cancellationToken);
        var activeGroups = await GetActiveGroupIdsAsync(cancellationToken);
        var activeDepartments = await GetActiveDepartmentIdsAsync(cancellationToken);
        return WorkflowDefinitionValidator.Validate(request, activeUsers, activeGroups, activeDepartments);
    }

    private async Task<WorkflowValidationResult> ValidateUpdateRequestAsync(
        UpdateWorkflowDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var activeUsers = await GetActiveUserIdsAsync(cancellationToken);
        var activeGroups = await GetActiveGroupIdsAsync(cancellationToken);
        var activeDepartments = await GetActiveDepartmentIdsAsync(cancellationToken);
        return WorkflowDefinitionValidator.Validate(request, activeUsers, activeGroups, activeDepartments);
    }

    private async Task<WorkflowValidationResult> ValidateConfigAsync(
        string name,
        WorkflowDefinitionConfig config,
        CancellationToken cancellationToken)
    {
        var activeUsers = await GetActiveUserIdsAsync(cancellationToken);
        var activeGroups = await GetActiveGroupIdsAsync(cancellationToken);
        var activeDepartments = await GetActiveDepartmentIdsAsync(cancellationToken);
        return WorkflowDefinitionValidator.Validate(
            new CreateWorkflowDefinitionRequest(name, null, config, true),
            activeUsers,
            activeGroups,
            activeDepartments);
    }

    private async Task<IReadOnlyCollection<Guid>> GetActiveUserIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .Select(user => user.Id)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> GetActiveGroupIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Groups
            .AsNoTracking()
            .Where(group => group.IsActive)
            .Select(group => group.Id)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> GetActiveDepartmentIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .Select(department => department.Id)
            .ToArrayAsync(cancellationToken);
    }

    private void AddAudit(Guid workflowId, string action, Guid? userId, object? metadata = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Workflow",
            EntityId = workflowId,
            Action = action,
            UserId = userId,
            MetadataJson = metadata is null ? null : Serialize(metadata)
        });
    }

    private static WorkflowSummaryDto ToSummaryDto(WorkflowDefinition workflow)
    {
        var config = DeserializeConfig(workflow.DraftConfigJson);

        return new WorkflowSummaryDto(
            workflow.Id,
            workflow.FormId,
            workflow.Name,
            workflow.Description,
            workflow.Status,
            workflow.IsEnabled,
            workflow.HasUnpublishedChanges,
            workflow.CurrentVersionId,
            workflow.CurrentVersion?.VersionNumber,
            config.States.Count,
            config.Transitions.Count,
            config.ApprovalSteps.Count,
            workflow.ConcurrencyStamp,
            workflow.CreatedAt,
            workflow.CreatedById,
            workflow.UpdatedAt,
            workflow.UpdatedById);
    }

    private static WorkflowDetailDto ToDetailDto(WorkflowDefinition workflow)
    {
        return new WorkflowDetailDto(
            workflow.Id,
            workflow.FormId,
            workflow.Name,
            workflow.Description,
            workflow.Status,
            workflow.IsEnabled,
            workflow.HasUnpublishedChanges,
            workflow.CurrentVersionId,
            workflow.CurrentVersion?.VersionNumber,
            DeserializeConfig(workflow.DraftConfigJson),
            workflow.ConcurrencyStamp,
            workflow.CreatedAt,
            workflow.CreatedById,
            workflow.UpdatedAt,
            workflow.UpdatedById);
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string requestedStamp)
    {
        if (string.IsNullOrWhiteSpace(requestedStamp)
            || !string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new WorkflowManagementException(StatusCodes.Status409Conflict, "The workflow was changed by another user.");
        }
    }

    private static WorkflowDefinitionConfig DeserializeConfig(JsonDocument configJson)
    {
        var config = configJson.RootElement.Deserialize<WorkflowDefinitionConfig>(JsonOptions);
        return WorkflowDefinitionValidator.NormalizeConfig(config);
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
