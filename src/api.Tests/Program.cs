using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Security.Claims;
using OpenBusinessPlatform.Api.Application.Common;
using OpenBusinessPlatform.Api.Configuration;
using OpenBusinessPlatform.Api.Domain.Common;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Dashboard;
using OpenBusinessPlatform.Api.Modules.Dashboards;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Printing;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;
using OpenBusinessPlatform.Api.Modules.Triggers;
using OpenBusinessPlatform.Api.Modules.Workflows;
using OpenBusinessPlatform.Api.Platform;

var configuredDirectory = new BootstrapAdminUserDirectory(Options.Create(new BootstrapAdminOptions
{
    Email = "Admin@Company.Test",
    Password = "correct-password"
}));

var validUser = configuredDirectory.ValidateCredentials(new LoginRequest("admin@company.test", "correct-password"));

AssertNotNull(validUser, "Configured bootstrap admin credentials should sign in.");
AssertEqual("bootstrap-admin", validUser!.Id, "Bootstrap admin should use a stable id.");
AssertEqual("admin@company.test", validUser.Email, "Bootstrap admin email should be normalized.");
AssertEqual("Platform Admin", validUser.Name, "Bootstrap admin should expose a display name.");
AssertSequenceEqual(new[] { "Admin" }, validUser.Roles, "Bootstrap admin should receive the Admin role.");

var wrongPasswordUser = configuredDirectory.ValidateCredentials(new LoginRequest("admin@company.test", "wrong-password"));
AssertNull(wrongPasswordUser, "Wrong bootstrap admin password should not sign in.");

var wrongEmailUser = configuredDirectory.ValidateCredentials(new LoginRequest("viewer@company.test", "correct-password"));
AssertNull(wrongEmailUser, "Wrong bootstrap admin email should not sign in.");

var missingConfigurationDirectory = new BootstrapAdminUserDirectory(Options.Create(new BootstrapAdminOptions()));
var missingConfigurationUser = missingConfigurationDirectory.ValidateCredentials(new LoginRequest("admin@company.test", "correct-password"));
AssertNull(missingConfigurationUser, "Missing bootstrap admin configuration should disable login.");

using (var appSettings = JsonDocument.Parse(File.ReadAllText(GetRepositoryFilePath("src", "api", "appsettings.json"))))
{
    var configuredPostgres = appSettings
        .RootElement
        .GetProperty("ConnectionStrings")
        .GetProperty("Postgres")
        .GetString();

    AssertEqual(
        "Host=localhost;Port=55432;Database=open_business_platform;Username=obp;Password=obp_dev_password",
        configuredPostgres,
        "Checked-in API appsettings should match the project Compose PostgreSQL host port.");
}

RunWithEnvironment(
    new Dictionary<string, string?>
    {
        ["AUTH_COOKIE_NAME"] = "obp_test.auth",
        ["AUTH_COOKIE_REQUIRE_SECURE"] = "false",
        ["Authentication__CookieName"] = null,
        ["Authentication__RequireSecureCookies"] = null,
        ["ConnectionStrings__Postgres"] = null,
        ["POSTGRES_HOST"] = null,
        ["POSTGRES_PORT"] = null,
        ["POSTGRES_DB"] = null,
        ["POSTGRES_USER"] = null,
        ["POSTGRES_PASSWORD"] = null,
        ["API_PORT"] = "5099",
        ["ASPNETCORE_URLS"] = null,
        ["VITE_APP_HOST"] = "127.0.0.1",
        ["VITE_APP_PORT"] = "5199",
        ["Cors__AllowedOrigins__0"] = null,
        ["Cors__AllowedOrigins__1"] = null
    },
    () =>
    {
        EnvironmentConfiguration.ApplyDerivedValues();

        AssertEqual("obp_test.auth", Environment.GetEnvironmentVariable("Authentication__CookieName"), "Auth cookie name should be configurable per local clone.");
        AssertEqual("false", Environment.GetEnvironmentVariable("Authentication__RequireSecureCookies"), "Secure auth cookies should be configurable for temporary HTTP-only staging.");
        AssertEqual(
            "Host=localhost;Port=55432;Database=open_business_platform;Username=obp;Password=obp_dev_password",
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres"),
            "Host-run API development should default to the project Compose PostgreSQL port instead of the common local PostgreSQL port.");
        AssertEqual("http://localhost:5099", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"), "API URL should be derived from API_PORT when not explicitly set.");
        AssertEqual("http://127.0.0.1:5199", Environment.GetEnvironmentVariable("Cors__AllowedOrigins__0"), "CORS should include the configured Vite host and port.");
        AssertEqual("http://localhost:5199", Environment.GetEnvironmentVariable("Cors__AllowedOrigins__1"), "CORS should still include localhost for browser fallback.");
    });

var dbOptions = new DbContextOptionsBuilder<OpenBusinessPlatformDbContext>()
    .UseNpgsql("Host=localhost;Database=open_business_platform_model_test;Username=obp;Password=obp_dev_password")
    .Options;
using var dbContext = new OpenBusinessPlatformDbContext(dbOptions);
var model = dbContext.Model;

AssertTable<User>(model, "users");
AssertTable<PasswordResetToken>(model, "password_reset_tokens");
AssertTable<Role>(model, "roles");
AssertTable<UserRole>(model, "user_roles");
AssertTable<RolePermission>(model, "role_permissions");
AssertTable<RoleFormPermission>(model, "role_form_permissions");
AssertTable<Group>(model, "groups");
AssertTable<UserGroup>(model, "user_groups");
AssertTable<RoleReportPermission>(model, "role_report_permissions");
AssertTable<RoleFieldPermission>(model, "role_field_permissions");
AssertTable<Department>(model, "departments");
AssertTable<UserDepartment>(model, "user_departments");
AssertTable<FormDefinition>(model, "forms");
AssertTable<FormVersion>(model, "form_versions");
AssertTable<FormRecord>(model, "records");
AssertTable<ReportDefinition>(model, "reports");
AssertTable<DashboardDefinition>(model, "dashboards");
AssertTable<TriggerDefinition>(model, "triggers");
AssertTable<TriggerExecutionLog>(model, "trigger_logs");
AssertTable<WorkflowDefinition>(model, "workflow_definitions");
AssertTable<WorkflowDefinitionVersion>(model, "workflow_definition_versions");
AssertTable<WorkflowHistoryEntry>(model, "workflow_history");
AssertTable<WorkflowApprovalTask>(model, "workflow_approval_tasks");
AssertTable<PrintTemplate>(model, "print_templates");
AssertTable<PrintTemplateVersion>(model, "print_template_versions");
AssertTable<Notification>(model, "notifications");
AssertTable<NotificationPreference>(model, "notification_preferences");
AssertTable<AuditLogEntry>(model, "audit_logs");

AssertTypeAssignable<AuditedAggregateRoot<Guid>, User>();
AssertTypeAssignable<Entity<Guid>, PasswordResetToken>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, Role>();
AssertTypeAssignable<Entity<Guid>, RolePermission>();
AssertTypeAssignable<Entity<Guid>, RoleFormPermission>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, Group>();
AssertTypeAssignable<Entity<Guid>, UserGroup>();
AssertTypeAssignable<Entity<Guid>, RoleReportPermission>();
AssertTypeAssignable<Entity<Guid>, RoleFieldPermission>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, Department>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, FormDefinition>();
AssertTypeAssignable<CreationAuditedEntity<Guid>, FormVersion>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, FormRecord>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, ReportDefinition>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, DashboardDefinition>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, TriggerDefinition>();
AssertTypeAssignable<Entity<Guid>, TriggerExecutionLog>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, WorkflowDefinition>();
AssertTypeAssignable<CreationAuditedEntity<Guid>, WorkflowDefinitionVersion>();
AssertTypeAssignable<CreationAuditedEntity<Guid>, WorkflowHistoryEntry>();
AssertTypeAssignable<AuditedEntity<Guid>, WorkflowApprovalTask>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, PrintTemplate>();
AssertTypeAssignable<CreationAuditedEntity<Guid>, PrintTemplateVersion>();
AssertTypeAssignable<Entity<Guid>, Notification>();
AssertTypeAssignable<Entity<Guid>, NotificationPreference>();
AssertTypeAssignable<Entity<Guid>, AuditLogEntry>();

AssertGuidId<User>(model);
AssertGuidId<PasswordResetToken>(model);
AssertGuidId<Role>(model);
AssertGuidId<RolePermission>(model);
AssertGuidId<RoleFormPermission>(model);
AssertGuidId<Group>(model);
AssertGuidId<RoleReportPermission>(model);
AssertGuidId<RoleFieldPermission>(model);
AssertGuidId<Department>(model);
AssertGuidId<FormDefinition>(model);
AssertGuidId<FormVersion>(model);
AssertGuidId<FormRecord>(model);
AssertGuidId<ReportDefinition>(model);
AssertGuidId<DashboardDefinition>(model);
AssertGuidId<TriggerDefinition>(model);
AssertGuidId<TriggerExecutionLog>(model);
AssertGuidId<WorkflowDefinition>(model);
AssertGuidId<WorkflowDefinitionVersion>(model);
AssertGuidId<WorkflowHistoryEntry>(model);
AssertGuidId<WorkflowApprovalTask>(model);
AssertGuidId<PrintTemplate>(model);
AssertGuidId<PrintTemplateVersion>(model);
AssertGuidId<Notification>(model);
AssertGuidId<NotificationPreference>(model);
AssertGuidId<AuditLogEntry>(model);

AssertUniqueIndex<User>(model, new[] { nameof(User.Email) }, "Users should have a unique email index.");
AssertUniqueIndex<Role>(model, new[] { nameof(Role.Name) }, "Roles should have a unique role name index.");
AssertUniqueIndex<RolePermission>(model, new[] { nameof(RolePermission.RoleId), nameof(RolePermission.Permission) }, "Role permissions should be unique per role/permission.");
AssertUniqueIndex<RoleFormPermission>(model, new[] { nameof(RoleFormPermission.RoleId), nameof(RoleFormPermission.FormId), nameof(RoleFormPermission.Action) }, "Role form permissions should be unique per role/form/action.");
AssertUniqueIndex<Group>(model, new[] { nameof(Group.Name) }, "Groups should have a unique group name index.");
AssertUniqueIndex<UserGroup>(model, new[] { nameof(UserGroup.UserId), nameof(UserGroup.GroupId) }, "User groups should be unique per user/group.");
AssertUniqueIndex<RoleReportPermission>(model, new[] { nameof(RoleReportPermission.RoleId), nameof(RoleReportPermission.ReportId), nameof(RoleReportPermission.Action) }, "Report permissions should be unique per role/report/action.");
AssertUniqueIndex<RoleFieldPermission>(model, new[] { nameof(RoleFieldPermission.RoleId), nameof(RoleFieldPermission.FormId), nameof(RoleFieldPermission.FieldId) }, "Field permissions should be unique per role/form/field.");
AssertUniqueIndex<FormVersion>(model, new[] { nameof(FormVersion.FormId), nameof(FormVersion.VersionNumber) }, "Form versions should be unique per form/version number.");
AssertUniqueIndex<WorkflowDefinitionVersion>(model, new[] { nameof(WorkflowDefinitionVersion.WorkflowDefinitionId), nameof(WorkflowDefinitionVersion.VersionNumber) }, "Workflow definition versions should be unique per workflow/version number.");
AssertUniqueIndex<PrintTemplateVersion>(model, new[] { nameof(PrintTemplateVersion.PrintTemplateId), nameof(PrintTemplateVersion.VersionNumber) }, "Print template versions should be unique per template/version number.");

AssertJsonColumn<FormVersion>(model, nameof(FormVersion.SchemaJson));
AssertJsonColumn<FormVersion>(model, nameof(FormVersion.LayoutJson));
AssertJsonColumn<FormVersion>(model, nameof(FormVersion.ValidationJson));
AssertJsonColumn<FormRecord>(model, nameof(FormRecord.ValuesJson));
AssertJsonColumn<ReportDefinition>(model, nameof(ReportDefinition.ConfigJson));
AssertJsonColumn<DashboardDefinition>(model, nameof(DashboardDefinition.ConfigJson));
AssertJsonColumn<DashboardDefinition>(model, nameof(DashboardDefinition.LayoutJson));
AssertJsonColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ConditionsJson));
AssertJsonColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ActionsJson));
AssertJsonColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.InputJson));
AssertJsonColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.ResultJson));
AssertJsonColumn<WorkflowDefinition>(model, nameof(WorkflowDefinition.DraftConfigJson));
AssertJsonColumn<WorkflowDefinitionVersion>(model, nameof(WorkflowDefinitionVersion.ConfigJson));
AssertJsonColumn<WorkflowHistoryEntry>(model, nameof(WorkflowHistoryEntry.MetadataJson));
AssertJsonColumn<PrintTemplate>(model, nameof(PrintTemplate.ConfigJson));
AssertJsonColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.ConfigJson));
AssertJsonColumn<Notification>(model, nameof(Notification.MetadataJson));
AssertJsonColumn<AuditLogEntry>(model, nameof(AuditLogEntry.BeforeJson));
AssertJsonColumn<AuditLogEntry>(model, nameof(AuditLogEntry.AfterJson));
AssertJsonColumn<AuditLogEntry>(model, nameof(AuditLogEntry.MetadataJson));
AssertJsonColumn<User>(model, nameof(User.ExtraPropertiesJson));
AssertJsonColumn<Role>(model, nameof(Role.ExtraPropertiesJson));
AssertJsonColumn<Department>(model, nameof(Department.ExtraPropertiesJson));
AssertJsonColumn<FormDefinition>(model, nameof(FormDefinition.ExtraPropertiesJson));
AssertJsonColumn<FormRecord>(model, nameof(FormRecord.ExtraPropertiesJson));
AssertJsonColumn<ReportDefinition>(model, nameof(ReportDefinition.ExtraPropertiesJson));
AssertJsonColumn<DashboardDefinition>(model, nameof(DashboardDefinition.ExtraPropertiesJson));
AssertJsonColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ExtraPropertiesJson));
AssertJsonColumn<WorkflowDefinition>(model, nameof(WorkflowDefinition.ExtraPropertiesJson));
AssertJsonColumn<PrintTemplate>(model, nameof(PrintTemplate.ExtraPropertiesJson));
AssertJsonColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.ExtraPropertiesJson));

AssertColumn<User>(model, nameof(User.PasswordHash), "password_hash", "Users should store a password hash column.");
AssertColumn<User>(model, nameof(User.PasswordUpdatedAt), "password_updated_at", "Users should store password update metadata.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.UserId), "user_id", "Password reset tokens should be tied to users.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.TokenHash), "token_hash", "Password reset tokens should store only token hashes.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.ExpiresAt), "expires_at", "Password reset tokens should expire.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.UsedAt), "used_at", "Password reset tokens should track use.");
AssertColumn<RoleFormPermission>(model, nameof(RoleFormPermission.Scope), "scope", "Role form permissions should store a record access scope.");
AssertColumn<NotificationPreference>(model, nameof(NotificationPreference.InAppEnabled), "in_app_enabled", "Notification preferences should store in-app delivery choice.");
AssertColumn<NotificationPreference>(model, nameof(NotificationPreference.ShowUnreadBadge), "show_unread_badge", "Notification preferences should store unread badge choice.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryAttemptCount), "auto_retry_attempt_count", "Trigger logs should store automatic retry attempt count.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryMaxAttempts), "auto_retry_max_attempts", "Trigger logs should store automatic retry max attempts.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryNextAttemptAt), "auto_retry_next_attempt_at", "Trigger logs should store the next automatic retry time.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryLockedAt), "auto_retry_locked_at", "Trigger logs should store automatic retry lock metadata.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryCompletedAt), "auto_retry_completed_at", "Trigger logs should store automatic retry completion.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryExhaustedAt), "auto_retry_exhausted_at", "Trigger logs should store automatic retry exhaustion.");
AssertColumn<TriggerExecutionLog>(model, nameof(TriggerExecutionLog.AutoRetryDisabledAt), "auto_retry_disabled_at", "Trigger logs should store disabled-trigger retry skips.");
AssertColumn<TriggerDefinition>(model, nameof(TriggerDefinition.AutoRetryEnabled), "auto_retry_enabled", "Triggers should store whether automatic retries are enabled.");
AssertColumn<TriggerDefinition>(model, nameof(TriggerDefinition.AutoRetryMaxAttempts), "auto_retry_max_attempts", "Triggers should store user-authored retry attempt limits.");
AssertColumn<TriggerDefinition>(model, nameof(TriggerDefinition.AutoRetryDelaySeconds), "auto_retry_delay_seconds", "Triggers should store user-authored retry delay seconds.");
AssertJsonColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ScheduleJson));
AssertColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ScheduleNextRunAt), "schedule_next_run_at", "Scheduled triggers should store their next due run.");
AssertColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ScheduleLastRunAt), "schedule_last_run_at", "Scheduled triggers should store their last run metadata.");
AssertColumn<WorkflowDefinition>(model, nameof(WorkflowDefinition.Status), "status", "Workflow definitions should store draft/published status.");
AssertColumn<WorkflowDefinition>(model, nameof(WorkflowDefinition.CurrentVersionId), "current_version_id", "Workflow definitions should point at the current published version.");
AssertColumn<WorkflowDefinition>(model, nameof(WorkflowDefinition.IsEnabled), "is_enabled", "Workflow definitions should store enabled state.");
AssertColumn<WorkflowDefinition>(model, nameof(WorkflowDefinition.HasUnpublishedChanges), "has_unpublished_changes", "Workflow definitions should track unpublished draft changes.");
AssertColumn<WorkflowDefinitionVersion>(model, nameof(WorkflowDefinitionVersion.WorkflowDefinitionId), "workflow_definition_id", "Workflow versions should point at their workflow definition.");
AssertColumn<WorkflowDefinitionVersion>(model, nameof(WorkflowDefinitionVersion.VersionNumber), "version_number", "Workflow versions should store a stable version number.");
AssertColumn<WorkflowHistoryEntry>(model, nameof(WorkflowHistoryEntry.WorkflowDefinitionId), "workflow_definition_id", "Workflow history should link to a workflow definition.");
AssertColumn<WorkflowHistoryEntry>(model, nameof(WorkflowHistoryEntry.WorkflowDefinitionVersionId), "workflow_definition_version_id", "Workflow history should link to the workflow version used.");
AssertColumn<WorkflowHistoryEntry>(model, nameof(WorkflowHistoryEntry.RecordId), "record_id", "Workflow history should link to a record.");
AssertColumn<WorkflowApprovalTask>(model, nameof(WorkflowApprovalTask.ApprovalGroupId), "approval_group_id", "Workflow approvals should group tasks created for one approval request.");
AssertColumn<WorkflowApprovalTask>(model, nameof(WorkflowApprovalTask.WorkflowDefinitionVersionId), "workflow_definition_version_id", "Workflow approvals should link to the workflow version used.");
AssertColumn<WorkflowApprovalTask>(model, nameof(WorkflowApprovalTask.AssignedToUserId), "assigned_to_user_id", "Workflow approvals should be assigned to a user.");
AssertColumn<WorkflowApprovalTask>(model, nameof(WorkflowApprovalTask.Status), "status", "Workflow approvals should store task status.");
AssertColumn<FormRecord>(model, nameof(FormRecord.WorkflowDefinitionId), "workflow_definition_id", "Records should store the active workflow definition.");
AssertColumn<FormRecord>(model, nameof(FormRecord.WorkflowDefinitionVersionId), "workflow_definition_version_id", "Records should store the active workflow definition version.");
AssertColumn<FormRecord>(model, nameof(FormRecord.WorkflowStateKey), "workflow_state_key", "Records should store the current workflow state key.");
AssertColumn<PrintTemplate>(model, nameof(PrintTemplate.FormId), "form_id", "Print templates should be scoped to forms.");
AssertColumn<PrintTemplate>(model, nameof(PrintTemplate.ReportId), "report_id", "Report print templates should optionally target reports.");
AssertColumn<PrintTemplate>(model, nameof(PrintTemplate.Type), "type", "Print templates should store record/report type.");
AssertColumn<PrintTemplate>(model, nameof(PrintTemplate.ConfigJson), "config_json", "Print templates should store JSONB layout config.");
AssertColumn<PrintTemplate>(model, nameof(PrintTemplate.CurrentVersionId), "current_version_id", "Print templates should point at the latest published version.");
AssertColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.PrintTemplateId), "print_template_id", "Print template versions should point at their draft template.");
AssertColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.FormId), "form_id", "Print template versions should retain form scope.");
AssertColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.ReportId), "report_id", "Print template versions should retain report scope.");
AssertColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.VersionNumber), "version_number", "Print template versions should store sequential version numbers.");
AssertColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.PublishedAt), "published_at", "Print template versions should store publish time.");
AssertColumn<PrintTemplateVersion>(model, nameof(PrintTemplateVersion.PublishedById), "published_by_id", "Print template versions should store publisher metadata.");

AssertUniqueIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.TokenHash) }, "Password reset token hashes should be unique.");
AssertIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.UserId) }, "Password reset tokens should be indexed by user.");
AssertIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.ExpiresAt) }, "Password reset tokens should be indexed by expiry.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.FormId) }, "Records should be indexed by form.");
AssertIndex<PrintTemplate>(model, new[] { nameof(PrintTemplate.FormId) }, "Print templates should be indexed by form.");
AssertIndex<PrintTemplate>(model, new[] { nameof(PrintTemplate.ReportId) }, "Print templates should be indexed by report.");
AssertIndex<PrintTemplate>(model, new[] { nameof(PrintTemplate.Type) }, "Print templates should be indexed by type.");
AssertIndex<PrintTemplate>(model, new[] { nameof(PrintTemplate.CurrentVersionId) }, "Print templates should be indexed by current published version.");
AssertIndex<PrintTemplateVersion>(model, new[] { nameof(PrintTemplateVersion.PrintTemplateId) }, "Print template versions should be indexed by template.");
AssertIndex<PrintTemplateVersion>(model, new[] { nameof(PrintTemplateVersion.FormId) }, "Print template versions should be indexed by form.");
AssertIndex<PrintTemplateVersion>(model, new[] { nameof(PrintTemplateVersion.ReportId) }, "Print template versions should be indexed by report.");
AssertIndex<PrintTemplateVersion>(model, new[] { nameof(PrintTemplateVersion.PublishedAt) }, "Print template versions should be indexed by publish time.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.FormVersionId) }, "Records should be indexed by form version.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.Status) }, "Records should be indexed by status.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.OwnerId) }, "Records should be indexed by owner.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.DepartmentId) }, "Records should be indexed by department.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.AssignedToUserId) }, "Records should be indexed by assigned user.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.AssignedGroupId) }, "Records should be indexed by assigned group.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.WorkflowDefinitionId) }, "Records should be indexed by active workflow definition.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.WorkflowDefinitionVersionId) }, "Records should be indexed by active workflow version.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.WorkflowStateKey) }, "Records should be indexed by active workflow state.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.CreatedById) }, "Records should be indexed by creator.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.CreatedAt) }, "Records should be indexed by created date.");
AssertIndex<ReportDefinition>(model, new[] { nameof(ReportDefinition.FormId) }, "Reports should be indexed by form.");
AssertIndex<ReportDefinition>(model, new[] { nameof(ReportDefinition.Type) }, "Reports should be indexed by type.");
AssertIndex<ReportDefinition>(model, new[] { nameof(ReportDefinition.CreatedById) }, "Reports should be indexed by creator.");
AssertIndex<DashboardDefinition>(model, new[] { nameof(DashboardDefinition.CreatedById) }, "Dashboards should be indexed by creator.");
AssertIndex<DashboardDefinition>(model, new[] { nameof(DashboardDefinition.Name) }, "Dashboards should be indexed by name.");
AssertIndex<TriggerDefinition>(model, new[] { nameof(TriggerDefinition.FormId) }, "Triggers should be indexed by form.");
AssertIndex<TriggerDefinition>(model, new[] { nameof(TriggerDefinition.EventName) }, "Triggers should be indexed by event.");
AssertIndex<TriggerDefinition>(model, new[] { nameof(TriggerDefinition.IsEnabled) }, "Triggers should be indexed by enabled state.");
AssertIndex<TriggerDefinition>(model, new[] { nameof(TriggerDefinition.ScheduleNextRunAt) }, "Scheduled triggers should be indexed by next run time.");
AssertIndex<TriggerExecutionLog>(model, new[] { nameof(TriggerExecutionLog.TriggerId) }, "Trigger logs should be indexed by trigger.");
AssertIndex<TriggerExecutionLog>(model, new[] { nameof(TriggerExecutionLog.FormId) }, "Trigger logs should be indexed by form.");
AssertIndex<TriggerExecutionLog>(model, new[] { nameof(TriggerExecutionLog.EventName) }, "Trigger logs should be indexed by event.");
AssertIndex<TriggerExecutionLog>(model, new[] { nameof(TriggerExecutionLog.EntityType), nameof(TriggerExecutionLog.EntityId) }, "Trigger logs should be indexed by entity.");
AssertIndex<TriggerExecutionLog>(model, new[] { nameof(TriggerExecutionLog.CreatedAt) }, "Trigger logs should be indexed by creation time.");
AssertIndex<TriggerExecutionLog>(model, new[] { nameof(TriggerExecutionLog.AutoRetryNextAttemptAt) }, "Trigger logs should be indexed by automatic retry due time.");
AssertIndex<WorkflowDefinition>(model, new[] { nameof(WorkflowDefinition.FormId) }, "Workflow definitions should be indexed by form.");
AssertIndex<WorkflowDefinition>(model, new[] { nameof(WorkflowDefinition.Status) }, "Workflow definitions should be indexed by status.");
AssertIndex<WorkflowDefinition>(model, new[] { nameof(WorkflowDefinition.IsEnabled) }, "Workflow definitions should be indexed by enabled state.");
AssertIndex<WorkflowDefinitionVersion>(model, new[] { nameof(WorkflowDefinitionVersion.WorkflowDefinitionId) }, "Workflow versions should be indexed by definition.");
AssertIndex<WorkflowHistoryEntry>(model, new[] { nameof(WorkflowHistoryEntry.RecordId) }, "Workflow history should be indexed by record.");
AssertIndex<WorkflowHistoryEntry>(model, new[] { nameof(WorkflowHistoryEntry.WorkflowDefinitionVersionId) }, "Workflow history should be indexed by workflow version.");
AssertIndex<WorkflowApprovalTask>(model, new[] { nameof(WorkflowApprovalTask.AssignedToUserId), nameof(WorkflowApprovalTask.Status) }, "Workflow approvals should be indexed by assignee and status.");
AssertIndex<WorkflowApprovalTask>(model, new[] { nameof(WorkflowApprovalTask.ApprovalGroupId) }, "Workflow approvals should be indexed by approval group.");
AssertIndex<WorkflowApprovalTask>(model, new[] { nameof(WorkflowApprovalTask.RecordId), nameof(WorkflowApprovalTask.TransitionKey), nameof(WorkflowApprovalTask.Status) }, "Workflow approvals should be indexed by record transition status.");
AssertIndex<Notification>(model, new[] { nameof(Notification.UserId) }, "Notifications should be indexed by recipient user.");
AssertIndex<Notification>(model, new[] { nameof(Notification.ReadAt) }, "Notifications should be indexed by read state.");
AssertIndex<Notification>(model, new[] { nameof(Notification.CreatedAt) }, "Notifications should be indexed by creation time.");
AssertUniqueIndex<NotificationPreference>(model, new[] { nameof(NotificationPreference.UserId) }, "Notification preferences should be unique per user.");
AssertIndex<NotificationPreference>(model, new[] { nameof(NotificationPreference.UpdatedAt) }, "Notification preferences should be indexed by update date.");
AssertIndex<RolePermission>(model, new[] { nameof(RolePermission.RoleId) }, "Role permissions should be indexed by role.");
AssertIndex<RoleFormPermission>(model, new[] { nameof(RoleFormPermission.RoleId) }, "Role form permissions should be indexed by role.");
AssertIndex<RoleFormPermission>(model, new[] { nameof(RoleFormPermission.FormId) }, "Role form permissions should be indexed by form.");
AssertIndex<AuditLogEntry>(model, new[] { nameof(AuditLogEntry.EntityType), nameof(AuditLogEntry.EntityId) }, "Audit logs should be indexed by entity.");
AssertIndex<AuditLogEntry>(model, new[] { nameof(AuditLogEntry.UserId) }, "Audit logs should be indexed by user.");
AssertIndex<AuditLogEntry>(model, new[] { nameof(AuditLogEntry.CreatedAt) }, "Audit logs should be indexed by created date.");

var passwordHasher = new LocalPasswordHasher();
var passwordHash = passwordHasher.HashPassword("temporary-password-1");
AssertNotEqual("temporary-password-1", passwordHash, "Password hashes should not store the raw password.");
AssertTrue(passwordHasher.VerifyPassword("temporary-password-1", passwordHash), "Password hasher should verify the original password.");
AssertFalse(passwordHasher.VerifyPassword("wrong-password", passwordHash), "Password hasher should reject an incorrect password.");

var resetTokenGenerator = new PasswordResetTokenGenerator();
var rawResetToken = resetTokenGenerator.Generate();
AssertFalse(string.IsNullOrWhiteSpace(rawResetToken), "Password reset tokens should be non-empty.");
AssertTrue(rawResetToken.Length >= 43, "Password reset tokens should have enough entropy for email recovery.");

var resetTokenHasher = new PasswordResetTokenHasher();
var resetTokenHash = resetTokenHasher.Hash(rawResetToken);
AssertNotEqual(rawResetToken, resetTokenHash, "Password reset token hashes should not store the raw token.");
AssertTrue(resetTokenHasher.Verify(rawResetToken, resetTokenHash), "Password reset token hasher should verify the original token.");
AssertFalse(resetTokenHasher.Verify($"{rawResetToken}x", resetTokenHash), "Password reset token hasher should reject a different token.");

var retryPolicy = TriggerRetryPolicy.Default;
AssertEqual(3, retryPolicy.MaxAttempts, "Automatic trigger retries should default to three attempts.");
var customRetryPolicyDefinition = new TriggerRetryPolicyDefinition(true, 5, 300);
var customRetryPolicy = TriggerRetryPolicy.FromDefinition(customRetryPolicyDefinition);
AssertNotNull(customRetryPolicy, "Enabled user-authored retry policies should resolve to a runtime policy.");
AssertEqual(5, customRetryPolicy!.MaxAttempts, "User-authored retry policies should control max attempts.");
AssertEqual(TimeSpan.FromSeconds(300), customRetryPolicy.Delay, "User-authored retry policies should control retry delay.");
AssertNull(TriggerRetryPolicy.FromDefinition(new TriggerRetryPolicyDefinition(false, 5, 300)), "Disabled user-authored retry policies should not schedule retries.");
AssertEqual("pending", TriggerRetryStateResolver.Resolve(new TriggerExecutionLog
{
    Status = TriggerExecutionStatuses.Failed,
    AutoRetryMaxAttempts = retryPolicy.MaxAttempts,
    AutoRetryNextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(1)
}, triggerEnabled: true), "Failed logs with a next retry time should expose pending retry state.");
AssertEqual("exhausted", TriggerRetryStateResolver.Resolve(new TriggerExecutionLog
{
    Status = TriggerExecutionStatuses.Failed,
    AutoRetryAttemptCount = retryPolicy.MaxAttempts,
    AutoRetryMaxAttempts = retryPolicy.MaxAttempts,
    AutoRetryExhaustedAt = DateTimeOffset.UtcNow
}, triggerEnabled: true), "Failed logs at the maximum attempts should expose exhausted retry state.");
AssertEqual("disabled", TriggerRetryStateResolver.Resolve(new TriggerExecutionLog
{
    Status = TriggerExecutionStatuses.Failed,
    AutoRetryMaxAttempts = retryPolicy.MaxAttempts,
    AutoRetryDisabledAt = DateTimeOffset.UtcNow
}, triggerEnabled: false), "Failed logs skipped by disabled triggers should expose disabled retry state.");
var retryNow = DateTimeOffset.Parse("2026-06-04T12:00:00Z");
var scheduledRetryLog = new TriggerExecutionLog { Status = TriggerExecutionStatuses.Failed };
TriggerRetryScheduler.ScheduleInitialFailure(scheduledRetryLog, retryPolicy, retryNow);
AssertEqual(0, scheduledRetryLog.AutoRetryAttemptCount, "Initial automatic retry scheduling should not consume an attempt.");
AssertEqual(retryPolicy.MaxAttempts, scheduledRetryLog.AutoRetryMaxAttempts, "Initial automatic retry scheduling should store max attempts.");
AssertEqual(retryNow.Add(retryPolicy.Delay), scheduledRetryLog.AutoRetryNextAttemptAt, "Initial automatic retry scheduling should store the next attempt time.");
TriggerRetryScheduler.MarkAttemptFailed(scheduledRetryLog, retryPolicy, retryNow.AddMinutes(1));
AssertEqual(1, scheduledRetryLog.AutoRetryAttemptCount, "Failed automatic retries should increment attempt count.");
AssertEqual(retryNow.AddMinutes(1).Add(retryPolicy.Delay), scheduledRetryLog.AutoRetryNextAttemptAt, "Failed automatic retries should schedule the next attempt.");
scheduledRetryLog.AutoRetryAttemptCount = retryPolicy.MaxAttempts - 1;
TriggerRetryScheduler.MarkAttemptFailed(scheduledRetryLog, retryPolicy, retryNow.AddMinutes(2));
AssertNotNull(scheduledRetryLog.AutoRetryExhaustedAt, "Automatic retries should mark exhaustion after the final attempt.");
AssertNull(scheduledRetryLog.AutoRetryNextAttemptAt, "Exhausted automatic retries should clear next attempt metadata.");
AssertNotNull(typeof(TriggerAutomaticRetryService).GetMethod(nameof(TriggerAutomaticRetryService.ProcessDueRetriesAsync)), "Automatic retry service should expose due retry processing.");
AssertTypeAssignable<BackgroundService, TriggerRetryWorker>();

var resetLink = PasswordRecoveryEmailFactory.BuildResetLink("http://localhost:5174/reset-password", rawResetToken);
AssertTrue(resetLink.StartsWith("http://localhost:5174/reset-password?token=", StringComparison.Ordinal), "Password reset links should point to the configured reset page.");
AssertTrue(resetLink.Contains(Uri.EscapeDataString(rawResetToken), StringComparison.Ordinal), "Password reset links should include the raw token only in the email URL.");

var recoveryEmail = PasswordRecoveryEmailFactory.CreateResetEmail("jane@company.test", resetLink, TimeSpan.FromMinutes(60));
AssertEqual("jane@company.test", recoveryEmail.ToEmail, "Password recovery emails should target the requested user email.");
AssertTrue(recoveryEmail.Subject.Contains("password", StringComparison.OrdinalIgnoreCase), "Password recovery emails should describe the password reset.");
AssertTrue(recoveryEmail.TextBody.Contains(resetLink, StringComparison.Ordinal), "Password recovery emails should include the reset link.");
var pdfEmailAttachment = new EmailAttachment(
    "employee-record-v2.pdf",
    PrintPdfDocumentBuilder.ContentType,
    "%PDF-1.4 attachment"u8.ToArray());
var emailWithAttachment = new EmailMessage(
    "manager@example.test",
    "Employee record",
    "Attached.",
    Attachments: new[] { pdfEmailAttachment });
var emailAttachments = emailWithAttachment.Attachments ?? Array.Empty<EmailAttachment>();
AssertEqual(1, emailAttachments.Count, "Email messages should carry PDF attachments.");
AssertEqual(PrintPdfDocumentBuilder.ContentType, emailAttachments.Single().ContentType, "PDF email attachments should carry application/pdf content type.");

var demoSchema = DemoDataSeeder.CreateEmployeeInformationSchema();
AssertEqual(8, demoSchema.Fields.Count, "Demo seed data should include the V1 employee information fields.");
AssertTrue(demoSchema.Fields.Any(field => field.Id == "email" && field.Type == FormFieldTypes.Email), "Demo employee form should include an email field.");
var validRecordPrintTemplate = new PrintTemplateConfig(
    1,
    PrintTemplateTypes.Record,
    new PrintTemplateHeaderConfig("Employee record", null, null, true),
    new[] { new PrintTemplateSectionConfig("main", PrintTemplateSectionKinds.Fields, "Main", new[] { "email" }, Array.Empty<string>()) },
    new PrintTemplateFooterConfig("Open Business Platform"),
    new PrintTemplateLayoutConfig(PrintTemplatePageSizes.Letter, PrintTemplateOrientations.Portrait, PrintTemplateMargins.Normal, RepeatTableHeaders: true));
var invalidRecordPrintTemplate = validRecordPrintTemplate with
{
    Sections = new[] { new PrintTemplateSectionConfig("main", PrintTemplateSectionKinds.Fields, "Main", new[] { "missing_field" }, Array.Empty<string>()) }
};
var invalidLayoutPrintTemplate = validRecordPrintTemplate with
{
    Layout = new PrintTemplateLayoutConfig("tabloid", "sideways", "tiny", RepeatTableHeaders: true)
};
var logoRecordPrintTemplate = validRecordPrintTemplate with
{
    Header = validRecordPrintTemplate.Header with { LogoUrl = "data:image/png;base64,iVBORw0KGgo=" }
};
var invalidLogoPrintTemplate = validRecordPrintTemplate with
{
    Header = validRecordPrintTemplate.Header with { LogoUrl = "javascript:alert(1)" }
};
var paginatedRecordPrintTemplate = validRecordPrintTemplate with
{
    Sections = new[]
    {
        new PrintTemplateSectionConfig(
            "main",
            PrintTemplateSectionKinds.Fields,
            "Main",
            new[] { "email" },
            Array.Empty<string>(),
            new PrintTemplateSectionPaginationConfig(PageBreakBefore: true, AvoidBreakInside: false))
    }
};
var conditionalRecordPrintTemplate = validRecordPrintTemplate with
{
    Sections = new[]
    {
        new PrintTemplateSectionConfig(
            "main",
            PrintTemplateSectionKinds.Fields,
            "Main",
            new[] { "email" },
            Array.Empty<string>(),
            null,
            new[] { new PrintTemplateSectionConditionConfig("department", PrintTemplateConditionOperators.Equal, "Finance") })
    }
};
var invalidConditionPrintTemplate = validRecordPrintTemplate with
{
    Sections = new[]
    {
        new PrintTemplateSectionConfig(
            "main",
            PrintTemplateSectionKinds.Fields,
            "Main",
            new[] { "email" },
            Array.Empty<string>(),
            null,
            new[]
            {
                new PrintTemplateSectionConditionConfig("missing_field", PrintTemplateConditionOperators.Equal, "Finance"),
                new PrintTemplateSectionConditionConfig("department", "starts_with", "Fin"),
                new PrintTemplateSectionConditionConfig("email", PrintTemplateConditionOperators.Contains, " ")
            })
    }
};
var validReportPrintTemplate = new PrintTemplateConfig(
    1,
    PrintTemplateTypes.Report,
    new PrintTemplateHeaderConfig("Employee report", null, null, true),
    new[] { new PrintTemplateSectionConfig("table", PrintTemplateSectionKinds.Table, "Rows", new[] { ReportSystemFields.Status }, Array.Empty<string>()) },
    new PrintTemplateFooterConfig("Open Business Platform"),
    new PrintTemplateLayoutConfig(PrintTemplatePageSizes.A4, PrintTemplateOrientations.Landscape, PrintTemplateMargins.Wide, RepeatTableHeaders: false));
AssertTrue(
    PrintTemplateValidator.Validate(validRecordPrintTemplate, PrintTemplateTypes.Record, demoSchema).Valid,
    "A record print template should accept fields from the form schema.");
AssertSequenceEqual(
    new[] { "print_template.field.unknown" },
    PrintTemplateValidator.Validate(invalidRecordPrintTemplate, PrintTemplateTypes.Record, demoSchema).Errors.Select(error => error.Code).ToArray(),
    "Record print templates should reject fields that do not exist on the form schema.");
AssertSequenceEqual(
    new[]
    {
        "print_template.layout.page_size_invalid",
        "print_template.layout.orientation_invalid",
        "print_template.layout.margin_invalid"
    },
    PrintTemplateValidator.Validate(invalidLayoutPrintTemplate, PrintTemplateTypes.Record, demoSchema).Errors.Select(error => error.Code).ToArray(),
    "Print templates should reject unsupported page setup values.");
AssertTrue(
    PrintTemplateValidator.Validate(logoRecordPrintTemplate, PrintTemplateTypes.Record, demoSchema).Valid,
    "Print templates should accept safe uploaded logo data URLs.");
AssertSequenceEqual(
    new[] { "print_template.header.logo_url_invalid" },
    PrintTemplateValidator.Validate(invalidLogoPrintTemplate, PrintTemplateTypes.Record, demoSchema).Errors.Select(error => error.Code).ToArray(),
    "Print templates should reject unsafe logo URL schemes.");
AssertTrue(
    PrintTemplateValidator.Validate(paginatedRecordPrintTemplate, PrintTemplateTypes.Record, demoSchema).Valid,
    "Record print templates should accept section pagination controls.");
AssertTrue(
    PrintTemplateValidator.Validate(conditionalRecordPrintTemplate, PrintTemplateTypes.Record, demoSchema).Valid,
    "Record print templates should accept valid section conditions.");
AssertSequenceEqual(
    new[]
    {
        "print_template.condition.field_unknown",
        "print_template.condition.operator_invalid",
        "print_template.condition.value_required"
    },
    PrintTemplateValidator.Validate(invalidConditionPrintTemplate, PrintTemplateTypes.Record, demoSchema).Errors.Select(error => error.Code).ToArray(),
    "Print templates should reject unsupported or unknown section conditions.");
AssertTrue(
    PrintTemplateValidator.Validate(validReportPrintTemplate, PrintTemplateTypes.Report, demoSchema).Valid,
    "Report print templates should accept reportable system fields.");
var pdfBytes = PrintPdfDocumentBuilder.Build(new PrintPdfDocument(
    "Employee record",
    new[] { "Record abc123", "Version 1" },
    new[]
    {
        new PrintPdfSection("Main", new[] { "Email: jane@example.test", "Department: Finance" })
    },
    "Open Business Platform"));
var pdfText = System.Text.Encoding.ASCII.GetString(pdfBytes);
AssertTrue(pdfText.StartsWith("%PDF-1.4", StringComparison.Ordinal), "Server-side print PDFs should start with a PDF header.");
AssertTrue(pdfText.Contains("Employee record", StringComparison.Ordinal), "Server-side print PDFs should include the template title.");
AssertTrue(pdfText.Contains("jane@example.test", StringComparison.Ordinal), "Server-side print PDFs should include rendered record values.");
AssertTrue(pdfText.TrimEnd().EndsWith("%%EOF", StringComparison.Ordinal), "Server-side print PDFs should end with a PDF EOF marker.");
var pdfService = new PrintPdfService();
var recordPdfText = System.Text.Encoding.ASCII.GetString(pdfService.BuildRecordPdf(
    new PrintTemplateVersionDetailDto(
        Guid.Parse("12121212-1212-1212-1212-121212121212"),
        Guid.Parse("34343434-3434-3434-3434-343434343434"),
        Guid.Parse("56565656-5656-5656-5656-565656565656"),
        null,
        "Employee record",
        null,
        PrintTemplateTypes.Record,
        2,
        validRecordPrintTemplate,
        DateTimeOffset.UtcNow,
        null,
        DateTimeOffset.UtcNow,
        null),
    new FormRecordDetailDto(
        Guid.Parse("78787878-7878-7878-7878-787878787878"),
        Guid.Parse("56565656-5656-5656-5656-565656565656"),
        Guid.Parse("90909090-9090-9090-9090-909090909090"),
        "active",
        null,
        null,
        null,
        null,
        new Dictionary<string, object?> { ["email"] = "jane@example.test" },
        demoSchema,
        Array.Empty<string>(),
        "stamp",
        DateTimeOffset.UtcNow,
        null,
        null,
        null)));
AssertTrue(recordPdfText.Contains("Template version 2", StringComparison.Ordinal), "Record PDFs should include the published template version number.");
AssertTrue(recordPdfText.Contains("Email: jane@example.test", StringComparison.Ordinal), "Record PDFs should render selected record fields.");
var reportPdfText = System.Text.Encoding.ASCII.GetString(pdfService.BuildReportPdf(
    new PrintTemplateVersionDetailDto(
        Guid.Parse("abababab-abab-abab-abab-abababababab"),
        Guid.Parse("bcbcbcbc-bcbc-bcbc-bcbc-bcbcbcbcbcbc"),
        Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd"),
        Guid.Parse("dededede-dede-dede-dede-dededededede"),
        "Employee report",
        null,
        PrintTemplateTypes.Report,
        3,
        validReportPrintTemplate,
        DateTimeOffset.UtcNow,
        null,
        DateTimeOffset.UtcNow,
        null),
    new ListReportExecutionDto(
        Guid.Parse("dededede-dede-dede-dede-dededededede"),
        Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd"),
        "Employee report",
        "Employee information",
        1,
        100,
        1,
        new[] { new ListReportExecutionColumnDto(ReportSystemFields.Status, "Status", "system", "system", null) },
        new[]
        {
            new ListReportExecutionRowDto(
                Guid.Parse("efefefef-efef-efef-efef-efefefefefef"),
                "active",
                new Dictionary<string, ListReportExecutionCellDto>
                {
                    [ReportSystemFields.Status] = new("active", "Active")
                },
                DateTimeOffset.UtcNow)
        })));
AssertTrue(reportPdfText.Contains("Template version 3", StringComparison.Ordinal), "Report PDFs should include the published template version number.");
AssertTrue(reportPdfText.Contains("Status", StringComparison.Ordinal), "Report PDFs should render selected report columns.");
AssertTrue(reportPdfText.Contains("Active", StringComparison.Ordinal), "Report PDFs should render report row display values.");
var viewablePrintTemplates = await PrintTemplateAuthorization.FilterViewableTemplatesAsync(
    new[]
    {
        new PrintTemplateSummaryDto(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            null,
            "Record template",
            null,
            PrintTemplateTypes.Record,
            1,
            "stamp",
            DateTimeOffset.UtcNow,
            null,
            null,
            null),
        new PrintTemplateSummaryDto(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "Allowed report template",
            null,
            PrintTemplateTypes.Report,
            1,
            "stamp",
            DateTimeOffset.UtcNow,
            null,
            null,
            null),
        new PrintTemplateSummaryDto(
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            "Denied report template",
            null,
            PrintTemplateTypes.Report,
            1,
            "stamp",
            DateTimeOffset.UtcNow,
            null,
            null,
            null),
        new PrintTemplateSummaryDto(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            null,
            "Unscoped report template",
            null,
            PrintTemplateTypes.Report,
            1,
            "stamp",
            DateTimeOffset.UtcNow,
            null,
            null,
            null)
    },
    (reportId, _) => Task.FromResult(reportId == Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
    CancellationToken.None);
AssertSequenceEqual(
    new[] { "Record template", "Allowed report template" },
    viewablePrintTemplates.Select(template => template.Name).ToArray(),
    "Report print templates should only list templates for reports the user can view.");
AssertEqual(4, DemoDataSeeder.DemoUsers.Count, "Demo seed data should include admin, builder, user, and viewer accounts.");
AssertEqual(3, DemoDataSeeder.DemoDepartments.Count, "Demo seed data should include HR, Finance, and Operations departments.");
AssertEqual(10, DemoDataSeeder.DemoEmployeeRecords.Count, "Demo seed data should include ten employee records.");

var validTriggerConditions = new TriggerConditionGroupDefinition(
    TriggerConditionModes.All,
    new[] { new TriggerConditionDefinition(TriggerConditionTypes.FieldEquals, "department", "HR") });
var validTriggerActions = new[]
{
    new TriggerActionDefinition("action-1", TriggerActionTypes.WriteAuditEntry, "Trigger matched")
};
var validUpdateFieldActions = new[]
{
    new TriggerActionDefinition("field-1", TriggerActionTypes.UpdateField, FieldId: "email", Value: "jane@example.test")
};
var notificationUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var notificationGroupId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var validNotificationActions = new[]
{
    new TriggerActionDefinition(
        "notify-1",
        TriggerActionTypes.SendNotification,
        Title: "Record needs review",
        Body: "Open the record and review it.",
        RecipientUserIds: new[] { notificationUserId },
        RecipientGroupIds: new[] { notificationGroupId })
};
var targetFormId = Guid.Parse("33333333-3333-3333-3333-333333333333");
var targetFormVersionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
var createRecordTargetSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition("email", FormFieldTypes.Email, "Email", Required: true),
        new FormFieldDefinition("department", FormFieldTypes.Select, "Department", Required: true, Options: new[]
        {
            new FormFieldOptionDefinition("hr", "Human Resources", "HR"),
            new FormFieldOptionDefinition("finance", "Finance", "Finance")
        })
    },
    new FormLayoutDefinition(Array.Empty<FormLayoutPageDefinition>()));
var validCreateRecordActions = new[]
{
    new TriggerActionDefinition(
        "create-1",
        TriggerActionTypes.CreateRecord,
        TargetFormId: targetFormId,
        Values: new Dictionary<string, TriggerActionValueDefinition>
        {
            ["email"] = new(SourceFieldId: "email"),
            ["department"] = new(Literal: "HR")
        })
};
var validWebhookActions = new[]
{
    new TriggerActionDefinition(
        "webhook-1",
        TriggerActionTypes.CallWebhook,
        WebhookUrl: "https://hooks.example.test/records",
        WebhookMethod: "post",
        WebhookHeaders: new Dictionary<string, string> { ["X-Source"] = "open-business-platform" })
};
var sourceTriggerFormId = Guid.Parse("99999999-0000-0000-0000-000000000001");
var emailPrintTemplateId = Guid.Parse("77777777-7777-7777-7777-777777777777");
var validEmailAttachmentActions = new[]
{
    new TriggerActionDefinition(
        "email-attachment-1",
        TriggerActionTypes.SendEmail,
        To: new[] { "manager@example.test" },
        Subject: "Employee record",
        Body: "Attached.",
        PrintTemplateId: emailPrintTemplateId)
};
var validEmailAttachmentTargets = new[]
{
    new TriggerPrintTemplateTarget(
        emailPrintTemplateId,
        sourceTriggerFormId,
        PrintTemplateTypes.Record,
        CurrentVersionId: Guid.Parse("88888888-8888-8888-8888-888888888888"))
};
var workflowStartDefinitionId = Guid.Parse("99999999-0000-0000-0000-000000000002");
var workflowStartVersionId = Guid.Parse("99999999-0000-0000-0000-000000000003");
var validStartWorkflowActions = new[]
{
    new TriggerActionDefinition(
        "workflow-1",
        TriggerActionTypes.StartWorkflow,
        WorkflowDefinitionId: workflowStartDefinitionId)
};
var validWorkflowStartTargets = new[]
{
    new TriggerWorkflowStartTarget(
        workflowStartDefinitionId,
        sourceTriggerFormId,
        IsEnabled: true,
        Status: WorkflowDefinitionStatuses.Published,
        CurrentVersionId: workflowStartVersionId)
};
var validTriggerRetryPolicy = new TriggerRetryPolicyDefinition(true, 4, 120);
var validDailySchedule = new TriggerScheduleDefinition(TriggerScheduleKinds.Daily, "Etc/UTC", DateTimeOffset.Parse("2026-06-04T12:00:00Z"));
AssertTrue(TriggerEvents.Supported.Contains(TriggerEvents.RecordCreated), "Trigger events should include record.created.");
AssertTrue(TriggerEvents.Supported.Contains(TriggerEvents.ScheduleDaily), "Trigger events should include schedule.daily.");
AssertTrue(TriggerConditionTypes.Supported.Contains(TriggerConditionTypes.FieldChanged), "Trigger conditions should include field_changed.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.AssignRecord), "Trigger actions should include assign_record.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.UpdateField), "Trigger actions should include update_field.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.SendNotification), "Trigger actions should include send_notification.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.CreateRecord), "Trigger actions should include create_record.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.CallWebhook), "Trigger actions should include call_webhook.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.StartWorkflow), "Trigger actions should include start_workflow.");
AssertFalse(TriggerActionTypes.ScheduledSupported.Contains(TriggerActionTypes.StartWorkflow), "Scheduled triggers should not support record-context workflow starts.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("New hire audit", null, TriggerEvents.RecordCreated, validTriggerConditions, validTriggerActions, true, validTriggerRetryPolicy, null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "A valid trigger definition should pass validation.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Normalize email", null, TriggerEvents.RecordCreated, validTriggerConditions, validUpdateFieldActions, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "A valid update_field action should pass validation.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Notify reviewers", null, TriggerEvents.RecordCreated, validTriggerConditions, validNotificationActions, true),
        new[] { notificationUserId },
        new[] { notificationGroupId }).Valid,
    "A valid send_notification action should pass validation.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Create related record", null, TriggerEvents.RecordCreated, validTriggerConditions, validCreateRecordActions, true, validTriggerRetryPolicy, null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        new[] { new TriggerTargetFormSchema(targetFormId, targetFormVersionId, createRecordTargetSchema) }).Valid,
    "A valid create_record action should pass validation.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Send webhook", null, TriggerEvents.RecordCreated, validTriggerConditions, validWebhookActions, true, validTriggerRetryPolicy, null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "A valid call_webhook action should pass validation.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Email record PDF", null, TriggerEvents.RecordCreated, validTriggerConditions, validEmailAttachmentActions, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        Array.Empty<TriggerWorkflowStartTarget>(),
        validEmailAttachmentTargets,
        sourceTriggerFormId).Valid,
    "A valid send_email action should accept a published same-form record print template attachment.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Scheduled PDF email", null, TriggerEvents.ScheduleDaily, null, validEmailAttachmentActions, true, validTriggerRetryPolicy, validDailySchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        Array.Empty<TriggerWorkflowStartTarget>(),
        validEmailAttachmentTargets,
        sourceTriggerFormId).Valid,
    "Scheduled email actions should reject record PDF attachments because no record context exists.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Start approval workflow", null, TriggerEvents.RecordCreated, validTriggerConditions, validStartWorkflowActions, true, validTriggerRetryPolicy, null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        validWorkflowStartTargets,
        sourceTriggerFormId).Valid,
    "A valid start_workflow action should pass validation.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Missing workflow target", null, TriggerEvents.RecordCreated, validTriggerConditions, new[] { new TriggerActionDefinition("workflow-1", TriggerActionTypes.StartWorkflow) }, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        validWorkflowStartTargets,
        sourceTriggerFormId).Valid,
    "Validation should reject start_workflow actions without a workflow target.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Disabled workflow target", null, TriggerEvents.RecordCreated, validTriggerConditions, validStartWorkflowActions, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        new[]
        {
            new TriggerWorkflowStartTarget(
                workflowStartDefinitionId,
                sourceTriggerFormId,
                IsEnabled: false,
                Status: WorkflowDefinitionStatuses.Published,
                CurrentVersionId: workflowStartVersionId)
        },
        sourceTriggerFormId).Valid,
    "Validation should reject disabled workflow targets.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Draft workflow target", null, TriggerEvents.RecordCreated, validTriggerConditions, validStartWorkflowActions, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        new[]
        {
            new TriggerWorkflowStartTarget(
                workflowStartDefinitionId,
                sourceTriggerFormId,
                IsEnabled: true,
                Status: WorkflowDefinitionStatuses.Draft,
                CurrentVersionId: null)
        },
        sourceTriggerFormId).Valid,
    "Validation should reject draft workflow targets.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Wrong form workflow target", null, TriggerEvents.RecordCreated, validTriggerConditions, validStartWorkflowActions, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        new[]
        {
            new TriggerWorkflowStartTarget(
                workflowStartDefinitionId,
                Guid.Parse("99999999-0000-0000-0000-000000000004"),
                IsEnabled: true,
                Status: WorkflowDefinitionStatuses.Published,
                CurrentVersionId: workflowStartVersionId)
        },
        sourceTriggerFormId).Valid,
    "Validation should reject workflow targets from a different source form.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Daily digest", null, TriggerEvents.ScheduleDaily, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validWebhookActions, true, validTriggerRetryPolicy, validDailySchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "A scheduled trigger with supported schedule actions should pass validation.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Missing schedule", null, TriggerEvents.ScheduleDaily, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validWebhookActions, true, validTriggerRetryPolicy, null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Scheduled trigger events should require schedule metadata.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Bad webhook", null, TriggerEvents.RecordCreated, validTriggerConditions, new[] { new TriggerActionDefinition("webhook-1", TriggerActionTypes.CallWebhook, WebhookUrl: "ftp://example.test/hook") }, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject webhook actions without an absolute http or https URL.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Bad retry policy", null, TriggerEvents.RecordCreated, validTriggerConditions, validWebhookActions, true, new TriggerRetryPolicyDefinition(true, 25, 10), null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject retry policies outside supported bounds.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Unknown target form",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[]
            {
                new TriggerActionDefinition(
                    "create-1",
                    TriggerActionTypes.CreateRecord,
                    TargetFormId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Values: new Dictionary<string, TriggerActionValueDefinition>
                    {
                        ["email"] = new(SourceFieldId: "email"),
                        ["department"] = new(Literal: "HR")
                    })
            },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        new[] { new TriggerTargetFormSchema(targetFormId, targetFormVersionId, createRecordTargetSchema) }).Valid,
    "Validation should reject create_record actions that reference unpublished or missing target forms.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Invalid target values",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[]
            {
                new TriggerActionDefinition(
                    "create-1",
                    TriggerActionTypes.CreateRecord,
                    TargetFormId: targetFormId,
                    Values: new Dictionary<string, TriggerActionValueDefinition>
                    {
                        ["email"] = new(SourceFieldId: "email"),
                        ["unknown"] = new(Literal: "nope")
                    })
            },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        new[] { new TriggerTargetFormSchema(targetFormId, targetFormVersionId, createRecordTargetSchema) }).Valid,
    "Validation should reject create_record actions with invalid target field maps.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("", null, "record.deleted", validTriggerConditions, validTriggerActions, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject missing names and unsupported events.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Bad field",
            null,
            TriggerEvents.RecordCreated,
            new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.FieldEquals, "missing", "HR") }),
            validTriggerActions,
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject conditions that reference missing fields.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing equality value",
            null,
            TriggerEvents.RecordCreated,
            new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.FieldEquals, "department") }),
            validTriggerActions,
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject field equality conditions without a comparison value.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Duplicate action", null, TriggerEvents.RecordCreated, validTriggerConditions, new[] { validTriggerActions[0], validTriggerActions[0] }, true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject duplicate action ids.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing update field",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("field-1", TriggerActionTypes.UpdateField, Value: "jane@example.test") },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject update_field actions without a field id.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Unknown update field",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("field-1", TriggerActionTypes.UpdateField, FieldId: "missing", Value: "jane@example.test") },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject update_field actions that reference missing fields.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing update value",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("field-1", TriggerActionTypes.UpdateField, FieldId: "email") },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject update_field actions without a value.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing notification title",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("notify-1", TriggerActionTypes.SendNotification, Body: "Review it.", RecipientUserIds: new[] { notificationUserId }) },
            true),
        new[] { notificationUserId },
        Array.Empty<Guid>()).Valid,
    "Validation should reject notification actions without a title.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing notification recipient",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("notify-1", TriggerActionTypes.SendNotification, Title: "Review", Body: "Review it.") },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject notification actions without recipients.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing notification user",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("notify-1", TriggerActionTypes.SendNotification, Title: "Review", Body: "Review it.", RecipientUserIds: new[] { notificationUserId }) },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject notification actions that reference inactive or missing users.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest(
            "Missing notification group",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[] { new TriggerActionDefinition("notify-1", TriggerActionTypes.SendNotification, Title: "Review", Body: "Review it.", RecipientGroupIds: new[] { notificationGroupId }) },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject notification actions that reference inactive or missing groups.");

var workflowUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
var workflowGroupId = Guid.Parse("77777777-7777-7777-7777-777777777777");
var workflowDepartmentId = Guid.Parse("88888888-8888-8888-8888-888888888888");
var validWorkflowConfig = new WorkflowDefinitionConfig(
    1,
    "draft",
    new[]
    {
        new WorkflowStateDefinition("draft", "Draft"),
        new WorkflowStateDefinition("manager_review", "Manager Review"),
        new WorkflowStateDefinition("approved", "Approved", IsFinal: true)
    },
    new[]
    {
        new WorkflowTransitionDefinition("submit", "Submit", "draft", "manager_review", "manager_approval"),
        new WorkflowTransitionDefinition("approve", "Approve", "manager_review", "approved")
    },
    new[]
    {
        new WorkflowApprovalStepDefinition(
            "manager_approval",
            "Manager approval",
            WorkflowApprovalModes.Any,
            new[]
            {
                new WorkflowAssigneeRuleDefinition(WorkflowAssigneeRuleTypes.DepartmentManager, DepartmentId: workflowDepartmentId),
                new WorkflowAssigneeRuleDefinition(WorkflowAssigneeRuleTypes.Group, GroupId: workflowGroupId)
            })
    });
var workflowActionConfig = validWorkflowConfig with
{
    Transitions = new[]
    {
        new WorkflowTransitionDefinition(
            "approve",
            "Approve",
            "manager_review",
            "approved",
            Actions: new[]
            {
                new WorkflowActionDefinition("audit-1", WorkflowActionTypes.WriteAuditEntry, Message: "Workflow transition completed."),
                new WorkflowActionDefinition("email-1", WorkflowActionTypes.SendEmail, To: new[] { "ops@example.test" }, Subject: "Approved", Body: "The record was approved."),
                new WorkflowActionDefinition("assign-1", WorkflowActionTypes.AssignRecord, AssignedToUserId: workflowUserId),
                new WorkflowActionDefinition("field-1", WorkflowActionTypes.UpdateField, FieldId: "email", Value: "approved@example.test"),
                new WorkflowActionDefinition("notify-1", WorkflowActionTypes.SendNotification, Title: "Approved", Body: "Record approved.", RecipientUserIds: new[] { workflowUserId }),
                new WorkflowActionDefinition(
                    "create-1",
                    WorkflowActionTypes.CreateRecord,
                    TargetFormId: targetFormId,
                    Values: new Dictionary<string, WorkflowActionValueDefinition>
                    {
                        ["email"] = new WorkflowActionValueDefinition(SourceFieldId: "email"),
                        ["department"] = new WorkflowActionValueDefinition(Literal: "HR")
                    })
            })
    }
};
AssertEqual(WorkflowDefinitionStatuses.Draft, WorkflowDefinitionStatuses.Draft, "Workflow definition status contracts should expose draft.");
AssertEqual(WorkflowDefinitionStatuses.Published, WorkflowDefinitionStatuses.Published, "Workflow definition status contracts should expose published.");
AssertTrue(WorkflowApprovalModes.Supported.Contains(WorkflowApprovalModes.Any), "Workflow approval modes should include any.");
AssertTrue(WorkflowAssigneeRuleTypes.Supported.Contains(WorkflowAssigneeRuleTypes.DepartmentManager), "Workflow assignee rules should include department managers.");
AssertTrue(WorkflowActionTypes.Supported.Contains(WorkflowActionTypes.SendNotification), "Workflow action contracts should include notification actions.");
AssertFalse(WorkflowActionTypes.Supported.Contains(WorkflowActionTypes.ChangeStatus), "Workflow action contracts should reject change_status so records.status stays aligned with workflow state.");
AssertTrue(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest("Employee approval", null, validWorkflowConfig, true),
        new[] { workflowUserId },
        new[] { workflowGroupId },
        new[] { workflowDepartmentId }).Valid,
    "A valid workflow definition should pass validation.");
AssertTrue(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest("Action workflow", null, workflowActionConfig, true),
        new[] { workflowUserId },
        new[] { workflowGroupId },
        new[] { workflowDepartmentId }).Valid,
    "Workflow validation should accept the safe V5 action execution subset.");
AssertFalse(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest(
            "Duplicate states",
            null,
            validWorkflowConfig with
            {
                States = new[]
                {
                    new WorkflowStateDefinition("draft", "Draft"),
                    new WorkflowStateDefinition("draft", "Draft again", IsFinal: true)
                }
            },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Workflow validation should reject duplicate state keys.");
AssertFalse(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest("Missing initial", null, validWorkflowConfig with { InitialStateKey = "missing" }, true),
        new[] { workflowUserId },
        new[] { workflowGroupId },
        new[] { workflowDepartmentId }).Valid,
    "Workflow validation should reject missing initial states.");
AssertFalse(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest(
            "Bad transition",
            null,
            validWorkflowConfig with
            {
                Transitions = new[] { new WorkflowTransitionDefinition("submit", "Submit", "draft", "missing") }
            },
            true),
        new[] { workflowUserId },
        new[] { workflowGroupId },
        new[] { workflowDepartmentId }).Valid,
    "Workflow validation should reject transition endpoints that do not exist.");
AssertFalse(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest(
            "Bad approval",
            null,
            validWorkflowConfig with
            {
                ApprovalSteps = new[]
                {
                    new WorkflowApprovalStepDefinition(
                        "manager_approval",
                        "Manager approval",
                        WorkflowApprovalModes.Any,
                        new[] { new WorkflowAssigneeRuleDefinition(WorkflowAssigneeRuleTypes.User, UserId: Guid.Parse("99999999-9999-9999-9999-999999999999")) })
                }
            },
            true),
        new[] { workflowUserId },
        new[] { workflowGroupId },
        new[] { workflowDepartmentId }).Valid,
    "Workflow validation should reject approval assignee rules that reference inactive or missing users.");
AssertFalse(
    WorkflowDefinitionValidator.Validate(
        new CreateWorkflowDefinitionRequest(
            "Unsafe action",
            null,
            validWorkflowConfig with
            {
                Transitions = new[]
                {
                    new WorkflowTransitionDefinition(
                        "submit",
                        "Submit",
                        "draft",
                        "manager_review",
                        Actions: new[] { new WorkflowActionDefinition("status-1", WorkflowActionTypes.ChangeStatus, Status: "archived") })
                }
            },
            true),
        new[] { workflowUserId },
        new[] { workflowGroupId },
        new[] { workflowDepartmentId }).Valid,
    "Workflow validation should reject change_status transition actions.");
AssertEqual(RecordWorkflowHistoryActions.Started, "workflow_started", "Record workflow history should expose a stable start action.");
AssertEqual(RecordWorkflowHistoryActions.Transitioned, "workflow_transitioned", "Record workflow history should expose a stable transition action.");
AssertEqual(RecordWorkflowHistoryActions.ActionSucceeded, "workflow_action_succeeded", "Workflow history should expose successful action attempts.");
AssertEqual(RecordWorkflowHistoryActions.ActionFailed, "workflow_action_failed", "Workflow history should expose failed action attempts.");
AssertEqual(TriggerWorkflowStartResultStatuses.Started, "started", "Workflow-start trigger results should expose a stable started status.");
AssertEqual(TriggerWorkflowStartResultStatuses.Skipped, "skipped", "Workflow-start trigger results should expose a stable skipped status.");
AssertEqual(TriggerWorkflowStartResultStatuses.Failed, "failed", "Workflow-start trigger results should expose a stable failed status.");
AssertEqual(TriggerWorkflowStartSkipReasons.RecordAlreadyHasActiveWorkflow, "record_already_has_active_workflow", "Workflow-start trigger skips should identify active workflow duplicates.");
var startWorkflowRequest = new StartRecordWorkflowRequest(Guid.Parse("99999999-9999-9999-9999-999999999999"), "record-stamp");
AssertEqual("record-stamp", startWorkflowRequest.ConcurrencyStamp, "Record workflow starts should require record concurrency stamps.");
var executeWorkflowTransitionRequest = new ExecuteRecordWorkflowTransitionRequest("record-stamp-2");
AssertEqual("record-stamp-2", executeWorkflowTransitionRequest.ConcurrencyStamp, "Record workflow transitions should require record concurrency stamps.");
var workflowTriggerAction = WorkflowActionExecutionService.ToTriggerActionDefinition(workflowActionConfig.Transitions.Single().Actions!.First());
AssertEqual(TriggerActionTypes.WriteAuditEntry, workflowTriggerAction.Type, "Workflow actions should convert to the shared trigger action primitive shape.");
var directTransitionOptions = RecordWorkflowService.GetAvailableDirectTransitions(validWorkflowConfig, "manager_review");
AssertEqual(1, directTransitionOptions.Count, "Only direct transitions from the current state should be available.");
AssertEqual("approve", directTransitionOptions.Single().Key, "Available direct transitions should expose transition keys.");
var approvalTransitionOptions = RecordWorkflowService.GetAvailableDirectTransitions(validWorkflowConfig, "draft");
AssertEqual(0, approvalTransitionOptions.Count, "Approval-gated transitions should wait for the approval inbox slice.");
var recordWorkflowState = new RecordWorkflowStateDto(
    Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
    Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
    Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
    Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"),
    "Employee approval",
    1,
    "manager_review",
    Array.Empty<RecordWorkflowStartOptionDto>(),
    directTransitionOptions,
    new[]
    {
        new RecordWorkflowHistoryDto(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"),
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"),
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            "draft",
            "manager_review",
            "submit",
            RecordWorkflowHistoryActions.Transitioned,
            workflowUserId,
            DateTimeOffset.Parse("2026-06-04T12:00:00Z"))
    },
    "record-stamp-3");
AssertEqual("manager_review", recordWorkflowState.StateKey, "Record workflow state responses should expose the current state key.");
AssertEqual("approve", recordWorkflowState.AvailableTransitions.Single().Key, "Record workflow state responses should include available direct transitions.");
AssertEqual(RecordWorkflowHistoryActions.Transitioned, recordWorkflowState.History.Single().Action, "Record workflow state responses should include recent history.");
AssertEqual(WorkflowApprovalTaskStatuses.Pending, "pending", "Workflow approvals should expose a pending status.");
AssertEqual(WorkflowApprovalTaskStatuses.Approved, "approved", "Workflow approvals should expose an approved status.");
AssertEqual(WorkflowApprovalTaskStatuses.Rejected, "rejected", "Workflow approvals should expose a rejected status.");
AssertEqual(WorkflowApprovalTaskStatuses.Canceled, "canceled", "Workflow approvals should expose a canceled status.");
AssertTrue(
    WorkflowApprovalService.IsApprovalComplete(WorkflowApprovalModes.Any, new[] { WorkflowApprovalTaskStatuses.Approved, WorkflowApprovalTaskStatuses.Pending }),
    "Any-mode approvals should complete after one approval.");
AssertFalse(
    WorkflowApprovalService.IsApprovalComplete(WorkflowApprovalModes.All, new[] { WorkflowApprovalTaskStatuses.Approved, WorkflowApprovalTaskStatuses.Pending }),
    "All-mode approvals should wait for every approver.");
AssertTrue(
    WorkflowApprovalService.IsApprovalComplete(WorkflowApprovalModes.All, new[] { WorkflowApprovalTaskStatuses.Approved, WorkflowApprovalTaskStatuses.Approved }),
    "All-mode approvals should complete after every approver approves.");
var approvalTaskDto = new WorkflowApprovalTaskDto(
    Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
    Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"),
    Guid.Parse("bbbbbbbb-0000-0000-0000-000000000003"),
    Guid.Parse("bbbbbbbb-0000-0000-0000-000000000004"),
    Guid.Parse("bbbbbbbb-0000-0000-0000-000000000005"),
    Guid.Parse("bbbbbbbb-0000-0000-0000-000000000006"),
    "manager_approval",
    "Manager approval",
    WorkflowApprovalModes.Any,
    "submit",
    "Submit",
    "draft",
    "manager_review",
    WorkflowApprovalTaskStatuses.Pending,
    workflowUserId,
    null,
    null,
    null,
    null,
    DateTimeOffset.Parse("2026-06-04T12:00:00Z"));
AssertEqual("manager_approval", approvalTaskDto.ApprovalStepKey, "Approval task DTOs should expose approval step keys.");
AssertEqual("submit", approvalTaskDto.TransitionKey, "Approval task DTOs should expose transition keys.");
var approvalResponseRequest = new RespondWorkflowApprovalRequest("Looks good.");
AssertEqual("Looks good.", approvalResponseRequest.Comment, "Approval responses should carry optional comments.");

AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Menu.UsersAccess), "Built-in permissions should include Users & Access menu visibility.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Users.Manage), "Built-in permissions should include user management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Reports.Manage), "Built-in permissions should include report management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Workflows.Manage), "Built-in permissions should include workflow management.");
AssertTrue(PlatformPermissions.FormActions.Contains(PlatformPermissions.Form.View), "Form actions should include view.");
AssertTrue(PlatformPermissions.FormActions.Contains(PlatformPermissions.Form.Export), "Form actions should include export.");
AssertTrue(PlatformPermissions.FormActions.Contains(PlatformPermissions.Form.Assign), "Form actions should include assign.");
AssertTrue(PlatformPermissions.WorkflowActions.Contains(PlatformPermissions.Workflow.Approve), "Workflow actions should include approve.");
AssertTrue(PlatformPermissions.RecordScopes.Supported.Contains(PlatformPermissions.RecordScopes.ManagedDepartment), "Record scopes should include managed department.");
AssertTrue(PlatformPermissions.ReportActions.Contains(PlatformPermissions.Report.Export), "Report actions should include export.");
AssertTrue(PlatformPermissions.FieldAccess.Supported.Contains(PlatformPermissions.FieldAccess.Hidden), "Field access should include hidden.");

var accessUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
var accessDepartmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
var accessGroupId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
var directRecord = new FormRecord { Id = Guid.NewGuid(), OwnerId = accessUserId, CreatedById = Guid.NewGuid(), ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>()) };
var departmentRecord = new FormRecord { Id = Guid.NewGuid(), DepartmentId = accessDepartmentId, ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>()) };
var groupRecord = new FormRecord { Id = Guid.NewGuid(), AssignedGroupId = accessGroupId, ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>()) };
var deniedRecord = new FormRecord { Id = Guid.NewGuid(), ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>()) };
var accessContext = new RecordAccessContext(accessUserId, new[] { accessDepartmentId }, Array.Empty<Guid>(), new[] { accessGroupId });
var filteredRecords = RecordAccessEvaluator
    .Apply(
        new[] { directRecord, departmentRecord, groupRecord, deniedRecord }.AsQueryable(),
        accessContext,
        new[] { PlatformPermissions.RecordScopes.Own, PlatformPermissions.RecordScopes.Department, PlatformPermissions.RecordScopes.Group })
    .Select(record => record.Id)
    .ToArray();
AssertSequenceEqual(new[] { directRecord.Id, departmentRecord.Id, groupRecord.Id }, filteredRecords, "Record access scopes should combine with OR semantics.");

var triggerBeforeSnapshot = new TriggerRecordSnapshot(
    Guid.NewGuid(),
    Guid.NewGuid(),
    "draft",
    accessUserId,
    null,
    null,
    null,
    new Dictionary<string, object?> { ["department"] = "Finance", ["email"] = "old@example.com" });
var triggerAfterSnapshot = triggerBeforeSnapshot with
{
    Status = "submitted",
    DepartmentId = accessDepartmentId,
    AssignedGroupId = accessGroupId,
    Values = new Dictionary<string, object?> { ["department"] = "HR", ["email"] = "new@example.com" }
};
var triggerEventContext = new TriggerEventContext(
    TriggerEvents.FieldChanged,
    triggerAfterSnapshot.FormId,
    triggerAfterSnapshot.RecordId,
    accessUserId,
    triggerBeforeSnapshot,
    triggerAfterSnapshot,
    new[] { "department", "email" },
    "draft",
    "submitted",
    null,
    null,
    null,
    accessGroupId,
    DateTimeOffset.UtcNow);
AssertTrue(
    TriggerConditionEvaluator.Matches(
        new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.FieldEquals, "department", "HR") }),
        triggerEventContext),
    "field_equals should match after values.");
AssertTrue(
    TriggerConditionEvaluator.Matches(
        new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.FieldChanged, "email") }),
        triggerEventContext),
    "field_changed should match changed field ids.");
AssertTrue(
    TriggerConditionEvaluator.Matches(
        new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.StatusChangedTo, Status: "submitted") }),
        triggerEventContext),
    "status_changed_to should match current status.");
AssertTrue(
    TriggerConditionEvaluator.Matches(
        new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.DepartmentEquals, DepartmentId: accessDepartmentId) }),
        triggerEventContext),
    "department_equals should match after department.");
AssertTrue(
    TriggerConditionEvaluator.Matches(
        new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.AssignedToGroup, GroupId: accessGroupId) }),
        triggerEventContext),
    "assigned_to_group should match current group assignment.");
AssertFalse(
    TriggerConditionEvaluator.Matches(
        new TriggerConditionGroupDefinition(TriggerConditionModes.All, new[] { new TriggerConditionDefinition(TriggerConditionTypes.FieldEquals, "department", "Operations") }),
        triggerEventContext),
    "all-mode groups should fail when a condition fails.");
var resolvedCreateRecordValues = TriggerActionRegistry.ResolveCreateRecordValues(validCreateRecordActions[0], triggerEventContext);
AssertEqual("new@example.com", resolvedCreateRecordValues["email"], "Create record actions should resolve source field values from the triggering record snapshot.");
AssertEqual("HR", resolvedCreateRecordValues["department"], "Create record actions should preserve literal target values.");

var bootstrapPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.NameIdentifier, BootstrapAdminUserDirectory.BootstrapAdminId),
    new Claim(ClaimTypes.Role, PlatformRoles.Admin)
}, "Test"));
var permissionService = new PermissionService(dbContext);
AssertTrue(await permissionService.CanAsync(bootstrapPrincipal, PlatformPermissions.Users.Manage, CancellationToken.None), "Bootstrap admin should have user management permission.");
AssertTrue(await permissionService.CanAccessFormAsync(bootstrapPrincipal, Guid.NewGuid(), PlatformPermissions.Form.Manage, CancellationToken.None), "Bootstrap admin should have form management permission.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.GetAllowedRecordScopesAsync)), "PermissionService should expose record scope resolution.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.ApplyRecordAccessAsync)), "PermissionService should expose record query filtering.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.CanAccessRecordAsync)), "PermissionService should expose record access checks.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.GetFieldAccessAsync)), "PermissionService should expose field access checks.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.CanAccessReportAsync)), "PermissionService should expose report access checks.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.ListGroupsAsync)), "Identity management should list groups.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.CreateGroupAsync)), "Identity management should create groups.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.UpdateGroupAsync)), "Identity management should update groups.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.ListDepartmentsAsync)), "Identity management should list departments.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.CreateDepartmentAsync)), "Identity management should create departments.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.UpdateDepartmentAsync)), "Identity management should update departments.");
AssertNotNull(typeof(FormRecordDetailDto).GetProperty(nameof(FormRecordDetailDto.ReadOnlyFieldIds)), "Record detail should include read-only field IDs.");
AssertNotNull(typeof(AssignRecordRequest), "Records should expose an assignment request contract.");
AssertNotNull(typeof(ChangeRecordStatusRequest), "Records should expose a status change request contract.");
AssertNotNull(typeof(TriggerDefinitionService).GetMethod(nameof(TriggerDefinitionService.ListTriggersAsync)), "Trigger service should list triggers.");
AssertNotNull(typeof(TriggerDefinitionService).GetMethod(nameof(TriggerDefinitionService.CreateTriggerAsync)), "Trigger service should create triggers.");
AssertNotNull(typeof(TriggerDefinitionService).GetMethod(nameof(TriggerDefinitionService.UpdateTriggerAsync)), "Trigger service should update triggers.");
AssertNotNull(typeof(TriggerDefinitionService).GetMethod(nameof(TriggerDefinitionService.ListTriggerLogsAsync)), "Trigger service should list trigger logs.");
AssertTypeAssignable<IPlatformApiModule, TriggersModule>();
AssertTrue(new TriggersModule().Id == "app.triggers", "Trigger module should expose a stable module id.");
AssertNotNull(typeof(TriggerActionRegistry).GetMethod(nameof(TriggerActionRegistry.ExecuteAsync)), "Trigger action registry should execute approved actions.");
AssertNotNull(
    typeof(TriggerActionRegistry).GetMethod("ExecuteStartWorkflowAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
    "Trigger action registry should execute workflow-start trigger actions.");
AssertNotNull(
    typeof(TriggerExecutionService).GetMethod("BuildFailedActionResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
    "Trigger execution service should include failed action metadata in trigger log results.");
var notificationPreferenceFilter = typeof(TriggerActionRegistry).GetMethod(
    "ExcludeDisabledNotificationRecipients",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(notificationPreferenceFilter, "Trigger action registry should filter notification recipients by in-app preferences.");
var enabledNotificationRecipientIds = (IReadOnlyList<Guid>)notificationPreferenceFilter!.Invoke(
    null,
    new object[]
    {
        new[] { accessUserId, accessDepartmentId, accessGroupId },
        new[] { accessDepartmentId }
    })!;
AssertSequenceEqual(new[] { accessUserId, accessGroupId }, enabledNotificationRecipientIds, "Notification recipient filtering should remove users who disabled in-app notifications.");
var notificationPreferenceSkip = typeof(TriggerActionRegistry).GetMethod(
    "ShouldSkipNotificationInsertion",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(notificationPreferenceSkip, "Trigger action registry should treat all-disabled notification recipients as a successful no-op.");
AssertTrue(
    (bool)notificationPreferenceSkip!.Invoke(null, new object[] { 2, 0 })!,
    "Notification action should no-op when active recipients exist but all disabled in-app notifications.");
AssertFalse(
    (bool)notificationPreferenceSkip.Invoke(null, new object[] { 0, 0 })!,
    "Notification action should not hide missing active recipients as a preference skip.");
AssertNotNull(typeof(TriggerExecutionService).GetMethod(nameof(TriggerExecutionService.ExecuteAsync)), "Trigger execution service should execute matching triggers.");
AssertNotNull(typeof(TriggerEventDispatcher).GetMethod(nameof(TriggerEventDispatcher.DispatchAsync)), "Trigger dispatcher should dispatch event contexts.");
AssertNotNull(typeof(WorkflowDefinitionService).GetMethod(nameof(WorkflowDefinitionService.ListWorkflowsAsync)), "Workflow service should list form-scoped workflows.");
AssertNotNull(typeof(WorkflowDefinitionService).GetMethod(nameof(WorkflowDefinitionService.CreateWorkflowAsync)), "Workflow service should create workflows.");
AssertNotNull(typeof(WorkflowDefinitionService).GetMethod(nameof(WorkflowDefinitionService.UpdateWorkflowAsync)), "Workflow service should update workflows.");
AssertNotNull(typeof(WorkflowDefinitionService).GetMethod(nameof(WorkflowDefinitionService.PublishWorkflowAsync)), "Workflow service should publish immutable workflow versions.");
AssertNotNull(typeof(WorkflowDefinitionService).GetMethod(nameof(WorkflowDefinitionService.EnableWorkflowAsync)), "Workflow service should enable workflows without deleting history.");
AssertNotNull(typeof(WorkflowDefinitionService).GetMethod(nameof(WorkflowDefinitionService.DisableWorkflowAsync)), "Workflow service should disable workflows without deleting history.");
AssertNotNull(typeof(RecordWorkflowService).GetMethod(nameof(RecordWorkflowService.GetRecordWorkflowAsync)), "Record workflow service should read record workflow state.");
AssertNotNull(typeof(RecordWorkflowService).GetMethod(nameof(RecordWorkflowService.StartRecordWorkflowAsync)), "Record workflow service should start enabled published workflows on records.");
AssertNotNull(typeof(RecordWorkflowService).GetMethod(nameof(RecordWorkflowService.ExecuteTransitionAsync)), "Record workflow service should execute direct workflow transitions.");
AssertNotNull(typeof(WorkflowActionExecutionService).GetMethod(nameof(WorkflowActionExecutionService.ExecuteTransitionActionsAsync)), "Workflow action service should execute transition actions.");
AssertNotNull(typeof(WorkflowActionExecutionService).GetMethod(nameof(WorkflowActionExecutionService.PersistRolledBackActionFailureAsync)), "Workflow action service should persist rolled-back action failures.");
AssertNotNull(typeof(WorkflowApprovalService).GetMethod(nameof(WorkflowApprovalService.ListForCurrentUserAsync)), "Workflow approval service should list current-user approval tasks.");
AssertNotNull(typeof(WorkflowApprovalService).GetMethod(nameof(WorkflowApprovalService.ApproveAsync)), "Workflow approval service should approve assigned tasks.");
AssertNotNull(typeof(WorkflowApprovalService).GetMethod(nameof(WorkflowApprovalService.RejectAsync)), "Workflow approval service should reject assigned tasks.");
AssertTypeAssignable<IPlatformApiModule, WorkflowsModule>();
AssertTrue(new WorkflowsModule().Id == "app.workflows", "Workflow module should expose a stable module id.");
AssertNotNull(typeof(RecordSubmissionService).GetConstructors().Single().GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(TriggerEventDispatcher)), "Record submission should receive the trigger dispatcher.");
AssertNotNull(typeof(RecordMutationService).GetConstructors().Single().GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(TriggerEventDispatcher)), "Record mutation should receive the trigger dispatcher.");
AssertNotNull(typeof(NotificationQueryService).GetMethod(nameof(NotificationQueryService.ListForUserAsync)), "Notification service should list current-user notifications.");
AssertNotNull(typeof(NotificationQueryService).GetMethod(nameof(NotificationQueryService.GetUnreadCountAsync)), "Notification service should count unread notifications.");
AssertNotNull(typeof(NotificationQueryService).GetMethod(nameof(NotificationQueryService.MarkReadAsync)), "Notification service should mark one notification read.");
AssertNotNull(typeof(NotificationQueryService).GetMethod(nameof(NotificationQueryService.MarkAllReadAsync)), "Notification service should mark all current-user notifications read.");
AssertNotNull(typeof(NotificationQueryService).GetMethod(nameof(NotificationQueryService.GetPreferencesAsync)), "Notification service should read current-user preferences.");
AssertNotNull(typeof(NotificationQueryService).GetMethod(nameof(NotificationQueryService.UpdatePreferencesAsync)), "Notification service should update current-user preferences.");
AssertTypeAssignable<IPlatformApiModule, NotificationsModule>();
AssertTrue(new NotificationsModule().Id == "app.notifications", "Notifications module should expose a stable module id.");
AssertNotNull(typeof(NotificationDto).GetProperty(nameof(NotificationDto.ReadAt)), "Notification DTO should expose read state.");
AssertNotNull(typeof(NotificationUnreadCountDto).GetProperty(nameof(NotificationUnreadCountDto.UnreadCount)), "Notification unread count DTO should expose unread count.");
AssertNotNull(typeof(NotificationPreferencesDto).GetProperty(nameof(NotificationPreferencesDto.InAppEnabled)), "Notification preferences DTO should expose in-app choice.");
AssertNotNull(typeof(NotificationPreferencesDto).GetProperty(nameof(NotificationPreferencesDto.ShowUnreadBadge)), "Notification preferences DTO should expose unread badge choice.");
AssertNotNull(typeof(UpdateNotificationPreferencesRequest).GetProperty(nameof(UpdateNotificationPreferencesRequest.InAppEnabled)), "Notification preference updates should accept in-app choice.");
AssertNotNull(typeof(UpdateNotificationPreferencesRequest).GetProperty(nameof(UpdateNotificationPreferencesRequest.ShowUnreadBadge)), "Notification preference updates should accept unread badge choice.");
AssertEqual("success", TriggerExecutionStatuses.Success, "Trigger success logs should use success status.");
AssertEqual("failed", TriggerExecutionStatuses.Failed, "Trigger failure logs should use failed status.");
var retrySourceLogId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
var retryMetadata = new TriggerRetryMetadata(retrySourceLogId);
AssertEqual(retrySourceLogId, retryMetadata.SourceLogId, "Trigger retry metadata should link a retry attempt to the failed source log.");
var retryLogDto = new TriggerExecutionLogDto(
    Guid.NewGuid(),
    Guid.NewGuid(),
    Guid.NewGuid(),
    TriggerEvents.RecordCreated,
    "Record",
    Guid.NewGuid(),
    TriggerExecutionStatuses.Success,
    null,
    null,
    null,
    DateTimeOffset.UtcNow,
    DateTimeOffset.UtcNow,
    DateTimeOffset.UtcNow,
    retrySourceLogId);
AssertEqual(retrySourceLogId, retryLogDto.RetryOfLogId, "Trigger log DTOs should expose retry source metadata.");
AssertNotNull(typeof(TriggerExecutionService).GetMethod(nameof(TriggerExecutionService.RetryFailedLogAsync)), "Trigger execution service should expose manual failed-log retry.");
AssertNotNull(typeof(ReportManagementService).GetMethod(nameof(ReportManagementService.ExecuteListReportAsync))?.GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(ClaimsPrincipal)), "Report execution should receive the current principal.");
AssertNotNull(typeof(ChartAggregationService).GetMethod(nameof(ChartAggregationService.PreviewAsync))?.GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(ClaimsPrincipal)), "Chart previews should receive the current principal.");
var reportRecordAccessAction = typeof(ReportManagementService).GetMethod(
    "GetRecordAccessActionForReportOperation",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(reportRecordAccessAction, "Report management should resolve distinct record scopes for report run and CSV export.");
AssertEqual(
    PlatformPermissions.Form.View,
    (string)reportRecordAccessAction!.Invoke(null, new object[] { false })!,
    "Report runs should filter records through view scope.");
AssertEqual(
    PlatformPermissions.Form.Export,
    (string)reportRecordAccessAction.Invoke(null, new object[] { true })!,
    "CSV exports should filter records through export scope.");
var chartSourceReportConfig = typeof(ChartAggregationService).GetMethod(
    "GetSourceReportConfigAsync",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
AssertNotNull(chartSourceReportConfig, "Chart preview should resolve source report configs through a dedicated helper.");
var chartSourceReportConfigParameters = chartSourceReportConfig!.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
AssertTrue(
    chartSourceReportConfigParameters.Contains(typeof(ClaimsPrincipal))
        && chartSourceReportConfigParameters.Contains(typeof(PermissionService)),
    "Chart source report configs should receive the current principal and permission service for report-level checks.");

var pagedResult = new PagedResultDto<string>(2, new[] { "first", "second" });
AssertEqual(2, pagedResult.TotalCount, "Paged results should expose total count.");
AssertSequenceEqual(new[] { "first", "second" }, pagedResult.Items, "Paged results should expose typed items.");
AssertTypeAssignable<IReadOnlyRepository<User, Guid>, IRepository<User, Guid>>();
AssertTypeAssignable<IRepository<User, Guid>, EfRepository<User, Guid>>();

var sampleUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var sampleRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var sampleDepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
var sampleCreatedAt = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
var sampleUpdatedAt = sampleCreatedAt.AddMinutes(5);

var dashboardSummary = new DashboardSummaryResponse(
    "Open Business Platform",
    new[]
    {
        new DashboardMetric("users", "Users", 4),
        new DashboardMetric("forms", "Forms", 3),
        new DashboardMetric("records", "Records", 10),
        new DashboardMetric("reports", "Reports", 2),
        new DashboardMetric("audit_logs", "Audit logs", 7)
    },
    new[]
    {
        new DashboardActivityItem(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Record created", "Jane Cooper", sampleCreatedAt, "Completed")
    });
AssertEqual(4, dashboardSummary.Metrics.Single(metric => metric.Key == "users").Value, "Dashboard summary metrics should expose database-backed counts by key.");
AssertEqual("Record created", dashboardSummary.RecentActivity.Single().Event, "Dashboard summaries should expose recent audit activity.");
AssertTypeAssignable<object, DashboardSummaryService>();

var userDto = new UserDto(
    sampleUserId,
    "Platform Admin",
    "admin@company.test",
    true,
    "bootstrap",
    "bootstrap-admin",
    new[] { new UserRoleDto(sampleRoleId, "Admin") },
    new[] { new UserDepartmentDto(sampleDepartmentId, "Operations", true) },
    Array.Empty<UserGroupDto>(),
    "user-stamp",
    sampleCreatedAt,
    null,
    sampleUpdatedAt,
    null);
AssertEqual(sampleUserId, userDto.Id, "User DTO should expose the domain user id.");
AssertEqual("admin@company.test", userDto.Email, "User DTO should expose email.");
AssertEqual("Admin", userDto.Roles.Single().Name, "User DTO should expose assigned role names.");
AssertEqual("Operations", userDto.Departments.Single().Name, "User DTO should expose assigned department names.");

var roleDto = new RoleDto(sampleRoleId, "Admin", "Platform administrators", true, 1, "role-stamp", sampleCreatedAt, null, null, null);
AssertEqual(sampleRoleId, roleDto.Id, "Role DTO should expose role id.");
AssertEqual(1, roleDto.UserCount, "Role DTO should expose assigned user count.");

var departmentDto = new DepartmentDto(
    sampleDepartmentId,
    "Operations",
    null,
    null,
    true,
    3,
    "department-stamp",
    sampleCreatedAt,
    null,
    null,
    null);
AssertEqual(sampleDepartmentId, departmentDto.Id, "Department DTO should expose department id.");
AssertEqual(3, departmentDto.UserCount, "Department DTO should expose assigned user count.");

var authResponse = new AuthenticatedUserResponse(
    sampleUserId.ToString(),
    "Jane Cooper",
    "jane@company.test",
    new[] { "Builder" },
    new[] { PlatformPermissions.Menu.Forms, PlatformPermissions.Forms.Create });
AssertTrue(authResponse.Permissions.Contains(PlatformPermissions.Forms.Create), "Auth response should expose effective permissions.");

var createUser = new CreateUserRequest("Jane Cooper", "jane@company.test", "temporary-password-1", new[] { sampleRoleId }, new[] { sampleDepartmentId }, Array.Empty<Guid>(), true);
AssertEqual(true, createUser.IsActive, "Create user request should carry active state.");
AssertEqual("temporary-password-1", createUser.Password, "Create user request should carry the initial password.");

var updateUser = new UpdateUserRequest("Jane Cooper", true, new[] { sampleRoleId }, new[] { sampleDepartmentId }, Array.Empty<Guid>(), "user-stamp");
AssertEqual("user-stamp", updateUser.ConcurrencyStamp, "Update user request should carry concurrency stamp.");

var resetPassword = new ResetUserPasswordRequest("new-temporary-password-2");
AssertEqual("new-temporary-password-2", resetPassword.NewPassword, "Reset password request should carry the replacement password.");
var requestPasswordReset = new RequestPasswordResetRequest("jane@company.test");
AssertEqual("jane@company.test", requestPasswordReset.Email, "Password reset requests should carry the recovery email.");
var completePasswordReset = new CompletePasswordResetRequest("reset-token", "new-temporary-password-2");
AssertEqual("reset-token", completePasswordReset.Token, "Complete password reset requests should carry the raw token.");
AssertEqual("new-temporary-password-2", completePasswordReset.NewPassword, "Complete password reset requests should carry the replacement password.");

var rolePermissions = new RolePermissionsDto(
    sampleRoleId,
    new[] { PlatformPermissions.Menu.Forms, PlatformPermissions.Forms.Create },
    new[] { new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.View) },
    Array.Empty<RoleReportPermissionDto>(),
    Array.Empty<RoleFieldPermissionDto>());
AssertEqual(sampleRoleId, rolePermissions.RoleId, "Role permissions DTO should expose the role id.");
AssertEqual(PlatformPermissions.Form.View, rolePermissions.FormPermissions.Single().Action, "Role permissions DTO should expose form actions.");

var updateRolePermissions = new UpdateRolePermissionsRequest(
    new[] { PlatformPermissions.Menu.UsersAccess, PlatformPermissions.Users.Manage },
    new[] { new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.Manage) },
    Array.Empty<RoleReportPermissionDto>(),
    Array.Empty<RoleFieldPermissionDto>());
AssertTrue(updateRolePermissions.Permissions.Contains(PlatformPermissions.Users.Manage), "Update role permissions request should carry global permissions.");

var normalizeFormPermissions = typeof(IdentityManagementService).GetMethod(
    "NormalizeFormPermissions",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(normalizeFormPermissions, "Identity management should normalize duplicate form access scopes before saving.");
var normalizedFormPermissions = (IReadOnlyCollection<RoleFormPermissionDto>)normalizeFormPermissions!.Invoke(
    null,
    new object[]
    {
        new[]
        {
            new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.View, PlatformPermissions.RecordScopes.Own),
            new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.View, PlatformPermissions.RecordScopes.Department)
        }
    })!;
AssertEqual(1, normalizedFormPermissions.Count, "Form permissions should be unique by form and action.");
AssertEqual(PlatformPermissions.RecordScopes.Department, normalizedFormPermissions.Single().Scope, "Broader record scopes should win duplicate form action grants.");
var resolveRecordScopes = typeof(PermissionService).GetMethod(
    "ResolveRecordScopes",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(resolveRecordScopes, "Permission service should resolve implied manage scopes through a focused helper.");
var managedOwnScopes = (IReadOnlyCollection<string>)resolveRecordScopes!.Invoke(
    null,
    new object[]
    {
        new[]
        {
            new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.Manage, PlatformPermissions.RecordScopes.Own)
        },
        PlatformPermissions.Form.View
    })!;
AssertSequenceEqual(
    new[] { PlatformPermissions.RecordScopes.Own },
    managedOwnScopes,
    "Form manage grants should imply record actions without widening the configured record scope.");
var managedPlusDirectScopes = (IReadOnlyCollection<string>)resolveRecordScopes.Invoke(
    null,
    new object[]
    {
        new[]
        {
            new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.Manage, PlatformPermissions.RecordScopes.Own),
            new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.View, PlatformPermissions.RecordScopes.Department)
        },
        PlatformPermissions.Form.View
    })!;
AssertSequenceEqual(
    new[] { PlatformPermissions.RecordScopes.Department, PlatformPermissions.RecordScopes.Own },
    managedPlusDirectScopes.OrderBy(scope => scope).ToArray(),
    "Direct action scopes and implied manage scopes should combine with OR semantics.");

var normalizeFieldPermissions = typeof(IdentityManagementService).GetMethod(
    "NormalizeFieldPermissions",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(normalizeFieldPermissions, "Identity management should normalize duplicate field rules before saving.");
var normalizedFieldPermissions = (IReadOnlyCollection<RoleFieldPermissionDto>)normalizeFieldPermissions!.Invoke(
    null,
    new object[]
    {
        new[]
        {
            new RoleFieldPermissionDto(sampleDepartmentId, "salary", PlatformPermissions.FieldAccess.ReadOnly),
            new RoleFieldPermissionDto(sampleDepartmentId, "salary", PlatformPermissions.FieldAccess.Hidden)
        }
    })!;
AssertEqual(1, normalizedFieldPermissions.Count, "Field permissions should be unique by form and field.");
AssertEqual(PlatformPermissions.FieldAccess.Hidden, normalizedFieldPermissions.Single().Access, "Hidden field access should win over read-only access.");

var formAccessOption = new FormAccessOptionDto(sampleDepartmentId, "Expense request", "draft");
AssertEqual(sampleDepartmentId, formAccessOption.Id, "Form access option should expose the form id.");
AssertEqual("Expense request", formAccessOption.Name, "Form access option should expose the form name.");

var formSummary = new FormSummaryDto(
    sampleDepartmentId,
    "Expense request",
    "Employee reimbursement intake.",
    "draft",
    0,
    null,
    "form-stamp",
    sampleCreatedAt,
    null,
    sampleUpdatedAt,
    null);
AssertEqual(sampleDepartmentId, formSummary.Id, "Form summary DTO should expose the form id.");
AssertEqual("draft", formSummary.Status, "Form summary DTO should expose the form status.");
AssertEqual(0, formSummary.FieldCount, "New form summaries should expose the field count.");

var createForm = new CreateFormRequest("Expense request", "Employee reimbursement intake.");
AssertEqual("Expense request", createForm.Name, "Create form request should carry the form name.");
AssertEqual("Employee reimbursement intake.", createForm.Description, "Create form request should carry the optional description.");
AssertTypeAssignable<object, FormManagementService>();

var emptyBuilderSchema = new FormSchemaDefinition(
    1,
    Array.Empty<FormFieldDefinition>(),
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition(
            "page_1",
            "Page 1",
            null,
            new[]
            {
                new FormLayoutSectionDefinition("section_1", "Main", null, Array.Empty<FormLayoutRowDefinition>())
            })
    }));
AssertTrue(FormSchemaValidator.ValidateDraftSchema(emptyBuilderSchema).Valid, "Empty builder drafts should save to the backend before they are publishable.");
AssertFalse(FormSchemaValidator.ValidateSchema(emptyBuilderSchema).Valid, "Empty builder drafts should not publish.");

var publishableSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition("employee_name", FormFieldTypes.Text, "Employee name", Required: true)
    },
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition(
            "page_1",
            "Page 1",
            null,
            new[]
            {
                new FormLayoutSectionDefinition(
                    "section_1",
                    "Main",
                    null,
                    new[]
                    {
                        new FormLayoutRowDefinition(
                            "row_1",
                            new[]
                            {
                                new FormLayoutColumnDefinition(
                                    "col_1",
                                    new ResponsiveSpanDefinition(12, 12, 12),
                                    new[] { "employee_name" })
                            })
                    })
            })
    }));
var updateDraftRequest = new UpdateFormDraftRequest(publishableSchema);
AssertEqual(publishableSchema, updateDraftRequest.Schema, "Update form draft requests should carry the backend-owned schema.");
var updateDraftMetadataRequest = new UpdateFormDraftRequest(publishableSchema, "Updated expense request", null);
AssertEqual("Updated expense request", updateDraftMetadataRequest.Name, "Update form draft requests should carry optional form names.");
AssertNull(updateDraftMetadataRequest.Description, "Update form draft requests should allow clearing optional descriptions.");

var formDetail = new FormDetailDto(
    sampleDepartmentId,
    "Expense request",
    "Employee reimbursement intake.",
    "draft",
    1,
    null,
    publishableSchema,
    "form-stamp",
    sampleCreatedAt,
    null,
    sampleUpdatedAt,
    null);
AssertEqual(publishableSchema, formDetail.DraftSchema, "Form detail responses should expose the saved backend draft schema.");
AssertJsonColumn<FormDefinition>(model, nameof(FormDefinition.DraftSchemaJson));

var publishedVersion = new PublishedFormVersionDto(
    Guid.Parse("44444444-4444-4444-4444-444444444444"),
    sampleDepartmentId,
    1,
    publishableSchema,
    sampleUserId,
    sampleUpdatedAt);
var publishResponse = new PublishFormResponse(
    formDetail with { Status = FormStatuses.Published, CurrentVersionId = publishedVersion.Id },
    publishedVersion);
AssertEqual(FormStatuses.Published, publishResponse.Form.Status, "Publish responses should return refreshed form status.");
AssertEqual(1, publishResponse.Version.VersionNumber, "Publish responses should expose the immutable version number.");
AssertEqual(publishableSchema, publishResponse.Version.Schema, "Publish responses should expose the immutable published schema.");

var publishedSubmission = new PublishedFormSubmissionDto(
    sampleDepartmentId,
    "Expense request",
    "Employee reimbursement intake.",
    publishedVersion.Id,
    1,
    publishableSchema);
AssertEqual(publishedVersion.Id, publishedSubmission.CurrentVersionId, "Published submission responses should expose the immutable current version id.");
AssertEqual(1, publishedSubmission.CurrentVersionNumber, "Published submission responses should expose the immutable current version number.");
AssertEqual(publishableSchema, publishedSubmission.Schema, "Published submission responses should expose only the published schema.");
AssertTrue(PlatformPermissions.FormActions.Contains(PlatformPermissions.Form.Submit), "Form actions should include submit access for published form rendering.");

var submitRecordRequest = new SubmitRecordRequest(new Dictionary<string, object?>
{
    ["employee_name"] = "Jane Cooper"
});
AssertEqual("Jane Cooper", submitRecordRequest.Values["employee_name"], "Submit record requests should carry field values.");
AssertTrue(FormSchemaValidator.ValidateRecordValues(publishableSchema, submitRecordRequest.Values).Valid, "Publishable form schemas should validate submitted values.");
AssertFalse(FormSchemaValidator.ValidateRecordValues(publishableSchema, new Dictionary<string, object?>()).Valid, "Required published fields should be enforced for record submission.");

var recordDto = new FormRecordDto(
    Guid.Parse("55555555-5555-5555-5555-555555555555"),
    sampleDepartmentId,
    publishedVersion.Id,
    RecordStatuses.Active,
    sampleUserId,
    sampleDepartmentId,
    null,
    null,
    submitRecordRequest.Values,
    "record-stamp",
    sampleUpdatedAt,
    sampleUserId);
AssertEqual(publishedVersion.Id, recordDto.FormVersionId, "Record responses should expose the submitted form version id.");
AssertEqual("Jane Cooper", recordDto.Values["employee_name"], "Record responses should expose submitted values.");
AssertTypeAssignable<object, RecordSubmissionService>();

var recordListRequest = new ListRecordsRequest(Page: 2, PageSize: 10, Search: "Jane");
AssertEqual(2, recordListRequest.Page, "List records requests should carry the requested page.");
AssertEqual(10, recordListRequest.PageSize, "List records requests should carry the requested page size.");
AssertEqual("Jane", recordListRequest.Search, "List records requests should carry the search term.");

var recordListItem = new FormRecordListItemDto(
    recordDto.Id,
    recordDto.FormId,
    recordDto.FormVersionId,
    RecordStatuses.Active,
    recordDto.OwnerId,
    recordDto.DepartmentId,
    recordDto.AssignedToUserId,
    recordDto.AssignedGroupId,
    recordDto.Values,
    recordDto.CreatedAt,
    recordDto.CreatedById);
AssertEqual(recordDto.FormVersionId, recordListItem.FormVersionId, "Record list items should expose the stored form version id.");
AssertEqual("Jane Cooper", recordListItem.Values["employee_name"], "Record list items should expose submitted values for default list views.");

var recordDetail = new FormRecordDetailDto(
    recordDto.Id,
    recordDto.FormId,
    recordDto.FormVersionId,
    RecordStatuses.Active,
    recordDto.OwnerId,
    recordDto.DepartmentId,
    recordDto.AssignedToUserId,
    recordDto.AssignedGroupId,
    recordDto.Values,
    publishableSchema,
    Array.Empty<string>(),
    recordDto.ConcurrencyStamp,
    recordDto.CreatedAt,
    recordDto.CreatedById,
    null,
    null);
AssertEqual(publishableSchema, recordDetail.Schema, "Record details should return the immutable form version schema used at submission.");
AssertTypeAssignable<object, RecordQueryService>();

var updateRecordRequest = new UpdateRecordRequest(
    new Dictionary<string, object?>
    {
        ["employee_name"] = "Jordan Lee"
    },
    recordDto.ConcurrencyStamp);
AssertEqual("Jordan Lee", updateRecordRequest.Values["employee_name"], "Update record requests should carry replacement field values.");
AssertEqual(recordDto.ConcurrencyStamp, updateRecordRequest.ConcurrencyStamp, "Update record requests should carry concurrency stamps.");
AssertTypeAssignable<object, RecordMutationService>();

var mergeProtectedValues = typeof(RecordMutationService).GetMethod(
    "MergeProtectedValues",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(mergeProtectedValues, "Record mutation should preserve protected field values before validating updates.");
var mergedProtectedValues = (IReadOnlyDictionary<string, object?>)mergeProtectedValues!.Invoke(
    null,
    new object[]
    {
        new Dictionary<string, object?>
        {
            ["employee_name"] = "Jane Cooper",
            ["email"] = "jane@company.test",
            ["salary"] = 100000
        },
        new Dictionary<string, object?>
        {
            ["employee_name"] = "Jordan Lee"
        },
        new FieldAccessResult(
            new HashSet<string>(new[] { "salary" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "email" }, StringComparer.Ordinal))
    })!;
AssertEqual("Jordan Lee", mergedProtectedValues["employee_name"], "Record updates should keep editable submitted values.");
AssertEqual("jane@company.test", mergedProtectedValues["email"], "Record updates should preserve omitted read-only values.");
AssertEqual(100000, Convert.ToInt32(mergedProtectedValues["salary"]), "Record updates should preserve omitted hidden values.");
var removeHiddenFieldsFromSchema = typeof(RecordQueryService).GetMethod(
    "RemoveHiddenFieldsFromSchema",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(removeHiddenFieldsFromSchema, "Record detail should remove hidden field metadata from returned schemas.");
var sensitiveSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition("employee_name", FormFieldTypes.Text, "Employee name", Required: true),
        new FormFieldDefinition("salary", FormFieldTypes.Number, "Salary")
    },
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition(
            "page_1",
            "Employee",
            null,
            new[]
            {
                new FormLayoutSectionDefinition(
                    "section_1",
                    null,
                    null,
                    new[]
                    {
                        new FormLayoutRowDefinition(
                            "row_1",
                            new[]
                            {
                                new FormLayoutColumnDefinition(
                                    "column_1",
                                    new ResponsiveSpanDefinition(12, 12, 12),
                                    new[] { "employee_name", "salary" })
                            })
                    })
            })
    }));
var sanitizedSchema = (FormSchemaDefinition)removeHiddenFieldsFromSchema!.Invoke(
    null,
    new object[]
    {
        sensitiveSchema,
        new HashSet<string>(new[] { "salary" }, StringComparer.Ordinal)
    })!;
AssertFalse(sanitizedSchema.Fields.Any(field => field.Id == "salary"), "Hidden fields should be removed from record detail schemas.");
AssertSequenceEqual(
    new[] { "employee_name" },
    sanitizedSchema.Layout.Pages.Single().Sections.Single().Rows.Single().Columns.Single().Fields,
    "Hidden fields should be removed from record detail layout references.");

var reportingSchema = publishableSchema with
{
    Fields = new FormFieldDefinition[]
    {
        new("employee_name", FormFieldTypes.Text, "Employee name", Required: true),
        new("salary", FormFieldTypes.Number, "Salary"),
        new(
            "department",
            FormFieldTypes.Select,
            "Department",
            Options: new[]
            {
                new FormFieldOptionDefinition("opt_hr", "Human Resources", "hr"),
                new FormFieldOptionDefinition("opt_finance", "Finance", "finance")
            })
    }
};
var reportableFields = FormReportableFieldMetadata.GetReportableFields(reportingSchema);
AssertTrue(reportableFields.Any(field => field.Id == "employee_name" && field.Label == "Employee name" && field.Source == ReportableFieldSources.Form), "Reportable metadata should include form text fields.");
AssertTrue(reportableFields.Any(field => field.Id == "salary" && field.SupportsAggregation), "Reportable metadata should mark number fields as aggregatable.");
AssertTrue(reportableFields.Any(field => field.Id == "department" && field.SupportsChoiceGrouping), "Reportable metadata should mark choice fields as groupable.");
AssertEqual("Human Resources", reportableFields.Single(field => field.Id == "department").Options.Single(option => option.Value == "hr").Label, "Reportable metadata should preserve option labels.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.UpdatedAt), "Reportable metadata should include updated date system field.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.OwnerId), "Reportable metadata should include owner system field.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.DepartmentId), "Reportable metadata should include department system field.");

var listReportConfig = new ListReportConfigDefinition(
    1,
    new[]
    {
        new ListReportColumnDefinition("employee_name", "Employee name", true, 180)
    },
    new[]
    {
        new ListReportFilterDefinition("status", ReportFilterOperators.Equal, "active")
    },
    new[]
    {
        new ListReportSortDefinition("created_at", ReportSortDirections.Desc)
    });
var createReportRequest = new CreateListReportRequest("Employee directory", listReportConfig);
AssertEqual("Employee directory", createReportRequest.Name, "Create list report requests should carry the report name.");
AssertTrue(ListReportConfigValidator.Validate(publishableSchema, listReportConfig).Valid, "List report configs should validate against known form fields and system fields.");
AssertTrue(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with
        {
            Columns = new[] { new ListReportColumnDefinition(ReportableSystemFields.UpdatedAt, "Updated date", true, 140) },
            Filters = new[] { new ListReportFilterDefinition(ReportableSystemFields.DepartmentId, ReportFilterOperators.Equal, sampleDepartmentId.ToString()) },
            Sort = new[] { new ListReportSortDefinition(ReportableSystemFields.OwnerId, ReportSortDirections.Asc) }
        }).Valid,
    "List report configs should validate against normalized system field metadata.");
AssertFalse(
    ListReportConfigValidator.Validate(
        publishableSchema,
        listReportConfig with
        {
            Columns = new[] { new ListReportColumnDefinition("missing_field", "Missing", true, 180) }
        }).Valid,
    "List report configs should reject unknown fields.");

var reportSummary = new ListReportSummaryDto(
    Guid.Parse("66666666-6666-6666-6666-666666666666"),
    sampleDepartmentId,
    "Expense request",
    "Employee directory",
    ReportTypes.List,
    1,
    1,
    1,
    "report-stamp",
    sampleUpdatedAt,
    sampleUserId,
    null,
    null);
AssertEqual(1, reportSummary.ColumnCount, "List report summaries should expose configured column counts.");
AssertEqual("Employee directory", reportSummary.Name, "List report summaries should expose names.");

var executionConfig = listReportConfig with
{
    Columns = new[]
    {
        new ListReportColumnDefinition("employee_name", "Employee name", true, 180),
        new ListReportColumnDefinition("salary", "Salary", true, 120),
        new ListReportColumnDefinition(ReportableSystemFields.Status, "Status", true, 100)
    },
    Filters = new[] { new ListReportFilterDefinition(ReportableSystemFields.Status, ReportFilterOperators.Equal, RecordStatuses.Active) },
    Sort = new[] { new ListReportSortDefinition("salary", ReportSortDirections.Desc) }
};
var executionRecords = new[]
{
    new FormRecord
    {
        Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        FormId = sampleDepartmentId,
        FormVersionId = publishedVersion.Id,
        Status = RecordStatuses.Active,
        ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
        {
            ["employee_name"] = "Jordan Lee",
            ["salary"] = 80000,
            ["department"] = "finance"
        }),
        CreatedAt = sampleCreatedAt,
        CreatedById = sampleUserId
    },
    new FormRecord
    {
        Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
        FormId = sampleDepartmentId,
        FormVersionId = publishedVersion.Id,
        Status = RecordStatuses.Active,
        ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
        {
            ["employee_name"] = "Jane Cooper",
            ["salary"] = 120000,
            ["department"] = "hr"
        }),
        CreatedAt = sampleCreatedAt.AddMinutes(1),
        CreatedById = sampleUserId
    },
    new FormRecord
    {
        Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
        FormId = sampleDepartmentId,
        FormVersionId = publishedVersion.Id,
        Status = RecordStatuses.Deleted,
        ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
        {
            ["employee_name"] = "Archived Person",
            ["salary"] = 200000,
            ["department"] = "hr"
        }),
        CreatedAt = sampleCreatedAt.AddMinutes(2),
        CreatedById = sampleUserId
    }
};

var executedReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig,
    reportingSchema,
    executionRecords,
    new RunListReportRequest(Page: 1, PageSize: 10, Search: "Jane"));
AssertEqual(1, executedReport.TotalCount, "Report execution should apply runtime search after saved filters.");
AssertEqual("Jane Cooper", executedReport.Rows.Single().Cells["employee_name"].DisplayValue, "Report rows should expose display cells by field.");
AssertEqual(RecordStatuses.Active, executedReport.Rows.Single().Cells[ReportableSystemFields.Status].Value, "Report rows should expose system field values.");

var pagedExecutionReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig,
    reportingSchema,
    executionRecords,
    new RunListReportRequest(Page: 1, PageSize: 1));
AssertEqual(2, pagedExecutionReport.TotalCount, "Report execution should count rows after saved filters and before pagination.");
AssertEqual(1, pagedExecutionReport.Rows.Count, "Report execution should page rows.");
AssertEqual("Jane Cooper", pagedExecutionReport.Rows.Single().Cells["employee_name"].DisplayValue, "Report execution should apply saved sort before pagination.");
var fullExecutionReport = ListReportExecutionEngine.ExecuteAll(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig,
    reportingSchema,
    executionRecords,
    search: null);
AssertEqual(2, fullExecutionReport.Rows.Count, "Report CSV export should be able to execute all matching rows without pagination.");
AssertEqual("Jane Cooper", fullExecutionReport.Rows.First().Cells["employee_name"].DisplayValue, "Full report execution should preserve saved sort order.");

var csvReport = new ListReportExecutionDto(
    reportSummary.Id,
    sampleDepartmentId,
    "Employee directory / export",
    "Employee information",
    1,
    1,
    1,
    new[]
    {
        new ListReportExecutionColumnDto("employee_name", "Employee, name", FormFieldTypes.Text, ReportableFieldSources.Form, 180),
        new ListReportExecutionColumnDto("notes", "Notes", FormFieldTypes.Textarea, ReportableFieldSources.Form, 240)
    },
    new[]
    {
        new ListReportExecutionRowDto(
            Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa"),
            RecordStatuses.Active,
            new Dictionary<string, ListReportExecutionCellDto>
            {
                ["employee_name"] = new("Jane \"JJ\" Cooper", "Jane \"JJ\" Cooper"),
                ["notes"] = new("Line one\nLine two, checked", "Line one\nLine two, checked")
            },
            sampleCreatedAt)
    });
var csvExport = ListReportCsvExporter.Export(csvReport);
AssertEqual("employee-directory-export.csv", csvExport.FileName, "CSV export filenames should be safe and based on the report name.");
AssertEqual(
    "\"Employee, name\",Notes\r\n\"Jane \"\"JJ\"\" Cooper\",\"Line one\nLine two, checked\"\r\n",
    csvExport.Content,
    "CSV export should include visible column labels and escape commas, quotes, and newlines.");
AssertTypeAssignable<object, ReportManagementService>();

var chartConfig = new ChartWidgetConfigDefinition(
    ChartWidgetTypes.BarChart,
    new ChartMetricDefinition(ChartMetricTypes.Count, null),
    GroupByFieldId: "department",
    DateFieldId: null,
    Columns: Array.Empty<string>(),
    Limit: 5,
    ReportId: null);
AssertTrue(ChartWidgetConfigValidator.Validate(reportingSchema, chartConfig).Valid, "Chart widget configs should validate groupable fields.");

var chartResult = ChartAggregationEngine.Execute(
    sampleDepartmentId,
    "Employee information",
    chartConfig,
    reportingSchema,
    executionRecords.Where(record => record.Status == RecordStatuses.Active).ToArray());
AssertEqual(ChartWidgetTypes.BarChart, chartResult.WidgetType, "Chart aggregation should return the requested widget type.");
AssertEqual(2, chartResult.Series.Count, "Bar chart aggregation should group active records.");
AssertEqual("Human Resources", chartResult.Series.Single(point => point.Key == "hr").Label, "Choice chart labels should use option labels.");
AssertEqual(1m, chartResult.Series.Single(point => point.Key == "hr").Value, "Count chart values should count records per group.");

var analyticsBreakdownRequest = new DashboardAnalyticsRequest(
    DashboardAnalyticsWidgetTypes.Breakdown,
    new DashboardAnalyticsSourceDefinition(sampleDepartmentId),
    new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Count),
    GroupByFieldId: "department",
    DateFieldId: null,
    Columns: Array.Empty<string>(),
    Limit: 5);
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsBreakdownRequest).Valid,
    "Dashboard analytics should validate grouped breakdown requests.");

var analyticsSummaryRequest = analyticsBreakdownRequest with
{
    WidgetType = DashboardAnalyticsWidgetTypes.Summary,
    Metric = new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Sum, "salary"),
    GroupByFieldId = null
};
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsSummaryRequest).Valid,
    "Dashboard analytics should validate numeric summary metrics.");

var analyticsTrendRequest = analyticsBreakdownRequest with
{
    WidgetType = DashboardAnalyticsWidgetTypes.Trend,
    DateFieldId = ReportableSystemFields.CreatedAt,
    GroupByFieldId = null
};
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsTrendRequest).Valid,
    "Dashboard analytics should validate date trend requests.");

var analyticsTableRequest = analyticsBreakdownRequest with
{
    WidgetType = DashboardAnalyticsWidgetTypes.Table,
    GroupByFieldId = null,
    Columns = new[] { "employee_name", ReportableSystemFields.Status }
};
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsTableRequest).Valid,
    "Dashboard analytics should validate table slice requests.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsSummaryRequest with { Metric = new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Average, "department") }).Valid,
    "Dashboard analytics should reject non-numeric average metrics.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsBreakdownRequest with { GroupByFieldId = "salary" }).Valid,
    "Dashboard analytics should reject non-choice grouping fields.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsTrendRequest with { DateFieldId = "department" }).Valid,
    "Dashboard analytics should reject non-date trend fields.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsTableRequest with { Columns = new[] { "missing_field" } }).Valid,
    "Dashboard analytics should reject unknown table columns.");

AssertTypeAssignable<object, DashboardAnalyticsService>();
var invalidAnalytics = DashboardAnalyticsRequestValidator.Validate(
    reportingSchema,
    analyticsBreakdownRequest with { Limit = 100 });
AssertEqual(
    "dashboard.analytics.limit.range",
    invalidAnalytics.Errors.Single().Code,
    "Dashboard analytics limit errors should have stable structured codes.");
AssertNotNull(
    typeof(DashboardAnalyticsService).GetMethod(nameof(DashboardAnalyticsService.RunAsync))?.GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(ClaimsPrincipal)),
    "Dashboard analytics execution should receive the current principal.");
var analyticsSourceReportConfig = typeof(DashboardAnalyticsService).GetMethod(
    "GetSourceReportConfigAsync",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
AssertNotNull(analyticsSourceReportConfig, "Dashboard analytics should resolve source report configs through a dedicated helper.");
var analyticsSourceReportConfigParameters = analyticsSourceReportConfig!.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
AssertTrue(
    analyticsSourceReportConfigParameters.Contains(typeof(ClaimsPrincipal))
        && analyticsSourceReportConfigParameters.Contains(typeof(PermissionService)),
    "Dashboard analytics source report configs should receive the current principal and permission service for report-level checks.");
var ensureVisibleAnalyticsRequest = typeof(DashboardAnalyticsService).GetMethod(
    "EnsureVisibleRequest",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
AssertNotNull(ensureVisibleAnalyticsRequest, "Dashboard analytics should check hidden direct field references through a focused helper.");
try
{
    ensureVisibleAnalyticsRequest!.Invoke(
        null,
        new object[]
        {
            analyticsTableRequest with { Columns = new[] { "salary" } },
            new HashSet<string>(new[] { "salary" }, StringComparer.Ordinal)
        });
    throw new InvalidOperationException("Hidden dashboard analytics table fields should be rejected.");
}
catch (System.Reflection.TargetInvocationException exception)
{
    var analyticsException = exception.InnerException as DashboardAnalyticsException;
    AssertNotNull(analyticsException, "Hidden dashboard analytics table fields should raise a dashboard analytics exception.");
    AssertEqual(403, analyticsException!.StatusCode, "Hidden dashboard analytics field references should be forbidden.");
}

var tableChartResult = ChartAggregationEngine.Execute(
    sampleDepartmentId,
    "Employee information",
    chartConfig with
    {
        WidgetType = ChartWidgetTypes.Table,
        Columns = new[] { "employee_name", ReportableSystemFields.Status },
        GroupByFieldId = null
    },
    reportingSchema,
    executionRecords.Where(record => record.Status == RecordStatuses.Active).ToArray());
AssertEqual(2, tableChartResult.Rows.Count, "Table chart widgets should return record rows.");
AssertEqual("Jane Cooper", tableChartResult.Rows.First().Cells["employee_name"].DisplayValue, "Table chart rows should expose display cells.");
AssertTypeAssignable<object, ChartAggregationService>();

var analyticsChartConfig = new ChartWidgetConfigDefinition(
    ChartWidgetTypes.DateTrend,
    new ChartMetricDefinition(ChartMetricTypes.Count),
    GroupByFieldId: null,
    DateFieldId: ReportableSystemFields.CreatedAt,
    Columns: Array.Empty<string>(),
    Limit: 5,
    ReportId: null);
var analyticsTrendPreview = ChartAggregationEngine.Execute(
    sampleDepartmentId,
    "Employee information",
    analyticsChartConfig,
    reportingSchema,
    executionRecords.Where(record => record.Status == RecordStatuses.Active).ToArray());
AssertEqual(2, analyticsTrendPreview.TotalCount, "Dashboard analytics trend execution should count permitted active records.");
AssertTrue(analyticsTrendPreview.Series.Count > 0, "Dashboard analytics trend execution should return date series points.");

var dashboardConfig = new SavedDashboardConfigDefinition(
    1,
    new[]
    {
        new SavedDashboardWidgetDefinition(
            "widget-1",
            "Employees by department",
            sampleDepartmentId,
            chartConfig)
    });
var dashboardLayout = new SavedDashboardLayoutDefinition(
    1,
    new[]
    {
        new SavedDashboardWidgetLayoutDefinition("widget-1", DashboardWidgetWidths.Wide, 1)
    });
var dashboardSources = new[]
{
    new DashboardSourceDefinition(
        sampleDepartmentId,
        reportingSchema,
        new[]
        {
            new DashboardSourceReportDefinition(Guid.Parse("99999999-9999-9999-9999-999999999999"), ReportTypes.List)
        })
};
AssertTrue(DashboardDefinitionValidator.Validate(dashboardConfig, dashboardLayout, dashboardSources).Valid, "Dashboard configs should validate known chart widgets and layout ids.");

var invalidDashboardConfig = dashboardConfig with
{
    Widgets = new[]
    {
        dashboardConfig.Widgets.Single(),
        dashboardConfig.Widgets.Single() with { Title = "Duplicate id" }
    }
};
AssertFalse(DashboardDefinitionValidator.Validate(invalidDashboardConfig, dashboardLayout, dashboardSources).Valid, "Dashboard configs should reject duplicate widget ids.");

var invalidLayout = dashboardLayout with
{
    Widgets = new[]
    {
        new SavedDashboardWidgetLayoutDefinition("missing-widget", DashboardWidgetWidths.Full, 1)
    }
};
AssertFalse(DashboardDefinitionValidator.Validate(dashboardConfig, invalidLayout, dashboardSources).Valid, "Dashboard configs should reject layout widgets that do not match config widgets.");

var createDashboardRequest = new CreateDashboardRequest(
    "Operations dashboard",
    "Saved widgets for V2 dashboards.",
    dashboardConfig,
    dashboardLayout);
AssertEqual("Operations dashboard", createDashboardRequest.Name, "Create dashboard requests should carry dashboard names.");
AssertEqual(1, createDashboardRequest.Config.Widgets.Count, "Create dashboard requests should carry widgets.");
AssertTypeAssignable<object, DashboardDefinitionService>();
AssertTypeAssignable<IPlatformApiModule, DashboardsModule>();

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyCollection<T> expected, IReadOnlyCollection<T> actual, string message)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException($"{message} Expected: [{string.Join(", ", expected)}]. Actual: [{string.Join(", ", actual)}].");
    }
}

static void AssertNotNull(object? value, string message)
{
    if (value is null)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertNull(object? value, string message)
{
    if (value is not null)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertTrue(bool value, string message)
{
    if (!value)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool value, string message)
{
    if (value)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertNotEqual<T>(T notExpected, T actual, string message)
{
    if (EqualityComparer<T>.Default.Equals(notExpected, actual))
    {
        throw new InvalidOperationException($"{message} Value should not be: {notExpected}.");
    }
}

static void RunWithEnvironment(IReadOnlyDictionary<string, string?> values, Action action)
{
    var previousValues = values.ToDictionary(
        pair => pair.Key,
        pair => Environment.GetEnvironmentVariable(pair.Key));

    try
    {
        foreach (var (key, value) in values)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        action();
    }
    finally
    {
        foreach (var (key, value) in previousValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

static string GetRepositoryFilePath(params string[] relativeSegments)
{
    var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (currentDirectory is not null)
    {
        var candidate = Path.Combine(new[] { currentDirectory.FullName }.Concat(relativeSegments).ToArray());

        if (File.Exists(candidate))
        {
            return candidate;
        }

        currentDirectory = currentDirectory.Parent;
    }

    throw new InvalidOperationException($"Could not find repository file: {string.Join("/", relativeSegments)}.");
}

static void AssertTable<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string expectedTable)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");

    AssertEqual(expectedTable, entity.GetTableName(), $"{typeof(TEntity).Name} should map to the expected table.");
}

static void AssertTypeAssignable<TBase, TEntity>()
{
    if (!typeof(TBase).IsAssignableFrom(typeof(TEntity)))
    {
        throw new InvalidOperationException($"{typeof(TEntity).Name} should inherit from {typeof(TBase).Name}.");
    }
}

static void AssertGuidId<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");
    var property = entity.FindProperty(nameof(Entity<Guid>.Id))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name}.Id should be mapped.");

    AssertEqual(typeof(Guid), property.ClrType, $"{typeof(TEntity).Name}.Id should be a Guid.");
    AssertEqual("uuid", property.GetColumnType(), $"{typeof(TEntity).Name}.Id should use PostgreSQL uuid.");
}

static void AssertJsonColumn<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string propertyName)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");
    var property = entity.FindProperty(propertyName)
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name}.{propertyName} should be mapped.");

    AssertEqual("jsonb", property.GetColumnType(), $"{typeof(TEntity).Name}.{propertyName} should use PostgreSQL JSONB.");
}

static void AssertColumn<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string propertyName, string expectedColumn, string message)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");
    var property = entity.FindProperty(propertyName)
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name}.{propertyName} should be mapped.");

    AssertEqual(expectedColumn, property.GetColumnName(), message);
}

static void AssertIndex<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string[] propertyNames, string message)
{
    AssertIndexCore<TEntity>(model, propertyNames, unique: false, message);
}

static void AssertUniqueIndex<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string[] propertyNames, string message)
{
    AssertIndexCore<TEntity>(model, propertyNames, unique: true, message);
}

static void AssertIndexCore<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string[] propertyNames, bool unique, string message)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");

    var hasIndex = entity
        .GetIndexes()
        .Any(index =>
            index.IsUnique == unique
            && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

    if (!hasIndex)
    {
        throw new InvalidOperationException($"{message} Expected index over [{string.Join(", ", propertyNames)}].");
    }
}
