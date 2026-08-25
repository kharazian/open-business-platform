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
using OpenBusinessPlatform.Api.Modules.CreatorAnalysis;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Integrations;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Printing;
using OpenBusinessPlatform.Api.Modules.Processing;
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;
using OpenBusinessPlatform.Api.Modules.Triggers;
using OpenBusinessPlatform.Api.Modules.Workflows;
using OpenBusinessPlatform.Api.Modules.Workspaces;
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

var workspaceOwnedRole = new Role();
WorkspaceOwnershipGuard.AssignForCreate(workspaceOwnedRole, WorkspaceDefaults.WorkspaceId);
AssertEqual(WorkspaceDefaults.WorkspaceId, workspaceOwnedRole.WorkspaceId, "New workspace-owned rows should inherit the active workspace.");
AssertThrows<InvalidOperationException>(
    () => WorkspaceOwnershipGuard.AssignForCreate(new Role { WorkspaceId = Guid.NewGuid() }, WorkspaceDefaults.WorkspaceId),
    "Creating data for another workspace should be rejected.");
AssertThrows<InvalidOperationException>(
    () => WorkspaceOwnershipGuard.EnsureActive(new Role { WorkspaceId = Guid.NewGuid() }, WorkspaceDefaults.WorkspaceId),
    "Updating or deleting data from another workspace should be rejected.");
var selectedWorkspaceId = Guid.NewGuid();
var selectedWorkspacePrincipal = new ClaimsPrincipal(new ClaimsIdentity(
    new[] { new Claim(WorkspaceClaims.WorkspaceId, selectedWorkspaceId.ToString()) },
    "test"));
AssertEqual(
    selectedWorkspaceId,
    HttpContextWorkspaceContext.ResolveWorkspaceId(selectedWorkspacePrincipal),
    "The request workspace should come from the authenticated workspace claim.");
AssertEqual(
    WorkspaceDefaults.WorkspaceId,
    HttpContextWorkspaceContext.ResolveWorkspaceId(new ClaimsPrincipal()),
    "Requests without a workspace claim should retain the compatibility workspace.");

var creatorAnalyzer = new CreatorExportAnalyzer();
const string creatorSecretSentinel = "TASK010_SECRET_7tK9vQ2mN8xR4pL6";
const string creatorCustomerSentinel = "TASK010_CUSTOMER_AcmeSensitiveName";
var creatorSource = $$"""
    application OrderBridge {
      form Orders {
        field Order_Number autonumber
        Customer
        (
          type = lookup
        )
      }
      report Orders_List list {
      }
      workflow Fulfillment {
      }
      function Push_Order {
        api_key = "{{creatorSecretSentinel}}"
      }
      connection ERP password = "{{creatorSecretSentinel}}"
      record {{creatorCustomerSentinel}}
      page Custom_Dashboard {
      }
    }
    """;
var creatorReport = creatorAnalyzer.Analyze(creatorSource, System.Text.Encoding.UTF8.GetByteCount(creatorSource));
var creatorReportJson = JsonSerializer.Serialize(creatorReport);
AssertFalse(creatorReport.CanImport, "Creator analysis must never enable import.");
AssertTrue(creatorReport.Constructs.Any(item => item.Type == "form" && item.Status == CreatorAnalysisStatuses.Supported), "Creator analysis should identify form candidates.");
AssertTrue(creatorReport.Constructs.Any(item => item.Type == "field" && item.ProposedType == "autonumber"), "Creator analysis should map direct field candidates.");
AssertTrue(creatorReport.Constructs.Any(item => item.Type == "field" && item.Status == CreatorAnalysisStatuses.ManualReview), "Creator lookups should require manual target mapping.");
AssertTrue(creatorReport.Constructs.Any(item => item.Type == "function" && item.Status == CreatorAnalysisStatuses.Unsafe), "Creator functions should remain unsafe and non-executable.");
AssertTrue(creatorReport.CredentialSignals.Any(item => item.Category == "api_key"), "Creator analysis should count credential categories.");
AssertFalse(creatorReportJson.Contains(creatorSecretSentinel, StringComparison.Ordinal), "Creator reports must never include detected secret values.");
AssertFalse(creatorReportJson.Contains(creatorCustomerSentinel, StringComparison.Ordinal), "Creator reports must never include source record values.");
AssertFalse(creatorReportJson.Contains("password =", StringComparison.OrdinalIgnoreCase), "Creator reports must never include credential source snippets.");
AssertEqual(creatorReportJson, JsonSerializer.Serialize(creatorAnalyzer.Analyze(creatorSource, System.Text.Encoding.UTF8.GetByteCount(creatorSource))), "Creator analysis should be deterministic.");
var unsafeCreatorNameReport = creatorAnalyzer.Analyze("form <script>alert(1)</script>", 31);
AssertEqual("[redacted]", unsafeCreatorNameReport.Constructs.Single().DisplayName, "Creator analysis should redact markup-bearing source names.");
var credentialCatalogSource = """
    connection ERP {
      private_key = "TASK010_PRIVATE_KEY_VALUE"
      authorization = "Bearer TASK010_AUTH_VALUE"
      client_secret = "TASK010_CLIENT_SECRET_VALUE"
      connection_string = "Host=internal;Password=TASK010_CONNECTION_VALUE"
    }
    """;
var credentialCatalogReport = creatorAnalyzer.Analyze(credentialCatalogSource, System.Text.Encoding.UTF8.GetByteCount(credentialCatalogSource));
var credentialCatalogJson = JsonSerializer.Serialize(credentialCatalogReport);
foreach (var category in new[] { "private_key", "authorization", "secret", "connection_credential", "password" })
    AssertTrue(credentialCatalogReport.CredentialSignals.Any(item => item.Category == category), $"Creator analysis should detect the {category} credential category.");
foreach (var value in new[] { "TASK010_PRIVATE_KEY_VALUE", "TASK010_AUTH_VALUE", "TASK010_CLIENT_SECRET_VALUE", "TASK010_CONNECTION_VALUE", "Host=internal" })
    AssertFalse(credentialCatalogJson.Contains(value, StringComparison.Ordinal), "Creator analysis should suppress every credential value and connection detail.");
var quotedCreatorReport = creatorAnalyzer.Analyze("// form Ignored\nvalue = \"function Hidden\"\nform Visible", 48);
AssertEqual(1, quotedCreatorReport.Constructs.Count, "Creator scanning should ignore construct keywords in comments and quoted values.");
var unknownCreatorReport = creatorAnalyzer.Analyze("future_widget Experimental {\n}", 31);
AssertTrue(unknownCreatorReport.Constructs.Any(item => item.Status == CreatorAnalysisStatuses.Unknown), "Creator analysis should retain unknown source sections.");
var malformedCreatorReport = creatorAnalyzer.Analyze("form Broken {\nfield Name text", 29);
AssertFalse(malformedCreatorReport.Complete, "Malformed Creator sources should produce an explicitly incomplete report.");
var boundedCreatorSource = string.Join('\n', Enumerable.Range(1, 600).Select(index => $"page Page_{index} {{ }}"));
var boundedCreatorReport = creatorAnalyzer.Analyze(boundedCreatorSource, System.Text.Encoding.UTF8.GetByteCount(boundedCreatorSource));
AssertTrue(boundedCreatorReport.Truncated, "Creator analysis should report construct truncation.");
AssertEqual(CreatorAnalysisLimits.MaxConstructs, boundedCreatorReport.Constructs.Count, "Creator analysis should cap returned constructs.");
AssertTrue(boundedCreatorReport.Summary.ConstructCount > boundedCreatorReport.Constructs.Count, "Creator summaries should retain the observed count after result truncation.");
var findingBoundSource = string.Join('\n', Enumerable.Range(1, 1_200).SelectMany(index => new[] { $"future_{index} {{", "}" }));
var findingBoundReport = creatorAnalyzer.Analyze(findingBoundSource, System.Text.Encoding.UTF8.GetByteCount(findingBoundSource));
AssertEqual(CreatorAnalysisLimits.MaxFindings, findingBoundReport.Findings.Count, "Creator analysis should cap returned findings.");
AssertTrue(findingBoundReport.Summary.FindingCount > findingBoundReport.Findings.Count, "Creator summaries should retain observed finding counts after truncation.");
CreatorAnalysisInputValidator.ValidateMetadata("source.ds", "text/plain; charset=utf-8", CreatorAnalysisLimits.MaxSourceBytes);
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.ValidateMetadata("source.zip", "text/plain", 10), "Creator analysis should reject archive extensions.");
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.ValidateMetadata("source.ds", "application/octet-stream", 10), "Creator analysis should reject non-text content types.");
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.ValidateMetadata("source.ds", "text/plain", CreatorAnalysisLimits.MaxSourceBytes + 1L), "Creator analysis should reject oversized metadata.");
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.ValidateMetadata("source.ds", "text/plain", 0), "Creator analysis should reject empty files.");
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.DecodeAndValidate(System.Text.Encoding.UTF8.GetBytes("  \r\n\t")), "Creator analysis should reject whitespace-only input.");
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.DecodeAndValidate([0xff, 0xfe]), "Creator analysis should reject malformed UTF-8.");
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.DecodeAndValidate([0x66, 0x00, 0x6f]), "Creator analysis should reject binary control bytes.");
var excessiveCreatorLines = System.Text.Encoding.UTF8.GetBytes(string.Join('\r', Enumerable.Repeat("form X", CreatorAnalysisLimits.MaxLines + 1)));
AssertThrows<CreatorAnalysisException>(() => CreatorAnalysisInputValidator.DecodeAndValidate(excessiveCreatorLines), "Creator analysis should bound CR-only line input.");

var validExportProcessingJob = new CreateProcessingJobRequest(
    "Nightly employee export",
    ProcessingJobKinds.RecordExport,
    new ProcessingJobConfigDefinition(Guid.NewGuid(), "nightly-export", "form_records", "csv", MaxRows: 5000),
    new ProcessingJobScheduleDefinition("daily", "UTC", DateTimeOffset.UtcNow.AddHours(1)),
    new ProcessingJobRetryPolicyDefinition(true, 5, 30),
    true);
AssertTrue(ProcessingJobValidator.Validate(validExportProcessingJob).Valid, "A bounded scheduled export processing job should be valid.");
AssertTrue(ProcessingJobValidator.Validate(validExportProcessingJob with
{
    FailureNotificationPolicy = new ProcessingFailureNotificationPolicyDefinition(true, true, new[] { Guid.NewGuid() })
}).Valid, "Processing failure notifications should accept an owner and bounded explicit recipients.");
AssertTrue(ProcessingJobValidator.Validate(validExportProcessingJob with
{
    FailureNotificationPolicy = new ProcessingFailureNotificationPolicyDefinition(true, false, Array.Empty<Guid>())
}).Errors.Any(error => error.Code == "processing.notifications.recipient_required"),
    "Enabled processing failure notifications should require at least one recipient.");
var duplicateNotificationRecipient = Guid.NewGuid();
AssertTrue(ProcessingJobValidator.Validate(validExportProcessingJob with
{
    FailureNotificationPolicy = new ProcessingFailureNotificationPolicyDefinition(true, false, new[] { duplicateNotificationRecipient, duplicateNotificationRecipient })
}).Errors.Any(error => error.Code == "processing.notifications.recipient_duplicate"),
    "Processing failure notification recipients should be unique.");
AssertTrue(ProcessingOperationalEventCodes.Supported.Contains(ProcessingOperationalEventCodes.RetryExhausted), "Processing operations should expose retry exhaustion events.");
AssertTrue(!ProcessingJobValidator.Validate(validExportProcessingJob with
{
    Config = validExportProcessingJob.Config with { MaxRows = 5001 }
}).Valid, "Export processing jobs should reject row limits above 5000.");
AssertTrue(!ProcessingJobValidator.Validate(validExportProcessingJob with
{
    Kind = ProcessingJobKinds.CsvRecordImport
}).Valid, "CSV import processing jobs should reject export schedules and retry policy.");
AssertTrue(!ProcessingJobValidator.ValidateManualRun(
    ProcessingJobKinds.CsvRecordImport,
    new CreateProcessingJobRunRequest("records.csv", new string('x', ProcessingJobValidator.MaxCsvBytes + 1))).Valid,
    "Queued CSV inputs should enforce the private input byte limit.");
var strictImportMapping = new RecordImportMappingDefinition(
    new[] { new RecordImportFieldMappingDefinition("email", "email") })
{
    AdditionalProperties = new Dictionary<string, JsonElement>
    {
        ["script"] = JsonSerializer.SerializeToElement("not allowed")
    }
};
var strictImportValidation = ProcessingJobValidator.Validate(new CreateProcessingJobRequest(
    "Strict import",
    ProcessingJobKinds.CsvRecordImport,
    new ProcessingJobConfigDefinition(Guid.NewGuid(), "strict-import", Mapping: strictImportMapping)));
AssertTrue(
    strictImportValidation.Errors.Any(error => error.Code == "processing.config.mapping_properties"),
    "Processing import mappings should reject unknown nested properties.");
AssertEqual(
    DateTimeOffset.Parse("2026-08-20T00:00:00Z"),
    RecurringScheduleCalculator.CalculateNextRun(
        new RecurringSchedule("daily", "UTC", DateTimeOffset.Parse("2026-08-19T00:00:00Z")),
        DateTimeOffset.Parse("2026-08-19T12:00:00Z")),
    "The shared recurring schedule calculator should advance deterministically.");

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
    AssertEqual(
        300,
        appSettings.RootElement.GetProperty("AutomationHealth").GetProperty("PendingAgeWarningSeconds").GetInt32(),
        "Checked-in automation health should use a five-minute pending warning threshold.");
}

RunWithEnvironment(
    new Dictionary<string, string?>
    {
        ["AUTH_COOKIE_NAME"] = "obp_test.auth",
        ["AUTH_COOKIE_REQUIRE_SECURE"] = "false",
        ["AUTOMATION_PENDING_AGE_WARNING_SECONDS"] = "420",
        ["AUTOMATION_METRICS_TOKEN"] = "test-metrics-token",
        ["Authentication__CookieName"] = null,
        ["Authentication__RequireSecureCookies"] = null,
        ["AutomationHealth__PendingAgeWarningSeconds"] = null,
        ["AutomationHealth__MetricsToken"] = null,
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
        AssertEqual("420", Environment.GetEnvironmentVariable("AutomationHealth__PendingAgeWarningSeconds"), "Automation pending-age thresholds should be configurable from deployment environment variables.");
        AssertEqual("test-metrics-token", Environment.GetEnvironmentVariable("AutomationHealth__MetricsToken"), "Automation metrics tokens should remain server-side configuration.");
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
AssertTable<Tenant>(model, "tenants");
AssertTable<Workspace>(model, "workspaces");
AssertTable<WorkspaceMembership>(model, "workspace_memberships");
AssertTable<WorkspaceBranding>(model, "workspace_branding");
AssertTable<WorkspaceLocalization>(model, "workspace_localizations");
AssertTable<UserLocalizationPreference>(model, "user_localization_preferences");
AssertTable<WorkspaceCustomDomain>(model, "workspace_custom_domains");
AssertTable<SsoProvider>(model, "sso_providers");
AssertTable<ExternalIdentity>(model, "external_identities");
AssertTable<AccessPolicy>(model, "access_policies");
AssertTable<RetentionPolicy>(model, "retention_policies");
AssertTable<LegalHold>(model, "legal_holds");
AssertTable<AdministrativeBackup>(model, "administrative_backups");
AssertTable<RestorePlan>(model, "restore_plans");
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
AssertTable<FormAutonumberSequence>(model, "form_autonumber_sequences");
AssertTable<FileAttachment>(model, "file_attachments");
AssertTable<RecordRelationship>(model, "record_relationships");
AssertTable<FormRecord>(model, "records");
AssertTable<ReportDefinition>(model, "reports");
AssertTable<DashboardDefinition>(model, "dashboards");
AssertTable<DashboardRevision>(model, "dashboard_revisions");
AssertTable<TriggerDefinition>(model, "triggers");
AssertTable<TriggerExecutionLog>(model, "trigger_logs");
AssertTable<TriggerEventOutboxMessage>(model, "trigger_event_outbox");
AssertTable<WorkflowDefinition>(model, "workflow_definitions");
AssertTable<WorkflowDefinitionVersion>(model, "workflow_definition_versions");
AssertTable<WorkflowHistoryEntry>(model, "workflow_history");
AssertTable<WorkflowApprovalTask>(model, "workflow_approval_tasks");
AssertTable<PrintTemplate>(model, "print_templates");
AssertTable<PrintTemplateVersion>(model, "print_template_versions");
AssertTable<Notification>(model, "notifications");
AssertTable<NotificationPreference>(model, "notification_preferences");
AssertTable<IntegrationApiKey>(model, "integration_api_keys");
AssertTable<IntegrationConnector>(model, "integration_connectors");
AssertTable<IntegrationLogEntry>(model, "integration_logs");
AssertTable<IncomingWebhookListener>(model, "incoming_webhook_listeners");
AssertTable<RecordImportJob>(model, "record_import_jobs");
AssertTable<RecordImportJobRow>(model, "record_import_job_rows");
AssertTable<ExternalExportJob>(model, "external_export_jobs");
AssertTable<ProcessingJobDefinition>(model, "processing_job_definitions");
AssertTable<ProcessingJobRun>(model, "processing_job_runs");
AssertTable<ProcessingOperationalLog>(model, "processing_operational_logs");
AssertTable<AuditLogEntry>(model, "audit_logs");
AssertEqual(WorkspaceDefaults.WorkspaceId, dbContext.ActiveWorkspaceId, "The compatibility context should resolve the stable default workspace.");
AssertUniqueIndex<Tenant>(model, new[] { nameof(Tenant.Slug) }, "Tenant slugs should be globally unique.");
AssertUniqueIndex<Workspace>(model, new[] { nameof(Workspace.TenantId), nameof(Workspace.Slug) }, "Workspace slugs should be unique within a tenant.");
AssertUniqueIndex<Workspace>(model, new[] { nameof(Workspace.TenantId) }, "Each tenant should have at most one default workspace.");
AssertUniqueIndex<WorkspaceMembership>(model, new[] { nameof(WorkspaceMembership.WorkspaceId), nameof(WorkspaceMembership.UserId) }, "A user should have one membership per workspace.");
AssertIndex<WorkspaceMembership>(model, new[] { nameof(WorkspaceMembership.UserId), nameof(WorkspaceMembership.Status) }, "Membership lookup by user and status should be indexed.");
AssertConcurrencyStamp<WorkspaceMembership>(model);
AssertWorkspaceOwned<SsoProvider>(model);
AssertWorkspaceOwned<ExternalIdentity>(model);
AssertWorkspaceOwned<AccessPolicy>(model);
AssertWorkspaceOwned<RetentionPolicy>(model);
AssertWorkspaceOwned<LegalHold>(model);
AssertWorkspaceOwned<AdministrativeBackup>(model);
AssertWorkspaceOwned<RestorePlan>(model);
AssertConcurrencyStamp<SsoProvider>(model);
AssertConcurrencyStamp<AccessPolicy>(model);
AssertConcurrencyStamp<RetentionPolicy>(model);
AssertConcurrencyStamp<LegalHold>(model);
AssertConcurrencyStamp<AdministrativeBackup>(model);
AssertTrue(AdministrativeBackupScopes.Supported.SetEquals(new[] { "configuration_only", "full" }), "Administrative backup scopes should remain explicit and bounded.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Backup.Manage), "Backup administration should be an assignable platform permission.");
AssertWorkspaceOwned<Role>(model);
AssertWorkspaceOwned<UserRole>(model);
AssertWorkspaceOwned<UserGroup>(model);
AssertWorkspaceOwned<UserDepartment>(model);
AssertWorkspaceOwned<FormDefinition>(model);
AssertWorkspaceOwned<FormAutonumberSequence>(model);
AssertWorkspaceOwned<FileAttachment>(model);
AssertWorkspaceOwned<RecordRelationship>(model);
AssertUniqueIndex<RecordRelationship>(model, new[] { nameof(RecordRelationship.WorkspaceId), nameof(RecordRelationship.SourceRecordId), nameof(RecordRelationship.SourceFieldId) }, "Lookup relationships should have one canonical edge per source record and field.");
AssertUniqueIndex<FormAutonumberSequence>(model, new[] { nameof(FormAutonumberSequence.WorkspaceId), nameof(FormAutonumberSequence.FormId), nameof(FormAutonumberSequence.FieldId) }, "Autonumber allocation should have one counter per workspace, form, and field.");
AssertWorkspaceOwned<FormRecord>(model);
AssertWorkspaceOwned<DashboardDefinition>(model);
AssertWorkspaceOwned<DashboardRevision>(model);
AssertWorkspaceOwned<TriggerEventOutboxMessage>(model);
AssertWorkspaceOwned<IntegrationLogEntry>(model);
AssertWorkspaceOwned<ProcessingJobDefinition>(model);
AssertWorkspaceOwned<ProcessingJobRun>(model);
AssertWorkspaceOwned<ProcessingOperationalLog>(model);
AssertConcurrencyStamp<ProcessingJobDefinition>(model);
AssertConcurrencyStamp<ProcessingJobRun>(model);
AssertUniqueIndex<ProcessingJobRun>(model, new[] { nameof(ProcessingJobRun.DefinitionId) }, "Processing jobs should enforce one active run per definition.");
AssertWorkspaceOwned<AuditLogEntry>(model);
AssertJsonColumn<TriggerEventOutboxMessage>(model, nameof(TriggerEventOutboxMessage.PayloadJson));
AssertIndex<TriggerEventOutboxMessage>(model, new[] { nameof(TriggerEventOutboxMessage.Status), nameof(TriggerEventOutboxMessage.NextAttemptAt) }, "Trigger event outbox polling should use a status/due-time index.");
AssertIndex<TriggerEventOutboxMessage>(model, new[] { nameof(TriggerEventOutboxMessage.LockedAt) }, "Trigger event outbox abandoned claims should be indexed.");

using (var outboxDbContext = new OpenBusinessPlatformDbContext(dbOptions))
{
    var outbox = new TriggerEventOutbox(outboxDbContext);
    var formId = Guid.NewGuid();
    var recordId = Guid.NewGuid();
    var snapshot = new TriggerRecordSnapshot(
        recordId,
        formId,
        "active",
        null,
        null,
        null,
        null,
        new Dictionary<string, object?> { ["name"] = "Outbox test" });
    var message = outbox.Enqueue(new TriggerEventContext(
        TriggerEvents.RecordCreated,
        formId,
        recordId,
        null,
        null,
        snapshot,
        Array.Empty<string>(),
        null,
        "active",
        null,
        null,
        null,
        null,
        DateTimeOffset.UtcNow));

    AssertEqual(EntityState.Added, outboxDbContext.Entry(message).State, "Enqueue should stage the event on the caller's DbContext transaction.");
    AssertEqual(TriggerEventOutboxStatuses.Pending, message.Status, "New outbox events should start pending.");
    AssertEqual(5, message.MaxAttempts, "New outbox events should have a bounded delivery attempt count.");
    AssertTrue(message.PayloadJson.RootElement.GetProperty("recordId").GetGuid() == recordId, "Outbox payload should retain its record context.");
}

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
AssertTypeAssignable<CreationAuditedEntity<Guid>, DashboardRevision>();
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
AssertTypeAssignable<AuditedAggregateRoot<Guid>, IntegrationApiKey>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, IntegrationConnector>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, IntegrationLogEntry>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, IncomingWebhookListener>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, RecordImportJob>();
AssertTypeAssignable<Entity<Guid>, RecordImportJobRow>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, ExternalExportJob>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, ProcessingJobDefinition>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, ProcessingJobRun>();
AssertTypeAssignable<WorkspaceEntity<Guid>, ProcessingOperationalLog>();
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
AssertGuidId<DashboardRevision>(model);
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
AssertGuidId<IntegrationApiKey>(model);
AssertGuidId<IntegrationConnector>(model);
AssertGuidId<IntegrationLogEntry>(model);
AssertGuidId<IncomingWebhookListener>(model);
AssertGuidId<RecordImportJob>(model);
AssertGuidId<RecordImportJobRow>(model);
AssertGuidId<ExternalExportJob>(model);
AssertGuidId<ProcessingJobDefinition>(model);
AssertGuidId<ProcessingJobRun>(model);
AssertGuidId<ProcessingOperationalLog>(model);
AssertGuidId<AuditLogEntry>(model);
AssertGuidId<WorkspaceBranding>(model);
AssertGuidId<WorkspaceLocalization>(model);
AssertGuidId<UserLocalizationPreference>(model);
AssertGuidId<WorkspaceCustomDomain>(model);

AssertUniqueIndex<User>(model, new[] { nameof(User.Email) }, "Users should have a unique email index.");
AssertUniqueIndex<WorkspaceBranding>(model, new[] { nameof(WorkspaceBranding.WorkspaceId) }, "Branding should be unique per workspace.");
AssertUniqueIndex<WorkspaceLocalization>(model, new[] { nameof(WorkspaceLocalization.WorkspaceId) }, "Localization defaults should be unique per workspace.");
AssertUniqueIndex<UserLocalizationPreference>(model, new[] { nameof(UserLocalizationPreference.WorkspaceId), nameof(UserLocalizationPreference.UserId) }, "Localization preferences should be unique per workspace user.");
AssertUniqueIndex<WorkspaceCustomDomain>(model, new[] { nameof(WorkspaceCustomDomain.Hostname) }, "Custom domains should be globally unique.");
AssertUniqueIndex<Role>(model, new[] { nameof(Role.WorkspaceId), nameof(Role.Name) }, "Role names should be unique within a workspace.");
AssertUniqueIndex<RolePermission>(model, new[] { nameof(RolePermission.RoleId), nameof(RolePermission.Permission) }, "Role permissions should be unique per role/permission.");
AssertUniqueIndex<RoleFormPermission>(model, new[] { nameof(RoleFormPermission.RoleId), nameof(RoleFormPermission.FormId), nameof(RoleFormPermission.Action) }, "Role form permissions should be unique per role/form/action.");
AssertUniqueIndex<Group>(model, new[] { nameof(Group.WorkspaceId), nameof(Group.Name) }, "Group names should be unique within a workspace.");
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
AssertJsonColumn<DashboardDefinition>(model, nameof(DashboardDefinition.PublishedSnapshotJson));
AssertJsonColumn<DashboardRevision>(model, nameof(DashboardRevision.SnapshotJson));
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
AssertJsonColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.ScopesJson));
AssertJsonColumn<IntegrationConnector>(model, nameof(IntegrationConnector.ConfigJson));
AssertJsonColumn<IntegrationConnector>(model, nameof(IntegrationConnector.SecretMetadataJson));
AssertJsonColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.RequestMetadataJson));
AssertJsonColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.ResponseMetadataJson));
AssertJsonColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.MappingJson));
AssertJsonColumn<RecordImportJob>(model, nameof(RecordImportJob.MappingJson));
AssertJsonColumn<RecordImportJobRow>(model, nameof(RecordImportJobRow.ErrorsJson));
AssertJsonColumn<ExternalExportJob>(model, nameof(ExternalExportJob.RequestJson));
AssertJsonColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ArtifactMetadataJson));
AssertJsonColumn<ProcessingJobDefinition>(model, nameof(ProcessingJobDefinition.ConfigJson));
AssertJsonColumn<ProcessingJobDefinition>(model, nameof(ProcessingJobDefinition.ScheduleJson));
AssertJsonColumn<ProcessingJobDefinition>(model, nameof(ProcessingJobDefinition.RetryPolicyJson));
AssertJsonColumn<ProcessingJobDefinition>(model, nameof(ProcessingJobDefinition.FailureNotificationPolicyJson));
AssertJsonColumn<ProcessingJobRun>(model, nameof(ProcessingJobRun.ResultJson));
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
AssertJsonColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.ExtraPropertiesJson));
AssertJsonColumn<IntegrationConnector>(model, nameof(IntegrationConnector.ExtraPropertiesJson));
AssertJsonColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.ExtraPropertiesJson));
AssertJsonColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.ExtraPropertiesJson));
AssertJsonColumn<RecordImportJob>(model, nameof(RecordImportJob.ExtraPropertiesJson));
AssertJsonColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ExtraPropertiesJson));

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
AssertColumn<TriggerDefinition>(model, nameof(TriggerDefinition.ScheduleLockedAt), "schedule_locked_at", "Scheduled triggers should store their atomic claim lease.");
AssertConcurrencyStamp<FormRecord>(model);
AssertConcurrencyStamp<TriggerDefinition>(model);
AssertConcurrencyStamp<IntegrationConnector>(model);
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
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.Name), "name", "Integration API keys should store a display name.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.IntegrationKey), "integration_key", "Integration API keys should store a stable integration identity.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.KeyPrefix), "key_prefix", "Integration API keys should store a lookup/display prefix.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.KeyHash), "key_hash", "Integration API keys should store only the key hash.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.IsActive), "is_active", "Integration API keys should store active state.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.LastUsedAt), "last_used_at", "Integration API keys should track last use time.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.LastUsedIp), "last_used_ip", "Integration API keys should track last use IP metadata.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.LastUsedUserAgent), "last_used_user_agent", "Integration API keys should track last use user agent metadata.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.RevokedAt), "revoked_at", "Integration API keys should track revocation time.");
AssertColumn<IntegrationApiKey>(model, nameof(IntegrationApiKey.RevokedById), "revoked_by_id", "Integration API keys should track the revoking user.");
AssertColumn<IntegrationConnector>(model, nameof(IntegrationConnector.Name), "name", "Integration connectors should store a display name.");
AssertColumn<IntegrationConnector>(model, nameof(IntegrationConnector.ConnectorKey), "connector_key", "Integration connectors should store a stable connector key.");
AssertColumn<IntegrationConnector>(model, nameof(IntegrationConnector.Type), "type", "Integration connectors should store the connector type.");
AssertColumn<IntegrationConnector>(model, nameof(IntegrationConnector.ConfigJson), "config_json", "Integration connectors should store sanitized config JSON.");
AssertColumn<IntegrationConnector>(model, nameof(IntegrationConnector.SecretMetadataJson), "secret_metadata_json", "Integration connectors should store secret metadata without raw secrets.");
AssertColumn<IntegrationConnector>(model, nameof(IntegrationConnector.IsActive), "is_active", "Integration connectors should store active state.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.Direction), "direction", "Integration logs should store inbound/outbound direction.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.IntegrationType), "integration_type", "Integration logs should store integration type.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.IntegrationKey), "integration_key", "Integration logs should store stable integration identity.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.SourceType), "source_type", "Integration logs should store source type.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.SourceId), "source_id", "Integration logs should store optional source id.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.TargetEntityType), "target_entity_type", "Integration logs should store target entity type.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.TargetEntityId), "target_entity_id", "Integration logs should store target entity id.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.Status), "status", "Integration logs should store execution status.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.AttemptCount), "attempt_count", "Integration logs should store attempt count.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.MaxAttempts), "max_attempts", "Integration logs should store retry max attempts.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.IsRetryable), "is_retryable", "Integration logs should store whether failure can be retried.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.RetryNextAttemptAt), "retry_next_attempt_at", "Integration logs should store next retry metadata.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.RetryRequestedAt), "retry_requested_at", "Integration logs should store explicit retry request time.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.RetryRequestedById), "retry_requested_by_id", "Integration logs should store explicit retry requester.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.ErrorCode), "error_code", "Integration logs should store sanitized error code.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.ErrorMessage), "error_message", "Integration logs should store sanitized error message.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.StartedAt), "started_at", "Integration logs should store start time.");
AssertColumn<IntegrationLogEntry>(model, nameof(IntegrationLogEntry.CompletedAt), "completed_at", "Integration logs should store completion time.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.Name), "name", "Incoming webhook listeners should store a display name.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.ListenerKey), "listener_key", "Incoming webhook listeners should expose a stable route key.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.TargetFormId), "target_form_id", "Incoming webhook listeners should target one form.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.Action), "action", "Incoming webhook listeners should store create/upsert action.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.AuthMode), "auth_mode", "Incoming webhook listeners should store the authentication mode.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.SecretPrefix), "secret_prefix", "Incoming webhook listener secrets should store a lookup/display prefix.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.SecretHash), "secret_hash", "Incoming webhook listener secrets should store only a hash.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.SafeLookupFieldId), "safe_lookup_field_id", "Incoming webhook listener upserts should require an explicit safe lookup field.");
AssertColumn<IncomingWebhookListener>(model, nameof(IncomingWebhookListener.IsActive), "is_active", "Incoming webhook listeners should store active state.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.FormId), "form_id", "Record import jobs should target one form.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.IntegrationKey), "integration_key", "Record import jobs should store the integration identity.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.FileName), "file_name", "Record import jobs should store a safe file name.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.Status), "status", "Record import jobs should store status.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.TotalRows), "total_rows", "Record import jobs should store total row count.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.SucceededRows), "succeeded_rows", "Record import jobs should store succeeded row count.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.FailedRows), "failed_rows", "Record import jobs should store failed row count.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.StartedAt), "started_at", "Record import jobs should store start time.");
AssertColumn<RecordImportJob>(model, nameof(RecordImportJob.CompletedAt), "completed_at", "Record import jobs should store completion time.");
AssertColumn<RecordImportJobRow>(model, nameof(RecordImportJobRow.ImportJobId), "import_job_id", "Record import job rows should link to the import job.");
AssertColumn<RecordImportJobRow>(model, nameof(RecordImportJobRow.RowNumber), "row_number", "Record import job rows should store source row number.");
AssertColumn<RecordImportJobRow>(model, nameof(RecordImportJobRow.Status), "status", "Record import job rows should store row status.");
AssertColumn<RecordImportJobRow>(model, nameof(RecordImportJobRow.RecordId), "record_id", "Record import job rows should link successful records.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.SourceType), "source_type", "External export jobs should store source type.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.Format), "format", "External export jobs should store output format.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.IntegrationKey), "integration_key", "External export jobs should store integration identity.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.FormId), "form_id", "External export jobs should store optional form scope.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ReportId), "report_id", "External export jobs should store optional report scope.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.Status), "status", "External export jobs should store status.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.RowCount), "row_count", "External export jobs should store exported row count.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ArtifactFileName), "artifact_file_name", "External export jobs should store artifact file name.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ArtifactContentType), "artifact_content_type", "External export jobs should store artifact content type.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ArtifactSizeBytes), "artifact_size_bytes", "External export jobs should store artifact size.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.ArtifactContent), "artifact_content", "External export jobs should store protected artifact content.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.StartedAt), "started_at", "External export jobs should store start time.");
AssertColumn<ExternalExportJob>(model, nameof(ExternalExportJob.CompletedAt), "completed_at", "External export jobs should store completion time.");

AssertUniqueIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.TokenHash) }, "Password reset token hashes should be unique.");
AssertUniqueIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.KeyPrefix) }, "Integration API key prefixes should be unique for lookup.");
AssertUniqueIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.KeyHash) }, "Integration API key hashes should be unique.");
AssertUniqueIndex<IntegrationConnector>(model, new[] { nameof(IntegrationConnector.WorkspaceId), nameof(IntegrationConnector.ConnectorKey) }, "Integration connector keys should be unique within a workspace.");
AssertUniqueIndex<IncomingWebhookListener>(model, new[] { nameof(IncomingWebhookListener.ListenerKey) }, "Incoming webhook listener route keys should be unique.");
AssertUniqueIndex<IncomingWebhookListener>(model, new[] { nameof(IncomingWebhookListener.SecretPrefix) }, "Incoming webhook listener secret prefixes should be unique for lookup.");
AssertUniqueIndex<RecordImportJobRow>(model, new[] { nameof(RecordImportJobRow.ImportJobId), nameof(RecordImportJobRow.RowNumber) }, "Record import job rows should be unique per source row.");
AssertIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.IntegrationKey) }, "Integration API keys should be indexed by integration identity.");
AssertIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.IsActive) }, "Integration API keys should be indexed by active state.");
AssertIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.LastUsedAt) }, "Integration API keys should be indexed by last use time.");
AssertIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.RevokedAt) }, "Integration API keys should be indexed by revocation time.");
AssertIndex<IntegrationApiKey>(model, new[] { nameof(IntegrationApiKey.CreatedById) }, "Integration API keys should be indexed by creator.");
AssertIndex<IntegrationConnector>(model, new[] { nameof(IntegrationConnector.Type) }, "Integration connectors should be indexed by type.");
AssertIndex<IntegrationConnector>(model, new[] { nameof(IntegrationConnector.IsActive) }, "Integration connectors should be indexed by active state.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.IntegrationKey) }, "Integration logs should be indexed by integration identity.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.Direction) }, "Integration logs should be indexed by direction.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.IntegrationType) }, "Integration logs should be indexed by integration type.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.Status) }, "Integration logs should be indexed by status.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.SourceType), nameof(IntegrationLogEntry.SourceId) }, "Integration logs should be indexed by source.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.TargetEntityType), nameof(IntegrationLogEntry.TargetEntityId) }, "Integration logs should be indexed by target entity.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.RetryNextAttemptAt) }, "Integration logs should be indexed by retry due time.");
AssertIndex<IntegrationLogEntry>(model, new[] { nameof(IntegrationLogEntry.CreatedAt) }, "Integration logs should be indexed by creation time.");
AssertIndex<IncomingWebhookListener>(model, new[] { nameof(IncomingWebhookListener.TargetFormId) }, "Incoming webhook listeners should be indexed by target form.");
AssertIndex<IncomingWebhookListener>(model, new[] { nameof(IncomingWebhookListener.IsActive) }, "Incoming webhook listeners should be indexed by active state.");
AssertIndex<RecordImportJob>(model, new[] { nameof(RecordImportJob.FormId) }, "Record import jobs should be indexed by target form.");
AssertIndex<RecordImportJob>(model, new[] { nameof(RecordImportJob.Status) }, "Record import jobs should be indexed by status.");
AssertIndex<RecordImportJob>(model, new[] { nameof(RecordImportJob.CreatedAt) }, "Record import jobs should be indexed by creation time.");
AssertIndex<RecordImportJobRow>(model, new[] { nameof(RecordImportJobRow.ImportJobId) }, "Record import job rows should be indexed by import job.");
AssertIndex<RecordImportJobRow>(model, new[] { nameof(RecordImportJobRow.Status) }, "Record import job rows should be indexed by status.");
AssertIndex<ExternalExportJob>(model, new[] { nameof(ExternalExportJob.SourceType) }, "External export jobs should be indexed by source type.");
AssertIndex<ExternalExportJob>(model, new[] { nameof(ExternalExportJob.Status) }, "External export jobs should be indexed by status.");
AssertIndex<ExternalExportJob>(model, new[] { nameof(ExternalExportJob.FormId) }, "External export jobs should be indexed by form.");
AssertIndex<ExternalExportJob>(model, new[] { nameof(ExternalExportJob.ReportId) }, "External export jobs should be indexed by report.");
AssertIndex<ExternalExportJob>(model, new[] { nameof(ExternalExportJob.CreatedAt) }, "External export jobs should be indexed by creation time.");
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
AssertUniqueIndex<DashboardDefinition>(model, new[] { nameof(DashboardDefinition.WorkspaceId), nameof(DashboardDefinition.Slug) }, "Dashboard slugs should be unique within a workspace.");
AssertUniqueIndex<DashboardDefinition>(model, new[] { nameof(DashboardDefinition.WorkspaceId), nameof(DashboardDefinition.PublishedSlug) }, "Published dashboard slugs should be unique within a workspace.");
AssertUniqueIndex<DashboardRevision>(model, new[] { nameof(DashboardRevision.WorkspaceId), nameof(DashboardRevision.DashboardId), nameof(DashboardRevision.RevisionNumber) }, "Dashboard revision numbers should be unique per dashboard and workspace.");
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
AssertUniqueIndex<Notification>(model, new[] { nameof(Notification.WorkspaceId), nameof(Notification.UserId), nameof(Notification.DeduplicationKey) }, "Processing notifications should enforce recipient-scoped deduplication.");
AssertUniqueIndex<ProcessingOperationalLog>(model, new[] { nameof(ProcessingOperationalLog.WorkspaceId), nameof(ProcessingOperationalLog.EventKey) }, "Processing operational event keys should be workspace-unique.");
AssertIndex<ProcessingOperationalLog>(model, new[] { nameof(ProcessingOperationalLog.DefinitionId), nameof(ProcessingOperationalLog.OccurredAt) }, "Processing operational logs should support definition history queries.");
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

var apiKeyHasher = new IntegrationApiKeyHasher();
var apiKeyGenerator = new IntegrationApiKeyGenerator(apiKeyHasher);
var generatedApiKey = apiKeyGenerator.Generate();
AssertTrue(generatedApiKey.RawKey.StartsWith(IntegrationApiKeyGenerator.RawKeyPrefix, StringComparison.Ordinal), "Integration API keys should use a recognizable platform prefix.");
AssertTrue(generatedApiKey.RawKey.Contains('.', StringComparison.Ordinal), "Integration API keys should include a public prefix and private secret segment.");
AssertNotEqual(generatedApiKey.RawKey, generatedApiKey.KeyHash, "Integration API key hashes should not store the raw key.");
AssertEqual(generatedApiKey.KeyPrefix, IntegrationApiKeyGenerator.ExtractPrefix(generatedApiKey.RawKey), "Integration API keys should expose a stable lookup prefix.");
AssertTrue(apiKeyHasher.Verify(generatedApiKey.RawKey, generatedApiKey.KeyHash), "Integration API key hasher should verify the original raw key.");
AssertFalse(apiKeyHasher.Verify($"{generatedApiKey.RawKey}x", generatedApiKey.KeyHash), "Integration API key hasher should reject a different key.");
AssertTrue(IntegrationApiKeyScopes.Supported.Contains(IntegrationApiKeyScopes.Authenticate), "Integration API key scopes should include conservative authentication scope.");
AssertTrue(IntegrationApiKeyScopes.Supported.Contains(IntegrationApiKeyScopes.RecordsRead), "Integration API key scopes should include explicit record read scope.");
AssertTrue(IntegrationApiKeyScopes.Supported.Contains(IntegrationApiKeyScopes.RecordsCreate), "Integration API key scopes should include explicit record create scope.");
AssertTrue(IntegrationConnectorTypes.Supported.Contains(IntegrationConnectorTypes.Sftp), "Integration connectors should support SFTP configuration.");
AssertTrue(IntegrationConnectorTypes.Supported.Contains(IntegrationConnectorTypes.FileStorage), "Integration connectors should support file storage configuration.");
AssertTrue(IntegrationConnectorTypes.Supported.Contains(IntegrationConnectorTypes.VendorApi), "Integration connectors should support vendor API configuration.");
AssertFalse(
    IntegrationApiKeyAuthenticationPolicy.CanAuthenticate(new IntegrationApiKey { IsActive = false }),
    "Inactive integration API keys should not authenticate.");
AssertFalse(
    IntegrationApiKeyAuthenticationPolicy.CanAuthenticate(new IntegrationApiKey { IsActive = true, RevokedAt = DateTimeOffset.UtcNow }),
    "Revoked integration API keys should not authenticate.");
AssertTrue(
    IntegrationApiKeyAuthenticationPolicy.CanAuthenticate(new IntegrationApiKey { IsActive = true }),
    "Active non-revoked integration API keys should authenticate.");
AssertTrue(
    WorkspaceMembershipPolicy.CanTransition(WorkspaceMembershipStatuses.Invited, WorkspaceMembershipStatuses.Active),
    "Invited workspace members should be activatable.");
AssertFalse(
    WorkspaceMembershipPolicy.CanTransition(WorkspaceMembershipStatuses.Suspended, WorkspaceMembershipStatuses.Active),
    "Suspended workspace members should require a new invitation before activation.");
AssertEqual("/reports", SsoPolicy.NormalizeReturnPath("/reports"), "SSO should preserve local return paths.");
AssertEqual("/", SsoPolicy.NormalizeReturnPath("https://attacker.test"), "SSO should reject absolute return URLs.");
AssertEqual("/", SsoPolicy.NormalizeReturnPath("//attacker.test"), "SSO should reject scheme-relative return URLs.");
AssertTrue(SsoPolicy.CreateCodeChallenge("test-verifier").Length > 20, "SSO should derive a PKCE challenge.");
var policyDepartmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
var policyFormId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
var policySubject = new AccessPolicySubject(
    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
    new HashSet<string>(new[] { "Finance" }, StringComparer.Ordinal),
    new HashSet<string>(new[] { WorkspaceMembershipRoles.Member }, StringComparer.Ordinal),
    new HashSet<Guid>(new[] { policyDepartmentId }),
    new HashSet<Guid>(),
    false);
var matchingPolicyConditions = new AccessPolicyConditions(
    RoleAny: new[] { "Finance" },
    MembershipRoleAny: new[] { WorkspaceMembershipRoles.Member },
    DepartmentAny: new[] { policyDepartmentId },
    RecordStatusAny: new[] { "approved" },
    RecordOwnerIsCurrentUser: true);
AssertTrue(
    AccessPolicyEvaluator.Matches(
        matchingPolicyConditions,
        policySubject,
        new AccessPolicyResource(
            AccessPolicyResourceTypes.Record,
            policyFormId,
            PlatformPermissions.Form.Edit,
            "approved",
            policySubject.UserId)),
    "ABAC policy dimensions should combine with AND and values within a dimension with OR.");
AssertFalse(
    AccessPolicyEvaluator.Matches(
        matchingPolicyConditions,
        policySubject,
        new AccessPolicyResource(
            AccessPolicyResourceTypes.Record,
            policyFormId,
            PlatformPermissions.Form.Edit,
            "draft",
            policySubject.UserId)),
    "A non-matching record status should not trigger the deny policy.");
var policyFilteredSql = AccessPolicyEvaluator.ApplyRecordCondition(
    dbContext.Records,
    policySubject,
    matchingPolicyConditions).ToQueryString();
AssertTrue(
    policyFilteredSql.Contains("status", StringComparison.OrdinalIgnoreCase)
        && policyFilteredSql.Contains("created_by_id", StringComparison.OrdinalIgnoreCase),
    "Record ABAC guardrails should remain part of the database query.");
var linkedApiUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
var apiKeyPrincipal = IntegrationApiKeyPrincipalFactory.Create(
    new IntegrationApiKey
    {
        Id = Guid.Parse("dddddddd-1111-2222-3333-444444444444"),
        Name = "Payroll sync",
        IntegrationKey = "payroll-sync",
        WorkspaceId = WorkspaceDefaults.WorkspaceId,
        CreatedById = linkedApiUserId
    },
    new[] { IntegrationApiKeyScopes.Authenticate, IntegrationApiKeyScopes.RecordsRead });
AssertEqual(
    linkedApiUserId.ToString(),
    apiKeyPrincipal.FindFirstValue(IntegrationApiKeyClaims.CreatedByUserId),
    "API key principals should expose the linked created-by user for backend permission checks.");
AssertEqual(
    WorkspaceDefaults.WorkspaceId.ToString(),
    apiKeyPrincipal.FindFirstValue(WorkspaceClaims.WorkspaceId),
    "API key principals should carry their signed workspace boundary.");
AssertTrue(
    PublicRecordApiAccess.HasScope(apiKeyPrincipal, IntegrationApiKeyScopes.RecordsRead),
    "Public record API access should require explicit API key scopes.");
AssertFalse(
    PublicRecordApiAccess.HasScope(apiKeyPrincipal, IntegrationApiKeyScopes.RecordsCreate),
    "Public record API access should reject missing API key scopes.");
var effectiveRecordApiPrincipal = PublicRecordApiAccess.CreateEffectiveUserPrincipal(apiKeyPrincipal);
AssertEqual(
    linkedApiUserId.ToString(),
    effectiveRecordApiPrincipal.FindFirstValue(ClaimTypes.NameIdentifier),
    "Public record API access should evaluate records through the API key's linked user.");
AssertEqual("inbound", IntegrationLogDirections.Inbound, "Integration log directions should include inbound.");
AssertEqual("outbound", IntegrationLogDirections.Outbound, "Integration log directions should include outbound.");
AssertTrue(IntegrationLogDirections.Supported.Contains(IntegrationLogDirections.Inbound), "Integration log directions should be typed.");
AssertTrue(IntegrationLogTypes.Supported.Contains(IntegrationLogTypes.Webhook), "Integration log types should include webhook integrations.");
AssertTrue(IntegrationLogStatuses.Supported.Contains(IntegrationLogStatuses.Failed), "Integration log statuses should include failed.");
AssertTrue(IntegrationApiKeyScopes.Supported.Contains(IntegrationApiKeyScopes.WebhooksReceive), "Integration API key scopes should include explicit incoming webhook receive scope.");
AssertTrue(IncomingWebhookListenerAuthModes.Supported.Contains(IncomingWebhookListenerAuthModes.ApiKey), "Incoming webhook listeners should support API key authentication.");
AssertTrue(IncomingWebhookListenerAuthModes.Supported.Contains(IncomingWebhookListenerAuthModes.ListenerSecret), "Incoming webhook listeners should support listener secret authentication.");
AssertTrue(IncomingWebhookListenerActions.Supported.Contains(IncomingWebhookListenerActions.Create), "Incoming webhook listeners should support record creation.");
AssertTrue(IncomingWebhookListenerActions.Supported.Contains(IncomingWebhookListenerActions.Upsert), "Incoming webhook listeners should support conservative upsert.");
var listenerSecretHasher = new IncomingWebhookListenerSecretHasher();
var listenerSecretGenerator = new IncomingWebhookListenerSecretGenerator(listenerSecretHasher);
var generatedListenerSecret = listenerSecretGenerator.Generate();
AssertTrue(generatedListenerSecret.RawSecret.StartsWith(IncomingWebhookListenerSecretGenerator.RawSecretPrefix, StringComparison.Ordinal), "Incoming webhook listener secrets should use a recognizable platform prefix.");
AssertNotEqual(generatedListenerSecret.RawSecret, generatedListenerSecret.SecretHash, "Incoming webhook listener secret hashes should not store the raw secret.");
AssertTrue(listenerSecretHasher.Verify(generatedListenerSecret.RawSecret, generatedListenerSecret.SecretHash), "Incoming webhook listener secret hasher should verify the original raw secret.");
AssertFalse(listenerSecretHasher.Verify($"{generatedListenerSecret.RawSecret}x", generatedListenerSecret.SecretHash), "Incoming webhook listener secret hasher should reject a different secret.");
var validWebhookMapping = new IncomingWebhookMappingDefinition(new[]
{
    new IncomingWebhookFieldMappingDefinition("person.email", "email", Required: true),
    new IncomingWebhookFieldMappingDefinition("department", "department", Required: true)
});
var webhookTargetFormId = Guid.Parse("33333333-3333-3333-3333-333333333333");
var webhookTargetSchema = new FormSchemaDefinition(
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
AssertTrue(
    IncomingWebhookListenerValidator.Validate(
        new UpsertIncomingWebhookListenerRequest(
            "HR intake",
            "hr-intake",
            webhookTargetFormId,
            IncomingWebhookListenerActions.Create,
            IncomingWebhookListenerAuthModes.ApiKey,
            validWebhookMapping,
            IsActive: true),
        webhookTargetSchema).Valid,
    "Incoming webhook listener validation should accept mapped create listeners for one target form.");
AssertFalse(
    IncomingWebhookListenerValidator.Validate(
        new UpsertIncomingWebhookListenerRequest(
            "Bad mapping",
            "bad-mapping",
            webhookTargetFormId,
            IncomingWebhookListenerActions.Create,
            IncomingWebhookListenerAuthModes.ApiKey,
            new IncomingWebhookMappingDefinition(new[] { new IncomingWebhookFieldMappingDefinition("email", "missing") }),
            IsActive: true),
        webhookTargetSchema).Valid,
    "Incoming webhook listener validation should reject mappings to missing target fields.");
AssertFalse(
    IncomingWebhookListenerValidator.Validate(
        new UpsertIncomingWebhookListenerRequest(
            "Unsafe upsert",
            "unsafe-upsert",
            webhookTargetFormId,
            IncomingWebhookListenerActions.Upsert,
            IncomingWebhookListenerAuthModes.ApiKey,
            validWebhookMapping,
            IsActive: true),
        webhookTargetSchema).Valid,
    "Incoming webhook listener validation should reject upsert without an explicit safe lookup field.");
var mappedWebhookValues = IncomingWebhookPayloadMapper.MapValues(
    validWebhookMapping,
    new Dictionary<string, object?>
    {
        ["person"] = new Dictionary<string, object?> { ["email"] = "jane@example.test" },
        ["department"] = "HR",
        ["ignored"] = "not persisted"
    });
AssertEqual("jane@example.test", mappedWebhookValues["email"]?.ToString(), "Incoming webhook mapping should read nested payload paths.");
AssertEqual("HR", mappedWebhookValues["department"]?.ToString(), "Incoming webhook mapping should include only configured target values.");
AssertFalse(mappedWebhookValues.ContainsKey("ignored"), "Incoming webhook mapping should not persist unmapped payload fields.");
AssertNotNull(typeof(IncomingWebhookExecutionService).GetMethod(nameof(IncomingWebhookExecutionService.ReceiveAsync)), "Incoming webhook execution service should expose receive handling.");
AssertTrue(RecordImportJobStatuses.Supported.Contains(RecordImportJobStatuses.Pending), "Record import jobs should expose a pending status.");
AssertTrue(RecordImportJobStatuses.Supported.Contains(RecordImportJobStatuses.CompletedWithErrors), "Record import jobs should expose a completed_with_errors status.");
AssertTrue(RecordImportJobRowStatuses.Supported.Contains(RecordImportJobRowStatuses.Succeeded), "Record import job rows should expose a succeeded status.");
AssertTrue(RecordImportJobRowStatuses.Supported.Contains(RecordImportJobRowStatuses.Failed), "Record import job rows should expose a failed status.");
var importMapping = new RecordImportMappingDefinition(new[]
{
    new RecordImportFieldMappingDefinition("Email Address", "email"),
    new RecordImportFieldMappingDefinition("Department", "department")
});
var validImportRequest = new CreateRecordImportJobRequest(
    webhookTargetFormId,
    "hr-import",
    "employees.csv",
    "Email Address,Department\n\"jane@example.test\",HR\n\"sam@example.test\",\"Finance\"\n",
    importMapping);
var importRows = RecordImportCsvParser.Parse(validImportRequest.CsvContent);
AssertSequenceEqual(
    new[] { "Email Address", "Department" },
    importRows.Headers.ToArray(),
    "Record import CSV parser should expose headers in source order.");
AssertEqual("sam@example.test", importRows.Rows[1].Values["Email Address"], "Record import CSV parser should support quoted values.");
AssertTrue(
    RecordImportJobValidator.Validate(validImportRequest, webhookTargetSchema, importRows).Valid,
    "Record import validation should accept explicit CSV header to target field mappings.");
AssertTrue(
    RecordImportJobValidator.Validate(
        validImportRequest with { Mapping = strictImportMapping },
        webhookTargetSchema,
        importRows).Errors.Any(error => error.Code == "record_import.mapping_properties"),
    "Direct record imports should reject unknown nested mapping properties.");
AssertFalse(
    RecordImportJobValidator.Validate(
        validImportRequest with { Mapping = new RecordImportMappingDefinition(new[] { new RecordImportFieldMappingDefinition("Missing", "email") }) },
        webhookTargetSchema,
        importRows).Valid,
    "Record import validation should reject mappings from missing CSV headers.");
AssertFalse(
    RecordImportJobValidator.Validate(
        validImportRequest with { Mapping = new RecordImportMappingDefinition(new[] { new RecordImportFieldMappingDefinition("Email Address", "missing") }) },
        webhookTargetSchema,
        importRows).Valid,
    "Record import validation should reject mappings to missing target fields.");
AssertNotNull(typeof(RecordImportJobService).GetMethod(nameof(RecordImportJobService.CreateAsync)), "Record import job service should expose create handling.");
AssertNotNull(typeof(RecordImportJobService).GetMethod(nameof(RecordImportJobService.GetAsync)), "Record import job service should expose query handling.");
AssertTrue(ExternalExportJobSourceTypes.Supported.Contains(ExternalExportJobSourceTypes.FormRecords), "External export jobs should support form record sources.");
AssertTrue(ExternalExportJobSourceTypes.Supported.Contains(ExternalExportJobSourceTypes.ListReport), "External export jobs should support list report sources.");
AssertTrue(ExternalExportJobFormats.Supported.Contains(ExternalExportJobFormats.Csv), "External export jobs should support CSV output.");
AssertTrue(ExternalExportJobFormats.Supported.Contains(ExternalExportJobFormats.Json), "External export jobs should support JSON output.");
AssertTrue(ExternalExportJobStatuses.Supported.Contains(ExternalExportJobStatuses.Succeeded), "External export jobs should expose succeeded status.");
AssertNull(typeof(ExternalExportJobDetailDto).GetProperty("ArtifactContent"), "External export job details should not bypass the audited artifact download endpoint.");
var exportReport = new ListReportExecutionDto(
    Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
    webhookTargetFormId,
    "Employee export",
    "Employees",
    1,
    2,
    1,
    new[]
    {
        new ListReportExecutionColumnDto("email", "Email", FormFieldTypes.Email, "field", null)
    },
    new[]
    {
        new ListReportExecutionRowDto(
            Guid.Parse("eeeeeeee-0000-0000-0000-000000000002"),
            "active",
            new Dictionary<string, ListReportExecutionCellDto>
            {
                ["email"] = new("jane@example.test", "jane@example.test"),
                ["hidden_salary"] = new("90000", "90000")
            },
            DateTimeOffset.Parse("2026-06-10T13:45:00Z"))
    });
var csvArtifact = ExternalExportArtifactBuilder.Build(ExternalExportJobFormats.Csv, exportReport);
AssertEqual("text/csv; charset=utf-8", csvArtifact.ContentType, "External CSV export artifacts should expose a CSV content type.");
AssertTrue(csvArtifact.Content.Contains("jane@example.test", StringComparison.Ordinal), "External CSV export artifacts should include visible values.");
AssertFalse(csvArtifact.Content.Contains("90000", StringComparison.Ordinal), "External CSV export artifacts should exclude cells without visible columns.");
var jsonArtifact = ExternalExportArtifactBuilder.Build(ExternalExportJobFormats.Json, exportReport);
AssertEqual("application/json; charset=utf-8", jsonArtifact.ContentType, "External JSON export artifacts should expose a JSON content type.");
AssertTrue(jsonArtifact.Content.Contains("\"email\"", StringComparison.Ordinal), "External JSON export artifacts should include visible fields.");
AssertFalse(jsonArtifact.Content.Contains("hidden_salary", StringComparison.Ordinal), "External JSON export artifacts should exclude cells without visible columns.");
AssertNotNull(typeof(ExternalExportJobService).GetMethod(nameof(ExternalExportJobService.CreateAsync)), "External export job service should expose create handling.");
AssertNotNull(typeof(ExternalExportJobService).GetMethod(nameof(ExternalExportJobService.GetAsync)), "External export job service should expose query handling.");
AssertNotNull(typeof(ExternalExportJobService).GetMethod(nameof(ExternalExportJobService.GetArtifactAsync)), "External export job service should expose protected artifact downloads.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Integrations", "IntegrationsEndpoints.cs"))
        .Contains("/{exportJobId:guid}/artifact", StringComparison.Ordinal),
    "Integration endpoints should expose protected export artifact downloads.");
var sensitiveMetadata = new Dictionary<string, object?>
{
    ["Authorization"] = "Bearer secret",
    ["x-obp-api-key"] = generatedApiKey.RawKey,
    ["contentType"] = "application/json",
    ["nested"] = new Dictionary<string, object?>
    {
        ["password"] = "do-not-store",
        ["safe"] = "visible"
    }
};
var sanitizedMetadata = IntegrationMetadataSanitizer.Sanitize(sensitiveMetadata);
AssertNotNull(sanitizedMetadata, "Integration log metadata sanitizer should return sanitized metadata.");
AssertEqual(IntegrationMetadataSanitizer.RedactedValue, sanitizedMetadata!["Authorization"]?.ToString(), "Integration log metadata should redact authorization headers.");
AssertEqual(IntegrationMetadataSanitizer.RedactedValue, sanitizedMetadata["x-obp-api-key"]?.ToString(), "Integration log metadata should redact API key headers.");
AssertTrue(
    sanitizedMetadata["nested"] is IReadOnlyDictionary<string, object?> nestedMetadata
        && nestedMetadata["password"]?.ToString() == IntegrationMetadataSanitizer.RedactedValue
        && nestedMetadata["safe"]?.ToString() == "visible",
    "Integration log metadata redaction should recurse into nested metadata.");
var integrationRetryPolicy = IntegrationRetryPolicy.Default;
var integrationRetryNow = DateTimeOffset.Parse("2026-06-09T12:00:00Z");
var retryableIntegrationLog = new IntegrationLogEntry
{
    Status = IntegrationLogStatuses.Failed,
    IsRetryable = true
};
IntegrationRetryScheduler.ScheduleRetry(retryableIntegrationLog, integrationRetryPolicy, integrationRetryNow);
AssertEqual(0, retryableIntegrationLog.AttemptCount, "Initial integration retry scheduling should not consume an attempt.");
AssertEqual(integrationRetryPolicy.MaxAttempts, retryableIntegrationLog.MaxAttempts, "Initial integration retry scheduling should store max attempts.");
AssertEqual(integrationRetryNow.Add(integrationRetryPolicy.Delay), retryableIntegrationLog.RetryNextAttemptAt, "Initial integration retry scheduling should store next attempt time.");
AssertEqual(IntegrationRetryStates.Pending, IntegrationRetryStateResolver.Resolve(retryableIntegrationLog), "Retryable failed integration logs with next-attempt metadata should resolve pending state.");
IntegrationRetryScheduler.MarkAttemptFailed(retryableIntegrationLog, integrationRetryPolicy, integrationRetryNow.AddMinutes(1));
AssertEqual(1, retryableIntegrationLog.AttemptCount, "Failed integration retry attempts should increment attempt count.");
retryableIntegrationLog.AttemptCount = integrationRetryPolicy.MaxAttempts - 1;
IntegrationRetryScheduler.MarkAttemptFailed(retryableIntegrationLog, integrationRetryPolicy, integrationRetryNow.AddMinutes(2));
AssertNotNull(retryableIntegrationLog.RetryExhaustedAt, "Integration retries should mark exhaustion after the final attempt.");
AssertEqual(IntegrationRetryStates.Exhausted, IntegrationRetryStateResolver.Resolve(retryableIntegrationLog), "Exhausted integration logs should resolve exhausted state.");

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
var webhookTriggerId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
var webhookEventTime = DateTimeOffset.Parse("2026-07-15T18:30:00Z");
var webhookSnapshot = new TriggerRecordSnapshot(
    Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"),
    sourceTriggerFormId,
    "active",
    null,
    null,
    null,
    null,
    new Dictionary<string, object?>());
var webhookContext = new TriggerEventContext(
    TriggerEvents.RecordCreated,
    sourceTriggerFormId,
    webhookSnapshot.RecordId,
    null,
    null,
    webhookSnapshot,
    Array.Empty<string>(),
    null,
    "active",
    null,
    null,
    null,
    null,
    webhookEventTime);
var webhookIdempotencyKey = TriggerWebhookIdempotency.CreateKey(webhookTriggerId, "webhook-1", webhookContext);
AssertEqual(webhookIdempotencyKey, TriggerWebhookIdempotency.CreateKey(webhookTriggerId, "webhook-1", webhookContext), "Webhook retries should reuse the same deterministic idempotency key.");
AssertNotEqual(webhookIdempotencyKey, TriggerWebhookIdempotency.CreateKey(webhookTriggerId, "webhook-2", webhookContext), "Different webhook actions should receive different idempotency keys.");
AssertNotEqual(webhookIdempotencyKey, TriggerWebhookIdempotency.CreateKey(webhookTriggerId, "webhook-1", webhookContext with { OccurredAt = webhookEventTime.AddSeconds(1) }), "Different event occurrences should receive different idempotency keys.");
AssertTrue(webhookIdempotencyKey.StartsWith("obp_trigger_", StringComparison.Ordinal), "Webhook idempotency keys should be recognizable opaque platform keys.");
AssertTrue(TriggerWebhookIdempotency.IsReservedHeader("idempotency-key"), "Webhook idempotency header ownership should be case-insensitive.");
using (var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "https://hooks.example.test/records"))
{
    TriggerWebhookIdempotency.ApplyHeaders(
        webhookRequest,
        new Dictionary<string, string>
        {
            ["X-Source"] = "open-business-platform",
            ["idempotency-key"] = "user-value"
        },
        webhookIdempotencyKey);
    AssertSequenceEqual(new[] { webhookIdempotencyKey }, webhookRequest.Headers.GetValues(TriggerWebhookIdempotency.HeaderName).ToArray(), "Outbound webhooks should contain exactly one platform-generated idempotency key.");
    AssertSequenceEqual(new[] { "open-business-platform" }, webhookRequest.Headers.GetValues("X-Source").ToArray(), "Outbound webhook idempotency should preserve non-reserved custom headers.");
}
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
var validScheduledWorkflowStartActions = new[]
{
    new TriggerActionDefinition(
        "scheduled-workflow-1",
        TriggerActionTypes.ScheduledStartWorkflow,
        WorkflowDefinitionId: workflowStartDefinitionId,
        RecordSelection: new TriggerScheduledWorkflowRecordSelectionDefinition(
            TriggerScheduledWorkflowRecordSelectionModes.StatusEquals,
            Status: "submitted",
            MaxRecords: 50))
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
var everyOtherDaySchedule = new TriggerScheduleDefinition(
    TriggerScheduleKinds.Daily,
    "Etc/UTC",
    DateTimeOffset.Parse("2026-06-04T12:00:00Z"),
    Interval: 2);
var everyOtherMondaySchedule = new TriggerScheduleDefinition(
    TriggerScheduleKinds.Weekly,
    "Etc/UTC",
    DateTimeOffset.Parse("2026-06-01T09:30:00Z"),
    Interval: 2,
    DayOfWeek: 1);
var monthlyLastDayCandidateSchedule = new TriggerScheduleDefinition(
    TriggerScheduleKinds.Monthly,
    "Etc/UTC",
    DateTimeOffset.Parse("2026-01-31T08:00:00Z"),
    Interval: 1,
    DayOfMonth: 31);
AssertTrue(TriggerEvents.Supported.Contains(TriggerEvents.RecordCreated), "Trigger events should include record.created.");
AssertTrue(TriggerEvents.Supported.Contains(TriggerEvents.ScheduleDaily), "Trigger events should include schedule.daily.");
AssertEqual(2, everyOtherDaySchedule.Interval, "Daily schedule contracts should expose a typed interval.");
AssertEqual(1, everyOtherMondaySchedule.DayOfWeek, "Weekly schedule contracts should expose a typed day of week.");
AssertEqual(31, monthlyLastDayCandidateSchedule.DayOfMonth, "Monthly schedule contracts should expose a typed day of month.");
AssertEqual(
    DateTimeOffset.Parse("2026-06-08T12:00:00Z"),
    TriggerScheduleCalculator.CalculateNextRun(everyOtherDaySchedule, DateTimeOffset.Parse("2026-06-07T12:00:00Z")),
    "Daily schedule calculation should honor explicit intervals.");
AssertEqual(
    DateTimeOffset.Parse("2026-06-15T09:30:00Z"),
    TriggerScheduleCalculator.CalculateNextRun(everyOtherMondaySchedule, DateTimeOffset.Parse("2026-06-08T09:30:00Z")),
    "Weekly schedule calculation should honor explicit intervals and day-of-week contracts.");
AssertEqual(
    DateTimeOffset.Parse("2026-02-28T08:00:00Z"),
    TriggerScheduleCalculator.CalculateNextRun(monthlyLastDayCandidateSchedule, DateTimeOffset.Parse("2026-02-01T00:00:00Z")),
    "Monthly schedule calculation should clamp explicit day-of-month contracts to shorter months.");
AssertEqual("manual", TriggerScheduleRunSources.Manual, "Scheduled trigger run metadata should expose manual run sources.");
var manualScheduleMetadata = new TriggerScheduledRunMetadata(
    DateTimeOffset.Parse("2026-06-10T12:00:00Z"),
    DateTimeOffset.Parse("2026-06-10T12:00:00Z"),
    RunSource: TriggerScheduleRunSources.Manual);
AssertEqual(
    TriggerScheduleRunSources.Manual,
    manualScheduleMetadata.RunSource,
    "Manually run scheduled trigger logs should identify their run source.");
AssertEqual(TimeSpan.FromMinutes(5), TriggerScheduleService.ClaimLease, "Scheduled trigger claims should expire after a bounded lease.");
AssertEqual(TimeSpan.FromMinutes(5), TriggerEventOutboxProcessor.ClaimLease, "Trigger event outbox claims should expire after a bounded lease.");
AssertEqual(TimeSpan.FromSeconds(30), TriggerEventOutboxProcessor.CalculateRetryDelay(1), "First outbox delivery retry should wait 30 seconds.");
AssertEqual(TimeSpan.FromMinutes(1), TriggerEventOutboxProcessor.CalculateRetryDelay(2), "Second outbox delivery retry should use exponential backoff.");
AssertEqual(TimeSpan.FromMinutes(15), TriggerEventOutboxProcessor.CalculateRetryDelay(10), "Outbox delivery retry backoff should be bounded at 15 minutes.");
AssertTrue(TriggerEventOutboxStatuses.Supported.SetEquals(new[] { "pending", "processing", "completed", "dead_letter" }), "Outbox operations should accept only known delivery states.");
AssertNull(typeof(TriggerEventOutboxMessageDto).GetProperty("PayloadJson"), "Outbox operations DTOs should never expose event payload JSON.");
AssertEqual("healthy", TriggerEventOutboxOperationsService.GetHealthStatus(0, null, DateTimeOffset.UtcNow), "An empty outbox should report healthy.");
var outboxHealthNow = DateTimeOffset.UtcNow;
AssertEqual("delayed", TriggerEventOutboxOperationsService.GetHealthStatus(0, outboxHealthNow.AddMinutes(-6), outboxHealthNow), "Old pending messages should report delayed delivery.");
AssertEqual("attention", TriggerEventOutboxOperationsService.GetHealthStatus(1, null, outboxHealthNow), "Dead letters should require operator attention.");
AssertEqual(TimeSpan.FromDays(30), TriggerEventOutboxRetentionService.RetentionPeriod, "Completed outbox messages should have a documented retention period.");
AssertEqual(500, TriggerEventOutboxRetentionService.BatchSize, "Outbox retention should delete in bounded batches.");
var outboxOperationsSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Triggers", "TriggerEventOutboxOperationsService.cs"));
AssertTrue(outboxOperationsSource.Contains("message.Status == TriggerEventOutboxStatuses.DeadLetter", StringComparison.Ordinal), "Replay should atomically require dead-letter state.");
AssertTrue(outboxOperationsSource.Contains("trigger_event_outbox_replayed", StringComparison.Ordinal), "Manual outbox replay should write a dedicated audit action.");
var outboxRetentionSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Triggers", "TriggerEventOutboxRetentionService.cs"));
AssertTrue(outboxRetentionSource.Contains("message.Status == TriggerEventOutboxStatuses.Completed", StringComparison.Ordinal), "Retention should delete only completed messages.");
var triggerEndpointsSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Triggers", "TriggersEndpoints.cs"));
AssertTrue(triggerEndpointsSource.Contains("/outbox/{messageId:guid}/replay", StringComparison.Ordinal), "Trigger endpoints should expose protected dead-letter replay.");
var automationOptions = new AutomationHealthOptions
{
    PendingAgeWarningSeconds = 300,
    DeadLetterWarningCount = 1,
    MonitorIntervalSeconds = 300,
    MetricsEnabled = true,
    MetricsToken = "metrics-secret"
};
AssertEqual(30, new AutomationHealthOptions { PendingAgeWarningSeconds = 1 }.Normalize().PendingAgeWarningSeconds, "Automation pending-age warnings should have a safe lower bound.");
AssertEqual("healthy", AutomationOutboxSnapshotService.GetStatus(0, 299, automationOptions), "Automation delivery should remain healthy below configured thresholds.");
AssertEqual("degraded", AutomationOutboxSnapshotService.GetStatus(0, 300, automationOptions), "Old pending delivery should degrade automation health at the configured threshold.");
AssertEqual("degraded", AutomationOutboxSnapshotService.GetStatus(1, 0, automationOptions), "Dead letters should degrade automation health at the configured threshold.");
AssertFalse(AutomationMetrics.IsAccessAllowed(false, null, automationOptions), "Production metrics should reject missing credentials.");
AssertFalse(AutomationMetrics.IsAccessAllowed(false, "Bearer wrong", automationOptions), "Production metrics should reject invalid credentials.");
AssertTrue(AutomationMetrics.IsAccessAllowed(false, "Bearer metrics-secret", automationOptions), "Production metrics should accept the configured bearer token.");
AssertTrue(AutomationMetrics.IsAccessAllowed(true, null, new AutomationHealthOptions()), "Development metrics may run without a configured token.");
AssertFalse(AutomationMetrics.IsAccessAllowed(false, null, new AutomationHealthOptions()), "Production metrics should remain closed when no token is configured.");
AssertFalse(AutomationMetrics.IsAccessAllowed(true, null, new AutomationHealthOptions { MetricsEnabled = false }), "Disabled metrics should reject access in every environment.");
var metricsMessageId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
var metricsFormId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
var automationSnapshot = new AutomationOutboxSnapshot(
    "degraded",
    PendingCount: 2,
    ProcessingCount: 1,
    CompletedCount: 10,
    DeadLetterCount: 1,
    RetryBacklogCount: 1,
    OldestPendingAgeSeconds: 420,
    OldestPendingMessageId: metricsMessageId,
    OldestPendingFormId: metricsFormId,
    OldestPendingAttemptCount: 2,
    DeadLetters: new[] { new AutomationAttentionMessage(metricsMessageId, metricsFormId, TriggerEvents.RecordCreated, 5, DateTimeOffset.UtcNow) },
    ObservedAt: DateTimeOffset.UtcNow);
var automationMetrics = AutomationMetrics.Format(automationSnapshot, automationOptions);
AssertTrue(automationMetrics.Contains("obp_trigger_outbox_messages{status=\"dead_letter\"} 1", StringComparison.Ordinal), "Automation metrics should expose bounded aggregate delivery counts.");
AssertTrue(automationMetrics.Contains("obp_trigger_outbox_oldest_pending_age_seconds 420", StringComparison.Ordinal), "Automation metrics should expose oldest pending age.");
AssertFalse(automationMetrics.Contains(metricsMessageId.ToString(), StringComparison.OrdinalIgnoreCase), "Automation metrics should not label message identifiers.");
AssertFalse(automationMetrics.Contains(metricsFormId.ToString(), StringComparison.OrdinalIgnoreCase), "Automation metrics should not label form identifiers.");
AssertFalse(automationMetrics.Contains("payload", StringComparison.OrdinalIgnoreCase), "Automation metrics should never include event payload metadata.");
var automationMonitorSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Triggers", "AutomationOutboxMonitorWorker.cs"));
AssertTrue(automationMonitorSource.Contains("FormId {FormId}, MessageId {MessageId}", StringComparison.Ordinal), "Automation warning logs should identify the affected form and message.");
AssertFalse(automationMonitorSource.Contains("PayloadJson", StringComparison.Ordinal), "Automation warning logs should never access outbox payload JSON.");
var automationHealthCheckSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Triggers", "AutomationOutboxHealthCheck.cs"));
AssertTrue(automationHealthCheckSource.Contains("HealthCheckResult.Unhealthy", StringComparison.Ordinal), "Automation health should become unhealthy when its database query fails.");
using var partialTriggerResult = SerializeHarnessJson(new
{
    actions = new object[]
    {
        new { actionId = "email", type = TriggerActionTypes.SendEmail },
        new { actionId = "webhook", type = TriggerActionTypes.CallWebhook, status = "failed", errorMessage = "timeout" }
    }
});
var completedBeforeRetry = TriggerActionResumePolicy.GetCompletedActionIds(partialTriggerResult);
AssertTrue(completedBeforeRetry.SetEquals(new[] { "email" }), "Trigger retry resumption should skip only actions recorded as completed.");
using var retryTriggerResult = SerializeHarnessJson(new
{
    actions = new object[]
    {
        new { actionId = "email", type = TriggerActionTypes.SendEmail, executionStatus = "skipped" },
        new { actionId = "webhook", type = TriggerActionTypes.CallWebhook }
    }
});
using var mergedTriggerResult = TriggerActionResumePolicy.MergeCompletedActions(partialTriggerResult, retryTriggerResult);
AssertTrue(
    TriggerActionResumePolicy.GetCompletedActionIds(mergedTriggerResult).SetEquals(new[] { "email", "webhook" }),
    "Trigger retries should retain completed action checkpoints across attempts.");
AssertTrue(TriggerConditionTypes.Supported.Contains(TriggerConditionTypes.FieldChanged), "Trigger conditions should include field_changed.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.AssignRecord), "Trigger actions should include assign_record.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.UpdateField), "Trigger actions should include update_field.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.SendNotification), "Trigger actions should include send_notification.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.CreateRecord), "Trigger actions should include create_record.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.CallWebhook), "Trigger actions should include call_webhook.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.StartWorkflow), "Trigger actions should include start_workflow.");
AssertTrue(TriggerActionTypes.Supported.Contains(TriggerActionTypes.ScheduledStartWorkflow), "Trigger actions should include scheduled_start_workflow.");
AssertFalse(TriggerActionTypes.ScheduledSupported.Contains(TriggerActionTypes.StartWorkflow), "Scheduled triggers should not support record-context workflow starts.");
AssertTrue(TriggerActionTypes.ScheduledSupported.Contains(TriggerActionTypes.ScheduledStartWorkflow), "Scheduled triggers should support explicit scheduled workflow starts.");
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
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Start scheduled workflows", null, TriggerEvents.ScheduleDaily, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validScheduledWorkflowStartActions, true, validTriggerRetryPolicy, validDailySchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        validWorkflowStartTargets,
        sourceTriggerFormId).Valid,
    "A scheduled workflow start with explicit record selection and an eligible workflow should pass validation.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Ambiguous scheduled workflow", null, TriggerEvents.ScheduleDaily, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), new[] { validScheduledWorkflowStartActions[0] with { RecordSelection = null } }, true, validTriggerRetryPolicy, validDailySchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        validWorkflowStartTargets,
        sourceTriggerFormId).Valid,
    "Scheduled workflow starts should require explicit record selection.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Wrong event scheduled workflow", null, TriggerEvents.RecordCreated, validTriggerConditions, validScheduledWorkflowStartActions, true, validTriggerRetryPolicy, null),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<TriggerTargetFormSchema>(),
        validWorkflowStartTargets,
        sourceTriggerFormId).Valid,
    "Scheduled workflow start actions should be rejected on record-context trigger events.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Weekly digest", null, TriggerEvents.ScheduleWeekly, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validWebhookActions, true, validTriggerRetryPolicy, everyOtherMondaySchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "A scheduled weekly trigger with explicit day-of-week metadata should pass validation.");
AssertTrue(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Monthly digest", null, TriggerEvents.ScheduleMonthly, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validWebhookActions, true, validTriggerRetryPolicy, monthlyLastDayCandidateSchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "A scheduled monthly trigger with explicit day-of-month metadata should pass validation.");
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
        new CreateTriggerRequest("Bad weekly schedule", null, TriggerEvents.ScheduleWeekly, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validWebhookActions, true, validTriggerRetryPolicy, everyOtherMondaySchedule with { DayOfWeek = 7 }),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Scheduled weekly triggers should reject out-of-range day-of-week metadata.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Bad monthly schedule", null, TriggerEvents.ScheduleMonthly, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validWebhookActions, true, validTriggerRetryPolicy, monthlyLastDayCandidateSchedule with { DayOfMonth = 0 }),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Scheduled monthly triggers should reject out-of-range day-of-month metadata.");
AssertFalse(
    TriggerDefinitionValidator.Validate(
        demoSchema,
        new CreateTriggerRequest("Scheduled record create", null, TriggerEvents.ScheduleDaily, new TriggerConditionGroupDefinition(TriggerConditionModes.All, Array.Empty<TriggerConditionDefinition>()), validCreateRecordActions, true, validTriggerRetryPolicy, validDailySchedule),
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        new[] { new TriggerTargetFormSchema(targetFormId, targetFormVersionId, createRecordTargetSchema) }).Valid,
    "Scheduled triggers should reject record-context create_record actions.");
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
        new CreateTriggerRequest(
            "Reserved webhook header",
            null,
            TriggerEvents.RecordCreated,
            validTriggerConditions,
            new[]
            {
                new TriggerActionDefinition(
                    "webhook-1",
                    TriggerActionTypes.CallWebhook,
                    WebhookUrl: "https://hooks.example.test/records",
                    WebhookHeaders: new Dictionary<string, string> { ["idempotency-key"] = "user-value" })
            },
            true),
        Array.Empty<Guid>(),
        Array.Empty<Guid>()).Valid,
    "Validation should reject attempts to override the platform webhook idempotency key.");
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
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Integrations.Manage), "Built-in permissions should include integration API key management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Branding.Manage), "Built-in permissions should include workspace branding management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Localization.Manage), "Built-in permissions should include workspace localization management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Domains.Manage), "Built-in permissions should include custom-domain management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Compliance.Manage), "Built-in permissions should include compliance administration.");
AssertNotNull(typeof(ComplianceService).GetMethod(nameof(ComplianceService.GetPostureAsync)), "Compliance service should expose operational posture evidence.");
AssertNotNull(typeof(ComplianceService).GetMethod(nameof(ComplianceService.SearchAuditAsync)), "Compliance service should expose bounded audit search.");
AssertNotNull(typeof(ComplianceService).GetMethod(nameof(ComplianceService.ExportAuditAsync)), "Compliance service should expose audited CSV export.");
var defaultWorkspaceBranding = WorkspaceBrandingService.Resolve(null, "Acme Operations");
AssertEqual("Acme Operations", defaultWorkspaceBranding.AppName, "Branding defaults should use the workspace name.");
AssertEqual(WorkspaceBrandingService.DefaultLogoText, defaultWorkspaceBranding.LogoText, "Branding defaults should use safe deployment logo text.");
var defaultLocalization = LocalizationService.Resolve(null, null);
AssertEqual(LocalizationService.FallbackLocale, defaultLocalization.EffectiveLocale, "Localization should resolve the platform locale fallback.");
var localizedWorkspace = new WorkspaceLocalization { DefaultLocale = "fr-CA", DefaultTimeZone = "America/Toronto", FirstDayOfWeek = 1 };
var localizedUser = new UserLocalizationPreference { Locale = "en-CA", TimeZone = null };
var effectiveLocalization = LocalizationService.Resolve(localizedWorkspace, localizedUser);
AssertEqual("en-CA", effectiveLocalization.EffectiveLocale, "User locale should override the workspace locale.");
AssertEqual("America/Toronto", effectiveLocalization.EffectiveTimeZone, "Missing user timezone should inherit the workspace timezone.");
AssertEqual("xn--bcher-kva.example", CustomDomainService.NormalizeHostname("Bücher.Example."), "Custom domains should normalize case, trailing dots, and IDNs.");
AssertThrows<CustomDomainException>(() => CustomDomainService.NormalizeHostname("https://example.com/path"), "Custom domains should reject URLs and paths.");
AssertEqual("obp-verification=test", CloudflareDnsTxtResolver.NormalizeTxt("\"obp-verification=\" \"test\""), "DNS TXT chunks should normalize before verification.");
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
var permissionService = new PermissionService(dbContext, new AccessPolicyEvaluator(dbContext));
AssertTrue(await permissionService.CanAsync(bootstrapPrincipal, PlatformPermissions.Users.Manage, CancellationToken.None), "Bootstrap admin should have user management permission.");
AssertTrue(await permissionService.CanAccessFormAsync(bootstrapPrincipal, Guid.NewGuid(), PlatformPermissions.Form.Manage, CancellationToken.None), "Bootstrap admin should have form management permission.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.GetAllowedRecordScopesAsync)), "PermissionService should expose record scope resolution.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.ApplyRecordAccessAsync)), "PermissionService should expose record query filtering.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.CanAccessRecordAsync)), "PermissionService should expose record access checks.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.GetFieldAccessAsync)), "PermissionService should expose field access checks.");
AssertNotNull(typeof(PermissionService).GetMethod(nameof(PermissionService.CanAccessReportAsync)), "PermissionService should expose report access checks.");
AssertNotNull(typeof(FormManagementService).GetMethod(nameof(FormManagementService.ListAccessibleFormsAsync)), "Form management should expose permission-filtered form lists.");
AssertNotNull(typeof(ReportManagementService).GetMethod(nameof(ReportManagementService.ListAccessibleReportsAsync)), "Report management should expose permission-filtered report lists.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.ListGroupsAsync)), "Identity management should list groups.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.CreateGroupAsync)), "Identity management should create groups.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.UpdateGroupAsync)), "Identity management should update groups.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.ListDepartmentsAsync)), "Identity management should list departments.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.CreateDepartmentAsync)), "Identity management should create departments.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.UpdateDepartmentAsync)), "Identity management should update departments.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.ListDirectoryUsersAsync)), "Identity management should expose active user directory options for form picker fields.");
AssertNotNull(typeof(IdentityManagementService).GetMethod(nameof(IdentityManagementService.ListDirectoryDepartmentsAsync)), "Identity management should expose active department directory options for form picker fields.");
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
    typeof(TriggerActionRegistry).GetMethod("ExecuteScheduledStartWorkflowAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
    "Trigger action registry should execute explicit scheduled workflow-start actions.");
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
AssertNotNull(typeof(RecordSubmissionService).GetConstructors().Single().GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(TriggerEventOutbox)), "Record submission should stage trigger events transactionally.");
AssertNotNull(typeof(RecordMutationService).GetConstructors().Single().GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(TriggerEventOutbox)), "Record mutation should stage trigger events transactionally.");
AssertNotNull(typeof(RecordWorkflowService).GetConstructors().Single().GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(TriggerEventOutbox)), "Record workflow transitions should stage status events transactionally.");
AssertNotNull(typeof(WorkflowApprovalService).GetConstructors().Single().GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(TriggerEventOutbox)), "Approval-completed transitions should stage status events transactionally.");
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
AssertNotNull(typeof(IntegrationApiKeyService).GetMethod(nameof(IntegrationApiKeyService.ListAsync)), "Integration API key service should list keys without exposing raw secrets.");
AssertNotNull(typeof(IntegrationApiKeyService).GetMethod(nameof(IntegrationApiKeyService.CreateAsync)), "Integration API key service should create keys and return the raw key once.");
AssertNotNull(typeof(IntegrationApiKeyService).GetMethod(nameof(IntegrationApiKeyService.RevokeAsync)), "Integration API key service should revoke active keys.");
AssertNotNull(typeof(IntegrationApiKeyService).GetMethod(nameof(IntegrationApiKeyService.RotateAsync)), "Integration API key service should rotate keys and return the new raw key once.");
AssertNotNull(typeof(IntegrationApiKeyService).GetMethod(nameof(IntegrationApiKeyService.AuthenticateAsync)), "Integration API key service should authenticate raw keys for API requests.");
AssertNotNull(typeof(IntegrationConnectorService).GetMethod(nameof(IntegrationConnectorService.ListAsync)), "Integration connector service should list saved connector configurations.");
AssertNotNull(typeof(IntegrationConnectorService).GetMethod(nameof(IntegrationConnectorService.CreateAsync)), "Integration connector service should create connector configs without returning raw secrets.");
AssertNotNull(typeof(IntegrationConnectorService).GetMethod(nameof(IntegrationConnectorService.UpdateAsync)), "Integration connector service should update connector configs with concurrency checks.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Integrations", "IntegrationsEndpoints.cs"))
        .Contains("/api/integrations/connectors", StringComparison.Ordinal),
    "Integration endpoints should expose connector management routes.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Program.cs"))
        .Contains("AddScoped<IntegrationConnectorService>", StringComparison.Ordinal),
    "Integration connector service should be registered for endpoint injection.");
AssertNotNull(typeof(IntegrationLogService).GetMethod(nameof(IntegrationLogService.ListAsync)), "Integration log service should list observable integration attempts.");
AssertNotNull(typeof(IntegrationLogService).GetMethod(nameof(IntegrationLogService.GetAsync)), "Integration log service should get one integration attempt.");
AssertNotNull(typeof(IntegrationLogService).GetMethod(nameof(IntegrationLogService.RecordAsync)), "Integration log service should record sanitized integration attempts.");
AssertNotNull(typeof(IntegrationLogService).GetMethod(nameof(IntegrationLogService.RequestRetryAsync)), "Integration log service should explicitly mark retry requests.");
AssertNotNull(typeof(PublicRecordApiService).GetMethod(nameof(PublicRecordApiService.ListRecordsAsync)), "Public record API service should list records through API key authentication.");
AssertNotNull(typeof(PublicRecordApiService).GetMethod(nameof(PublicRecordApiService.GetRecordAsync)), "Public record API service should read records through API key authentication.");
AssertNotNull(typeof(PublicRecordApiService).GetMethod(nameof(PublicRecordApiService.CreateRecordAsync)), "Public record API service should create records through API key authentication.");
AssertEqual("v1", PublicRecordApiVersions.V1, "Public record API contracts should expose an explicit version.");
AssertTypeAssignable<IPlatformApiModule, IntegrationsModule>();
AssertTrue(new IntegrationsModule().Id == "app.integrations", "Integrations module should expose a stable module id.");
var createApiKeyRequest = new CreateIntegrationApiKeyRequest(
    "Payroll sync",
    "payroll-sync",
    new[] { IntegrationApiKeyScopes.Authenticate });
AssertEqual("payroll-sync", createApiKeyRequest.IntegrationKey, "Create integration API key requests should carry a stable integration identity.");
AssertTrue(createApiKeyRequest.Scopes!.Contains(IntegrationApiKeyScopes.Authenticate), "Create integration API key requests should carry typed scopes.");
var apiKeyDto = new IntegrationApiKeyDto(
    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
    "Payroll sync",
    "payroll-sync",
    "obp_sk_sampleprefix",
    new[] { IntegrationApiKeyScopes.Authenticate },
    true,
    null,
    null,
    null,
    null,
    null,
    "api-key-stamp",
    DateTimeOffset.UtcNow,
    Guid.Parse("11111111-2222-3333-4444-555555555555"),
    null,
    null);
var createdApiKey = new CreatedIntegrationApiKeyDto(apiKeyDto, generatedApiKey.RawKey);
AssertEqual(generatedApiKey.RawKey, createdApiKey.RawKey, "Created integration API key DTOs should return the raw key once.");
AssertEqual("obp_sk_sampleprefix", createdApiKey.ApiKey.KeyPrefix, "Created integration API key DTOs should expose only the display prefix after creation.");
var recordIntegrationLog = new RecordIntegrationLogRequest(
    IntegrationLogDirections.Outbound,
    IntegrationLogTypes.Webhook,
    "payroll-sync",
    IntegrationLogStatuses.Failed,
    "Trigger",
    Guid.Parse("99999999-1111-2222-3333-444444444444"),
    "Record",
    Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444"),
    AttemptCount: 1,
    MaxAttempts: 3,
    IsRetryable: true,
    RequestMetadata: sensitiveMetadata,
    ResponseMetadata: new Dictionary<string, object?> { ["statusCode"] = 500 },
    ErrorCode: "remote_500",
    ErrorMessage: "Remote service returned 500.");
AssertEqual(IntegrationLogDirections.Outbound, recordIntegrationLog.Direction, "Record integration log requests should carry typed direction.");
AssertEqual("payroll-sync", recordIntegrationLog.IntegrationKey, "Record integration log requests should carry integration identity.");
var integrationLogDto = new IntegrationLogDto(
    Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444"),
    IntegrationLogDirections.Outbound,
    IntegrationLogTypes.Webhook,
    "payroll-sync",
    "Trigger",
    Guid.Parse("99999999-1111-2222-3333-444444444444"),
    "Record",
    Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444"),
    IntegrationLogStatuses.Failed,
    1,
    3,
    true,
    integrationRetryNow.AddMinutes(1),
    null,
    null,
    null,
    null,
    null,
    new Dictionary<string, object?> { ["authorization"] = IntegrationMetadataSanitizer.RedactedValue },
    new Dictionary<string, object?> { ["statusCode"] = 500 },
    "remote_500",
    "Remote service returned 500.",
    DateTimeOffset.UtcNow,
    DateTimeOffset.UtcNow,
    "integration-log-stamp",
    DateTimeOffset.UtcNow,
    null,
    null,
    null);
AssertEqual(IntegrationRetryStates.Pending, integrationLogDto.RetryState, "Integration log DTOs should expose retry state.");
var publicCreateRecordRequest = new PublicCreateRecordRequest(new Dictionary<string, object?> { ["email"] = "jane@example.test" });
AssertEqual("jane@example.test", publicCreateRecordRequest.Values["email"]?.ToString(), "Public record create requests should carry record values.");
var publicRecordResponse = new PublicRecordResponse(
    Guid.Parse("cccccccc-1111-2222-3333-444444444444"),
    Guid.Parse("eeeeeeee-1111-2222-3333-444444444444"),
    Guid.Parse("dddddddd-1111-2222-3333-444444444444"),
    "active",
    new Dictionary<string, object?> { ["email"] = "jane@example.test" },
    DateTimeOffset.UtcNow,
    null);
AssertEqual("active", publicRecordResponse.Status, "Public record responses should expose record status.");
AssertFalse(publicRecordResponse.Values.ContainsKey("salary"), "Public record responses should omit hidden fields from values.");
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
var scheduledRunResultDto = new TriggerScheduledRunResultDto(retryLogDto, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow);
AssertEqual(retryLogDto.Id, scheduledRunResultDto.Log.Id, "Scheduled trigger run responses should include the created execution log.");
AssertNotNull(typeof(TriggerExecutionService).GetMethod(nameof(TriggerExecutionService.RetryFailedLogAsync)), "Trigger execution service should expose manual failed-log retry.");
AssertNotNull(typeof(TriggerScheduleService).GetMethod(nameof(TriggerScheduleService.RunScheduleNowAsync)), "Trigger schedule service should expose manual scheduled-trigger runs.");
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
    new[] { PlatformPermissions.Menu.Forms, PlatformPermissions.Forms.Create },
    WorkspaceDefaults.WorkspaceId);
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
    "role-permissions-stamp",
    new[] { PlatformPermissions.Menu.Forms, PlatformPermissions.Forms.Create },
    new[] { new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.View) },
    Array.Empty<RoleReportPermissionDto>(),
    Array.Empty<RoleFieldPermissionDto>());
AssertEqual(sampleRoleId, rolePermissions.RoleId, "Role permissions DTO should expose the role id.");
AssertEqual(PlatformPermissions.Form.View, rolePermissions.FormPermissions.Single().Action, "Role permissions DTO should expose form actions.");

var updateRolePermissions = new UpdateRolePermissionsRequest(
    "role-permissions-stamp",
    new[] { PlatformPermissions.Menu.UsersAccess, PlatformPermissions.Users.Manage },
    new[] { new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.Manage) },
    Array.Empty<RoleReportPermissionDto>(),
    Array.Empty<RoleFieldPermissionDto>());
AssertTrue(updateRolePermissions.Permissions.Contains(PlatformPermissions.Users.Manage), "Update role permissions request should carry global permissions.");
AssertEqual("role-permissions-stamp", updateRolePermissions.ConcurrencyStamp, "Permission updates should carry the parent role concurrency stamp.");

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

var lookupSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition(
            "customer",
            FormFieldTypes.RecordLookup,
            "Customer",
            Required: true,
            Lookup: new FormFieldLookupDefinition(
                "form_records",
                "11111111-1111-1111-1111-111111111111",
                new[] { "customer_name" },
                new[] { "customer_name", "customer_code" }))
    },
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition(
            "page_1",
            null,
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
                                    "col_1",
                                    new ResponsiveSpanDefinition(12, 12, 12),
                                    new[] { "customer" })
                            })
                    })
            })
    }));
AssertTrue(FormSchemaValidator.ValidateSchema(lookupSchema).Valid, "Record lookup schemas should validate with lookup configuration.");
AssertTrue(
    FormSchemaValidator.ValidateRecordValues(
        lookupSchema,
        new Dictionary<string, object?> { ["customer"] = "22222222-2222-2222-2222-222222222222" }).Valid,
    "Record lookup values should accept selected record id strings.");
AssertTrue(
    FormSchemaValidator.ValidateRecordValues(lookupSchema, new Dictionary<string, object?> { ["customer"] = 123 })
        .Errors
        .Any(error => error.Code == "record.lookup_type"),
    "Record lookup values should reject non-string values.");
AssertTrue(
    RecordLookupService.IsRecordLookupValue("22222222-2222-2222-2222-222222222222"),
    "Record lookup value helpers should accept selected record id strings.");
AssertFalse(
    RecordLookupService.IsRecordLookupValue("not-a-record-id"),
    "Record lookup value helpers should reject non-GUID strings.");
var subTableSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition(
            "line_items",
            FormFieldTypes.SubTable,
            "Line items",
            SubTable: new FormFieldSubTableDefinition(
                "child_form_records",
                "11111111-1111-1111-1111-111111111111",
                "parent_request",
                new[] { "item_name", "quantity", "price" },
                AllowInlineCreate: false,
                AllowInlineEdit: false,
                AllowInlineDelete: false,
                MinRows: 0,
                MaxRows: 25))
    },
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition(
            "page_1",
            null,
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
                                    "col_1",
                                    new ResponsiveSpanDefinition(12, 12, 12),
                                    new[] { "line_items" })
                            })
                    })
            })
    }));
AssertTrue(FormSchemaValidator.ValidateSchema(subTableSchema).Valid, "Sub-table schemas should validate with child form configuration.");
AssertTrue(FormSchemaValidator.ValidateRecordValues(subTableSchema, new Dictionary<string, object?>()).Valid, "Sub-table record values should be stored through child records.");
AssertTrue(
    FormSchemaValidator.ValidateRecordValues(subTableSchema, new Dictionary<string, object?> { ["line_items"] = "embedded-child-data" })
        .Errors
        .Any(error => error.Code == "record.sub_table_readonly"),
    "Sub-table values should reject embedded parent-record values.");
AssertTrue(
    FormSchemaValidator.ValidateSchema(
        subTableSchema with
        {
            Fields = new[]
            {
                subTableSchema.Fields.Single() with
                {
                    SubTable = subTableSchema.Fields.Single().SubTable! with
                    {
                        ParentLookupFieldId = string.Empty,
                        DisplayColumnFieldIds = Array.Empty<string>(),
                        MinRows = 10,
                        MaxRows = 3
                    }
                }
            }
        })
        .Errors
        .Any(error => error.Code == "field.sub_table_display_fields_required"),
    "Sub-table fields should require display columns before publishing.");
AssertTrue(
    FormSchemaValidator.ValidateSchema(
        subTableSchema with
        {
            Fields = new[]
            {
                subTableSchema.Fields.Single() with
                {
                    SubTable = subTableSchema.Fields.Single().SubTable! with
                    {
                        ParentLookupFieldId = string.Empty,
                        DisplayColumnFieldIds = Array.Empty<string>(),
                        MinRows = 10,
                        MaxRows = 3
                    }
                }
            }
        })
        .Errors
        .Any(error => error.Code == "field.sub_table_row_range"),
    "Sub-table row limits should reject min values larger than max values.");
var businessFieldSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition("attachment", FormFieldTypes.FileUpload, "Attachment"),
        new FormFieldDefinition("budget", FormFieldTypes.Currency, "Budget"),
        new FormFieldDefinition("completion", FormFieldTypes.Percent, "Completion"),
        new FormFieldDefinition("priority", FormFieldTypes.Rating, "Priority"),
        new FormFieldDefinition("website", FormFieldTypes.Url, "Website"),
        new FormFieldDefinition("start_time", FormFieldTypes.Time, "Start time"),
        new FormFieldDefinition("starts_at", FormFieldTypes.Datetime, "Starts at"),
        new FormFieldDefinition("owner", FormFieldTypes.UserPicker, "Owner"),
        new FormFieldDefinition("department", FormFieldTypes.DepartmentPicker, "Department")
    },
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition(
            "page_1",
            null,
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
                                    "col_1",
                                    new ResponsiveSpanDefinition(12, 12, 12),
                                    new[]
                                    {
                                        "attachment",
                                        "budget",
                                        "completion",
                                        "priority",
                                        "website",
                                        "start_time",
                                        "starts_at",
                                        "owner",
                                        "department"
                                    })
                            })
                    })
            })
    }));
AssertTrue(FormSchemaValidator.ValidateSchema(businessFieldSchema).Valid, "Business form field types should validate in published schemas.");
AssertTrue(
    FormSchemaValidator.ValidateRecordValues(
        businessFieldSchema,
        new Dictionary<string, object?>
        {
            ["attachment"] = "pending-upload.pdf",
            ["budget"] = 1250.5m,
            ["completion"] = 87.25m,
            ["priority"] = 4,
            ["website"] = "https://example.com/request",
            ["start_time"] = "09:30",
            ["starts_at"] = "2026-06-25T09:30",
            ["owner"] = "11111111-1111-1111-1111-111111111111",
            ["department"] = "22222222-2222-2222-2222-222222222222"
        }).Valid,
    "Business form field values should validate with their expected storage shapes.");
var invalidBusinessValues = FormSchemaValidator.ValidateRecordValues(
    businessFieldSchema,
    new Dictionary<string, object?>
    {
        ["attachment"] = 123,
        ["budget"] = "1250",
        ["completion"] = 125,
        ["priority"] = 6,
        ["website"] = "not-a-url",
        ["start_time"] = "25:00",
        ["starts_at"] = "2026-06-25",
        ["owner"] = "not-a-user-id",
        ["department"] = "not-a-department-id"
    });
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.percent"), "Percent values should stay inside 0 to 100.");
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.rating"), "Rating values should stay inside the supported rating range.");
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.url"), "URL values should require an absolute HTTP or HTTPS URL.");
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.time"), "Time values should use HH:mm format.");
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.datetime"), "Date-time values should use datetime-local format.");
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.user_picker_type"), "User picker values should be selected user ids.");
AssertTrue(invalidBusinessValues.Errors.Any(error => error.Code == "record.department_picker_type"), "Department picker values should be selected department ids.");
var addressSchema = new FormSchemaDefinition(
    1,
    new[] { new FormFieldDefinition("site_address", FormFieldTypes.Address, "Site address", Address: new FormFieldAddressDefinition(new[] { FormAddressSubfields.Line1, FormAddressSubfields.Country })) },
    new FormLayoutDefinition(new[] { new FormLayoutPageDefinition("page_1", null, null, new[] { new FormLayoutSectionDefinition("section_1", null, null, new[] { new FormLayoutRowDefinition("row_1", new[] { new FormLayoutColumnDefinition("col_1", new ResponsiveSpanDefinition(12, 12, 12), new[] { "site_address" }) }) }) }) }));
AssertTrue(FormSchemaValidator.ValidateSchema(addressSchema).Valid, "Structured address schemas should validate with supported required subfields.");
var validAddress = JsonSerializer.SerializeToElement(new { line1 = "100 King Street West", city = "Toronto", region = "ON", postalCode = "M5X 1A9", country = "Canada", latitude = 43.648m, longitude = -79.381m });
AssertTrue(FormSchemaValidator.ValidateRecordValues(addressSchema, new Dictionary<string, object?> { ["site_address"] = validAddress }).Valid, "Structured address values should validate.");
AssertEqual("100 King Street West, Toronto, ON, M5X 1A9, Canada", FormAddressValueFormatter.TryFormat(validAddress, out var formattedAddress) ? formattedAddress : null, "Addresses should have a stable human-readable display value.");
var invalidAddress = FormSchemaValidator.ValidateRecordValues(addressSchema, new Dictionary<string, object?> { ["site_address"] = JsonSerializer.SerializeToElement(new { city = "Toronto", country = "", latitude = 91, secret = "no" }) });
AssertTrue(invalidAddress.Errors.Any(error => error.Path == "values.site_address.line1" && error.Code == "record.address_member_required"), "Required address members should use member-specific paths.");
AssertTrue(invalidAddress.Errors.Any(error => error.Code == "record.address_coordinate_range"), "Address coordinates should use bounded ranges.");
AssertTrue(invalidAddress.Errors.Any(error => error.Code == "record.address_member_unknown"), "Unknown address members should be rejected.");
AssertFalse(
    FormSchemaValidator.ValidateSchema(addressSchema with { Fields = new[] { addressSchema.Fields.Single() with { Address = new FormFieldAddressDefinition(new[] { "unsupported" }) } } }).Valid,
    "Unsupported required address subfields should prevent publishing.");
var autonumberSchema = addressSchema with
{
    Fields = new[] { new FormFieldDefinition("request_number", FormFieldTypes.Autonumber, "Request number", Autonumber: new FormFieldAutonumberDefinition("REQ-", "-CA", 42, 6)) },
    Layout = addressSchema.Layout with { Pages = new[] { addressSchema.Layout.Pages.Single() with { Sections = new[] { addressSchema.Layout.Pages.Single().Sections.Single() with { Rows = new[] { addressSchema.Layout.Pages.Single().Sections.Single().Rows.Single() with { Columns = new[] { addressSchema.Layout.Pages.Single().Sections.Single().Rows.Single().Columns.Single() with { Fields = new[] { "request_number" } } } } } } } } } }
};
AssertTrue(FormSchemaValidator.ValidateSchema(autonumberSchema).Valid, "Autonumber schemas should accept bounded formatting configuration.");
AssertEqual("REQ-000042-CA", AutonumberService.Format(42, autonumberSchema.Fields.Single().Autonumber!), "Autonumber formatting should apply prefix, padding, and suffix deterministically.");
AssertThrows<RecordSubmissionException>(() => AutonumberService.EnsureClientValuesAbsent(autonumberSchema, new Dictionary<string, object?> { ["request_number"] = "chosen" }), "Record clients should not supply generated autonumber values.");
AssertFalse(FormSchemaValidator.ValidateSchema(autonumberSchema with { Fields = new[] { autonumberSchema.Fields.Single() with { Autonumber = new FormFieldAutonumberDefinition(new string('x', 41), null, -1, 19) } } }).Valid, "Invalid autonumber bounds should prevent publishing.");
AssertFalse(FormSchemaValidator.ValidateSchema(autonumberSchema with { Fields = new[] { autonumberSchema.Fields.Single() with { Autonumber = new FormFieldAutonumberDefinition(StartAt: FormAutonumberLimits.MaxStartAt + 1) } } }).Valid, "Autonumber starts beyond the cross-client safe bound should prevent publishing.");
var fileUploadSchema = autonumberSchema with
{
    Fields = new[] { new FormFieldDefinition("attachment", FormFieldTypes.FileUpload, "Attachment", FileUpload: new FormFieldFileUploadDefinition(1024, new[] { "application/pdf" })) },
    Layout = autonumberSchema.Layout with { Pages = new[] { autonumberSchema.Layout.Pages.Single() with { Sections = new[] { autonumberSchema.Layout.Pages.Single().Sections.Single() with { Rows = new[] { autonumberSchema.Layout.Pages.Single().Sections.Single().Rows.Single() with { Columns = new[] { autonumberSchema.Layout.Pages.Single().Sections.Single().Rows.Single().Columns.Single() with { Fields = new[] { "attachment" } } } } } } } } } }
};
AssertTrue(FormSchemaValidator.ValidateSchema(fileUploadSchema).Valid, "File upload schemas should accept bounded storage configuration.");
AssertFalse(FormSchemaValidator.ValidateSchema(fileUploadSchema with { Fields = new[] { fileUploadSchema.Fields.Single() with { FileUpload = new FormFieldFileUploadDefinition(FormFileUploadLimits.MaxSizeBytes + 1, new[] { "application/x-msdownload" }) } } }).Valid, "File upload schemas should reject excessive sizes and unsupported types.");
var fileScanner = new DeterministicFileAttachmentScanner();
var inspectedPdf = fileScanner.Inspect("request.pdf", "application/pdf", "%PDF-1.7\nbody"u8.ToArray(), fileUploadSchema.Fields.Single().FileUpload!);
AssertTrue(inspectedPdf.Accepted && inspectedPdf.ContentType == "application/pdf", "Attachment inspection should accept matching bounded PDF content.");
AssertFalse(fileScanner.Inspect("request.exe", "application/pdf", "%PDF-1.7\nbody"u8.ToArray(), fileUploadSchema.Fields.Single().FileUpload!).Accepted, "Attachment inspection should reject filename/content mismatches.");
AssertEqual("report.pdf", FileAttachmentService.NormalizeFileName("../unsafe/report.pdf"), "Attachment filenames should discard client path segments.");
AssertEqual("report.pdf", FileAttachmentService.NormalizeFileName("C:\\fakepath\\report.pdf"), "Attachment filenames should discard Windows client path segments.");
var dependentLookupSchema = lookupSchema with
{
    Fields = new[]
    {
        lookupSchema.Fields.Single() with
        {
            Lookup = lookupSchema.Fields.Single().Lookup! with
            {
                Filters = new[] { new FormFieldLookupFilterDefinition("department", "request_department") }
            }
        }
    }
};
var relationshipTargetId = Guid.Parse("77777777-7777-7777-7777-777777777777");
var relationshipEdges = RecordRelationshipService.ExtractEdges(lookupSchema, new Dictionary<string, object?> { ["customer"] = relationshipTargetId.ToString() });
AssertEqual(1, relationshipEdges.Count, "Lookup record values should materialize one relationship edge.");
AssertEqual("customer", relationshipEdges.Single().FieldId, "Relationship edges should retain the source lookup field.");
AssertEqual(relationshipTargetId, relationshipEdges.Single().TargetRecordId, "Relationship edges should retain the target record.");
AssertEqual(0, RecordRelationshipService.ExtractEdges(lookupSchema, new Dictionary<string, object?> { ["customer"] = "legacy-invalid" }).Count, "Invalid legacy lookup strings should not materialize relationship edges.");
AssertTrue(FormSchemaValidator.ValidateSchema(dependentLookupSchema).Valid, "Dependent record lookup filters should validate with source and parent field ids.");
AssertTrue(
    FormSchemaValidator.ValidateSchema(
        dependentLookupSchema with
        {
            Fields = new[]
            {
                dependentLookupSchema.Fields.Single() with
                {
                    Lookup = dependentLookupSchema.Fields.Single().Lookup! with
                    {
                        Filters = new[] { new FormFieldLookupFilterDefinition("", "request_department") }
                    }
                }
            }
        })
        .Errors
        .Any(error => error.Code == "field.lookup_filter_required"),
    "Dependent record lookup filters should reject missing source field ids.");
AssertTrue(
    RecordLookupService.MatchesLookupFilters(
        new Dictionary<string, object?> { ["department"] = "hr" },
        new[] { new FormFieldLookupFilterDefinition("department", "request_department") },
        new Dictionary<string, string?> { ["request_department"] = "hr" }),
    "Record lookup filters should match source values against parent form dependency values.");
AssertFalse(
    RecordLookupService.MatchesLookupFilters(
        new Dictionary<string, object?> { ["department"] = "finance" },
        new[] { new FormFieldLookupFilterDefinition("department", "request_department") },
        new Dictionary<string, string?> { ["request_department"] = "hr" }),
    "Record lookup filters should exclude records that do not match parent form dependency values.");
var incompleteLookupDraftSchema = lookupSchema with
{
    Fields = new[]
    {
        new FormFieldDefinition(
            "customer",
            FormFieldTypes.RecordLookup,
            "Customer",
            Lookup: new FormFieldLookupDefinition("form_records", string.Empty, Array.Empty<string>(), Array.Empty<string>()))
    }
};
AssertTrue(FormSchemaValidator.ValidateDraftSchema(incompleteLookupDraftSchema).Valid, "Incomplete lookup fields should still save in draft form.");
AssertFalse(FormSchemaValidator.ValidateSchema(incompleteLookupDraftSchema).Valid, "Incomplete lookup fields should not publish.");
var lookupOption = new RecordLookupOptionDto(
    Guid.Parse("22222222-2222-2222-2222-222222222222"),
    "Acme Corp",
    "ACME-001");
AssertEqual("Acme Corp", lookupOption.Label, "Lookup option should expose display label.");
AssertEqual("ACME-001", lookupOption.Description, "Lookup option should expose optional description.");
AssertEqual(
    "Acme Corp - ACME-001",
    RecordLookupService.ComposeLookupLabel(
        new Dictionary<string, object?>
        {
            ["customer_name"] = "Acme Corp",
            ["customer_code"] = "ACME-001"
        },
        new[] { "customer_name", "customer_code" }),
    "Lookup labels should join configured visible label fields.");
AssertTrue(
    RecordLookupService.MatchesLookupSearch(
        new Dictionary<string, object?> { ["customer_name"] = "Acme Corp" },
        new[] { "customer_name" },
        "acme"),
    "Lookup search should match configured visible search fields.");
AssertTrue(
    RecordQueryService.IsLinkedToParentRecord(
        new Dictionary<string, object?> { ["parent_request"] = "22222222-2222-2222-2222-222222222222" },
        "parent_request",
        Guid.Parse("22222222-2222-2222-2222-222222222222")),
    "Sub-table child rows should match selected parent record lookup ids.");
AssertFalse(
    RecordQueryService.IsLinkedToParentRecord(
        new Dictionary<string, object?> { ["parent_request"] = "33333333-3333-3333-3333-333333333333" },
        "parent_request",
        Guid.Parse("22222222-2222-2222-2222-222222222222")),
    "Sub-table child rows should exclude records linked to a different parent.");
AssertTrue(
    RecordQueryService.MatchesSubTableFilters(
        new Dictionary<string, object?> { ["item_name"] = "Laptop stand" },
        new Dictionary<string, string> { ["item_name"] = "lap" }),
    "Sub-table filters should match visible child column values by contains.");
AssertFalse(
    RecordQueryService.MatchesSubTableFilters(
        new Dictionary<string, object?> { ["item_name"] = "Mouse" },
        new Dictionary<string, string> { ["item_name"] = "lap" }),
    "Sub-table filters should exclude child rows that do not match column filters.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordsEndpoints.cs"))
        .Contains("/api/forms/{formId:guid}/fields/{fieldId}/lookup-options", StringComparison.Ordinal),
    "Records endpoints should expose a lookup options route.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordsEndpoints.cs"))
        .Contains("/{recordId:guid}/subtables/{fieldId}/rows", StringComparison.Ordinal),
    "Records endpoints should expose a parent-record scoped sub-table rows route.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordsEndpoints.cs"))
        .Contains("/{recordId:guid}/timeline", StringComparison.Ordinal),
    "Records endpoints should expose a permission-checked record activity timeline route.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordsEndpoints.cs"))
        .Contains("/{recordId:guid}/related/{sourceFormId:guid}/{sourceFieldId}", StringComparison.Ordinal),
    "Records endpoints should expose separately paged related-record rows.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Program.cs"))
        .Contains("AddScoped<RecordLookupService>", StringComparison.Ordinal),
    "Record lookup service should be registered for endpoint injection.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Program.cs"))
        .Contains("AddScoped<RelatedRecordService>", StringComparison.Ordinal),
    "Related record service should be registered for endpoint injection.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Program.cs"))
        .Contains("AddScoped<RecordTimelineService>", StringComparison.Ordinal),
    "Record timeline service should be registered for endpoint injection.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordLookupContracts.cs"))
        .Contains("DependencyValues", StringComparison.Ordinal),
    "Record lookup options requests should carry parent form dependency values for conditional lookups.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordSubmissionService.cs"))
        .Contains("ValidateLookupValuesAsync", StringComparison.Ordinal),
    "Record submission should validate selected lookup records before saving.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RecordMutationService.cs"))
        .Contains("ValidateLookupValuesAsync", StringComparison.Ordinal),
    "Record edits should validate selected lookup records before saving.");

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
recordListRequest = recordListRequest with { Filters = new Dictionary<string, string?> { [ReportableSystemFields.Status] = RecordStatuses.Active } };
AssertEqual(RecordStatuses.Active, recordListRequest.Filters![ReportableSystemFields.Status], "List records requests should carry bounded drill-through filters.");
var subTableRowsRequest = new ListSubTableRowsRequest(
    Page: 2,
    PageSize: 5,
    SortFieldId: "quantity",
    SortDirection: "asc",
    Filters: new Dictionary<string, string?> { ["item_name"] = "Laptop" });
AssertEqual(2, subTableRowsRequest.Page, "Sub-table row requests should carry the requested page.");
AssertEqual(5, subTableRowsRequest.PageSize, "Sub-table row requests should carry the requested page size.");
AssertEqual("quantity", subTableRowsRequest.SortFieldId, "Sub-table row requests should carry the requested sort field.");
AssertEqual("asc", subTableRowsRequest.SortDirection, "Sub-table row requests should carry sort direction.");
AssertEqual("Laptop", subTableRowsRequest.Filters!["item_name"], "Sub-table row requests should carry per-column filters.");
var runListReportRequest = new RunListReportRequest(
    Page: 2,
    PageSize: 10,
    Search: "Jane",
    SortFieldId: "employee_name",
    SortDirection: "desc",
    Filters: new Dictionary<string, string?> { ["department"] = "hr" });
AssertEqual("employee_name", runListReportRequest.SortFieldId, "Run report requests should carry the runtime sort field.");
AssertEqual("desc", runListReportRequest.SortDirection, "Run report requests should carry runtime sort direction.");
AssertEqual("hr", runListReportRequest.Filters!["department"], "Run report requests should carry runtime column filters.");

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

var subTableRows = new SubTableRowsDto(
    "line_items",
    new[]
    {
        new SubTableColumnDto("item_name", "Item name", FormFieldTypes.Text),
        new SubTableColumnDto("quantity", "Quantity", FormFieldTypes.Number)
    },
    1,
    new[]
    {
        new SubTableRowDto(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            new Dictionary<string, object?> { ["item_name"] = "Laptop", ["quantity"] = 1 },
            new Dictionary<string, string>(),
            sampleUpdatedAt)
    });
AssertEqual("line_items", subTableRows.FieldId, "Sub-table row responses should identify the parent field.");
AssertEqual("Item name", subTableRows.Columns[0].Label, "Sub-table row responses should expose visible child column labels.");
AssertEqual("Laptop", subTableRows.Items[0].Values["item_name"], "Sub-table row responses should expose child record values.");

var relatedTargetFormId = Guid.Parse("77777777-7777-7777-7777-777777777777");
var relatedTargetRecordId = Guid.Parse("88888888-8888-8888-8888-888888888888");
var relatedWorkspaceSchema = new FormSchemaDefinition(
    1,
    new[]
    {
        new FormFieldDefinition("parent", FormFieldTypes.RecordLookup, "Parent order", Lookup: new FormFieldLookupDefinition("form_records", relatedTargetFormId.ToString(), new[] { "number" }, new[] { "number" })),
        new FormFieldDefinition("sixth", FormFieldTypes.Text, "Sixth"),
        new FormFieldDefinition("name", FormFieldTypes.Text, "Name"),
        new FormFieldDefinition("secret", FormFieldTypes.Text, "Secret"),
        new FormFieldDefinition("amount", FormFieldTypes.Currency, "Amount"),
        new FormFieldDefinition("document", FormFieldTypes.FileUpload, "Document"),
        new FormFieldDefinition("customer", FormFieldTypes.RecordLookup, "Customer", Lookup: new FormFieldLookupDefinition("form_records", sampleDepartmentId.ToString(), new[] { "name" }, new[] { "name" })),
        new FormFieldDefinition("notes", FormFieldTypes.Textarea, "Notes"),
        new FormFieldDefinition("children", FormFieldTypes.SubTable, "Children", SubTable: new FormFieldSubTableDefinition("form_records", sampleDepartmentId.ToString(), "parent", Array.Empty<string>()))
    },
    new FormLayoutDefinition(new[]
    {
        new FormLayoutPageDefinition("page", null, null, new[]
        {
            new FormLayoutSectionDefinition("section", null, null, new[]
            {
                new FormLayoutRowDefinition("row", new[]
                {
                    new FormLayoutColumnDefinition("column", new ResponsiveSpanDefinition(12, 12, 12), new[] { "name", "parent", "secret", "amount", "document", "customer", "notes", "sixth", "children" })
                })
            })
        })
    }));
var relatedValues = new Dictionary<string, object?> { ["parent"] = relatedTargetRecordId.ToString(), ["name"] = "Invoice 100" };
AssertTrue(RelatedRecordService.IsRelationshipMatch(relatedWorkspaceSchema, relatedValues, "parent", relatedTargetFormId, relatedTargetRecordId), "Related rows should be validated against their immutable lookup definition and stored target value.");
AssertFalse(RelatedRecordService.IsRelationshipMatch(relatedWorkspaceSchema, relatedValues, "parent", relatedTargetFormId, Guid.NewGuid()), "A different target record should not match a related panel.");
AssertFalse(RelatedRecordService.IsRelationshipMatch(relatedWorkspaceSchema, relatedValues, "name", relatedTargetFormId, relatedTargetRecordId), "A non-lookup JSON value should not become a legacy relationship.");
var relatedColumns = RelatedRecordService.BuildPreviewColumns(
    relatedWorkspaceSchema,
    "parent",
    new HashSet<string>(new[] { "secret" }, StringComparer.Ordinal));
AssertSequenceEqual(new[] { "name", "amount", "document", "customer", "notes" }, relatedColumns.Select(column => column.FieldId).ToArray(), "Related preview columns should preserve layout order, omit protected/backlink/subtable fields, and stop at five.");
AssertEqual(string.Empty, RelatedRecordService.FormatCell(relatedWorkspaceSchema.Fields.Single(field => field.Id == "customer"), sampleDepartmentId.ToString(), null), "Unresolved related lookup cells should never fall back to raw UUIDs.");
AssertEqual("Acme", RelatedRecordService.FormatCell(relatedWorkspaceSchema.Fields.Single(field => field.Id == "customer"), sampleDepartmentId.ToString(), "Acme"), "Resolved related lookup cells should expose their permission-safe label.");
AssertEqual(string.Empty, RelatedRecordService.FormatCell(relatedWorkspaceSchema.Fields.Single(field => field.Id == "document"), Guid.NewGuid().ToString(), null), "Unresolved file cells should never expose attachment UUIDs.");
var relatedPanelDto = new RelatedRecordPanelDto(sampleDepartmentId, "Invoices", "parent", "Parent order", relatedColumns, 1);
var relatedRowsDto = new RelatedRecordRowsDto(
    relatedPanelDto,
    1,
    10,
    new[] { new RelatedRecordRowDto(Guid.NewGuid(), RecordStatuses.Active, sampleUpdatedAt, new Dictionary<string, string> { ["name"] = "Invoice 100" }) });
AssertEqual("Invoices", relatedRowsDto.Panel.SourceFormName, "Related row responses should carry their authorized panel descriptor.");
AssertEqual("Invoice 100", relatedRowsDto.Items.Single().Cells["name"], "Related rows should contain display-ready cells instead of unrestricted record values.");
var relatedServiceSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Records", "RelatedRecordService.cs"));
AssertTrue(relatedServiceSource.Contains("ApplyRecordAccessAsync", StringComparison.Ordinal), "Related rows should reuse backend record scopes and policy filtering.");
AssertTrue(relatedServiceSource.Contains("GetFieldAccessAsync", StringComparison.Ordinal), "Related panel and preview metadata should enforce hidden-field rules.");
AssertTrue(relatedServiceSource.Contains("EF.Functions.ILike", StringComparison.Ordinal), "Legacy relationship compatibility should compare JSONB lookup UUIDs case-insensitively.");
AssertTrue(relatedServiceSource.Contains("attachment.Id, attachment.RecordId, attachment.FieldId", StringComparison.Ordinal), "Related file previews should verify the exact stored attachment belongs to the visible record and field.");

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
AssertTypeAssignable<object, RecordTimelineService>();
var recordTimeline = new RecordTimelineDto(
    recordDto.Id,
    new[]
    {
        new RecordTimelineEntryDto(
            "audit:55555555555555555555555555555555",
            RecordTimelineSources.Audit,
            "record_updated",
            null,
            "record updated",
            sampleUpdatedAt,
            sampleUserId)
    });
AssertEqual(RecordTimelineSources.Audit, recordTimeline.Items.Single().Source, "Record timeline entries should expose their event source.");
AssertEqual("record updated", recordTimeline.Items.Single().Summary, "Record timeline entries should expose a display summary without raw metadata.");

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
            ["salary"] = 100000,
            ["request_number"] = "REQ-000042-CA"
        },
        new Dictionary<string, object?>
        {
            ["employee_name"] = "Jordan Lee"
        },
        new FieldAccessResult(
            new HashSet<string>(new[] { "salary" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "email" }, StringComparer.Ordinal)),
        autonumberSchema
    })!;
AssertEqual("Jordan Lee", mergedProtectedValues["employee_name"], "Record updates should keep editable submitted values.");
AssertEqual("jane@company.test", mergedProtectedValues["email"], "Record updates should preserve omitted read-only values.");
AssertEqual(100000, Convert.ToInt32(mergedProtectedValues["salary"]), "Record updates should preserve omitted hidden values.");
AssertEqual("REQ-000042-CA", mergedProtectedValues["request_number"], "Record updates should preserve omitted autonumber values.");
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
            }),
        new("budget", FormFieldTypes.Currency, "Budget"),
        new("completion", FormFieldTypes.Percent, "Completion"),
        new("priority", FormFieldTypes.Rating, "Priority"),
        new("website", FormFieldTypes.Url, "Website"),
        new("owner", FormFieldTypes.UserPicker, "Owner"),
        new("approving_department", FormFieldTypes.DepartmentPicker, "Approving department"),
        new(
            "line_items",
            FormFieldTypes.SubTable,
            "Line items",
            SubTable: new FormFieldSubTableDefinition(
                "child_form_records",
                "11111111-1111-1111-1111-111111111111",
                "parent_request",
                new[] { "item_name", "quantity" }))
    }
};
var reportableFields = FormReportableFieldMetadata.GetReportableFields(reportingSchema);
AssertTrue(reportableFields.Any(field => field.Id == "employee_name" && field.Label == "Employee name" && field.Source == ReportableFieldSources.Form), "Reportable metadata should include form text fields.");
AssertTrue(reportableFields.Any(field => field.Id == "salary" && field.SupportsAggregation), "Reportable metadata should mark number fields as aggregatable.");
AssertTrue(reportableFields.Any(field => field.Id == "budget" && field.SupportsAggregation), "Reportable metadata should mark currency fields as aggregatable.");
AssertTrue(reportableFields.Any(field => field.Id == "completion" && field.SupportsAggregation), "Reportable metadata should mark percent fields as aggregatable.");
AssertTrue(reportableFields.Any(field => field.Id == "priority" && field.SupportsAggregation && field.SupportsChoiceGrouping), "Reportable metadata should mark ratings as numeric and groupable.");
AssertTrue(reportableFields.Any(field => field.Id == "department" && field.SupportsChoiceGrouping), "Reportable metadata should mark choice fields as groupable.");
AssertTrue(reportableFields.Any(field => field.Id == "owner" && field.SupportsChoiceGrouping), "Reportable metadata should mark user pickers as groupable.");
AssertTrue(reportableFields.Any(field => field.Id == "approving_department" && field.SupportsChoiceGrouping), "Reportable metadata should mark department pickers as groupable.");
AssertTrue(reportableFields.Any(field => field.Id == "website" && field.Searchable), "Reportable metadata should mark URL fields as searchable.");
AssertFalse(reportableFields.Any(field => field.Id == "line_items"), "Reportable metadata should not flatten sub-table child records into parent report fields.");
AssertEqual("Human Resources", reportableFields.Single(field => field.Id == "department").Options.Single(option => option.Value == "hr").Label, "Reportable metadata should preserve option labels.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.UpdatedAt), "Reportable metadata should include updated date system field.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.OwnerId), "Reportable metadata should include owner system field.");
AssertTrue(reportableFields.Any(field => field.Id == ReportableSystemFields.DepartmentId), "Reportable metadata should include department system field.");
AssertNotNull(typeof(DefaultReportProvisioningService).GetMethod(nameof(DefaultReportProvisioningService.EnsureAllRecordsReportAsync)), "Default report provisioning should expose an idempotent publish hook.");
AssertEqual("All Records", DefaultListReportFactory.DefaultReportName, "Default report factory should use the standard All Records name.");
var defaultReportConfig = DefaultListReportFactory.CreateAllRecordsConfig(reportingSchema);
AssertSequenceEqual(
    new[] { "employee_name", "salary", "department", "budget", "completion", "priority", "website", "owner", "approving_department", ReportableSystemFields.Status, ReportableSystemFields.CreatedAt },
    defaultReportConfig.Columns.Select(column => column.FieldId).ToArray(),
    "Default All Records reports should show all form fields plus status and created date.");
AssertTrue(defaultReportConfig.Columns.All(column => column.Visible), "Default All Records report columns should be visible.");
AssertEqual(ReportableSystemFields.CreatedAt, defaultReportConfig.Sort.Single().FieldId, "Default All Records reports should sort by created date.");
AssertEqual(ReportSortDirections.Desc, defaultReportConfig.Sort.Single().Direction, "Default All Records reports should show newest records first.");

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
var updateReportRequest = new UpdateListReportRequest("Employee directory updated", listReportConfig, "report-stamp");
AssertEqual("Employee directory updated", updateReportRequest.Name, "Update list report requests should carry the report name.");
AssertEqual("report-stamp", updateReportRequest.ConcurrencyStamp, "Update list report requests should carry concurrency stamps.");
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
AssertTrue(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with
        {
            Filters = new[]
            {
                new ListReportFilterDefinition("salary", ReportFilterOperators.GreaterThan, "90000"),
                new ListReportFilterDefinition(ReportableSystemFields.CreatedAt, ReportFilterOperators.After, sampleCreatedAt.ToString("O"))
            }
        }).Valid,
    "List report configs should allow type-aware numeric and date filter operators.");
AssertTrue(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with { RowOpenAction = ListReportRowOpenActions.Edit }).Valid,
    "List report configs should allow saved row-open behavior settings.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with { RowOpenAction = "modal" }).Valid,
    "List report configs should reject unsupported row-open behavior settings.");
var typedActionConfig = listReportConfig with
{
    ReportActions = new[]
    {
        new ListReportActionDefinition("new", ListReportActionTypes.CreateRecord, "Create request"),
        new ListReportActionDefinition("export", ListReportActionTypes.ExportCsv, "Download CSV")
    },
    RowActions = new[]
    {
        new ListReportActionDefinition("view", ListReportActionTypes.ViewRecord, "Open"),
        new ListReportActionDefinition("delete", ListReportActionTypes.DeleteRecord, "Remove", Confirmation: "Remove this request?")
    }
};
AssertTrue(ListReportConfigValidator.Validate(reportingSchema, typedActionConfig).Valid, "Typed report and row actions should validate in their supported locations.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        typedActionConfig with
        {
            RowActions = new[]
            {
                new ListReportActionDefinition("edit", ListReportActionTypes.EditRecord, "Edit"),
                new ListReportActionDefinition("edit-again", ListReportActionTypes.EditRecord, "Edit again")
            }
        }).Valid,
    "Typed action config should reject duplicate action types.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        typedActionConfig with
        {
            ReportActions = new[] { new ListReportActionDefinition("delete", ListReportActionTypes.DeleteRecord, "Delete") }
        }).Valid,
    "Typed action config should reject row actions in the report action collection.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        typedActionConfig with
        {
            ReportActions = new[] { new ListReportActionDefinition("print", ListReportActionTypes.PrintReport, "Print", Confirmation: "Continue?") }
        }).Valid,
    "Typed action config should allow confirmation metadata only for delete-record actions.");
var actionWithUnknownProperty = JsonSerializer.Deserialize<ListReportActionDefinition>("""
    {"id":"view","type":"view_record","label":"View","enabled":true,"url":"https://example.invalid"}
    """);
AssertNotNull(actionWithUnknownProperty, "Typed action JSON should deserialize for validation.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        typedActionConfig with { RowActions = new[] { actionWithUnknownProperty! } }).Valid,
    "Typed action config should reject unknown URL, script, command, and payload properties.");
var configWithNullAction = JsonSerializer.Deserialize<ListReportConfigDefinition>("""
    {"schemaVersion":1,"columns":[{"fieldId":"employee_name","label":"Employee name","visible":true}],"filters":[],"sort":[],"reportActions":[null],"rowActions":[]}
    """);
AssertNotNull(configWithNullAction, "List report config JSON should deserialize for null-action validation.");
AssertFalse(ListReportConfigValidator.Validate(reportingSchema, configWithNullAction).Valid, "Typed action config should reject null action entries without failing the request pipeline.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with
        {
            Filters = new[] { new ListReportFilterDefinition("employee_name", ReportFilterOperators.GreaterThan, "90000") }
        }).Valid,
    "List report configs should reject numeric filter operators for text fields.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with
        {
            Filters = new[] { new ListReportFilterDefinition("salary", ReportFilterOperators.GreaterThan, "not-a-number") }
        }).Valid,
    "List report configs should reject invalid numeric comparison values.");
AssertFalse(
    ListReportConfigValidator.Validate(
        reportingSchema,
        listReportConfig with
        {
            Filters = new[] { new ListReportFilterDefinition(ReportableSystemFields.CreatedAt, ReportFilterOperators.After, "not-a-date") }
        }).Valid,
    "List report configs should reject invalid date comparison values.");
AssertFalse(
    ListReportConfigValidator.Validate(
        publishableSchema,
        listReportConfig with
        {
            Columns = new[] { new ListReportColumnDefinition("missing_field", "Missing", true, 180) }
        }).Valid,
    "List report configs should reject unknown fields.");
var relatedTargetSchema = new FormSchemaDefinition(
    1,
    new[] { new FormFieldDefinition("credit_limit", FormFieldTypes.Currency, "Credit limit") },
    new FormLayoutDefinition(Array.Empty<FormLayoutPageDefinition>()));
var relatedMetadata = new ReportableFieldMetadata(
    "customer.credit_limit", "Customer › Credit limit", FormFieldTypes.Currency, ReportableFieldSources.Relationship,
    Array.Empty<ReportableFieldOptionMetadata>(), true, true, true, true, false);
var relatedCatalog = new ReportFieldCatalog(
    new Dictionary<string, ReportableFieldMetadata> { [relatedMetadata.Id] = relatedMetadata },
    new Dictionary<string, RelatedReportField>
    {
        [relatedMetadata.Id] = new RelatedReportField(relatedMetadata.Id, "customer", Guid.Parse("11111111-1111-1111-1111-111111111111"), "credit_limit", relatedMetadata, relatedTargetSchema)
    });
var relatedConfig = new ListReportConfigDefinition(
    1,
    new[] { new ListReportColumnDefinition("customer.credit_limit", "Customer credit", true, 160) },
    new[] { new ListReportFilterDefinition("customer.credit_limit", ReportFilterOperators.GreaterThan, "90000") },
    new[] { new ListReportSortDefinition("customer.credit_limit", ReportSortDirections.Desc) });
AssertTrue(ListReportConfigValidator.Validate(relatedCatalog.Fields, relatedConfig).Valid, "Related report fields should retain terminal numeric validation behavior.");
var relationshipReportFields = new ReportRelationshipFieldService(null!, null!);
AssertEqual(0, relationshipReportFields.ValidatePaths(sampleDepartmentId, lookupSchema, relatedConfig, relatedCatalog).Count, "One-hop record lookup report paths should validate.");
AssertTrue(
    relationshipReportFields.ValidatePaths(sampleDepartmentId, lookupSchema, relatedConfig with { Columns = new[] { new ListReportColumnDefinition("customer.parent.credit_limit", "Too deep") } }, relatedCatalog)
        .Any(error => error.Code == "report.relationship.depth"),
    "Related report paths deeper than one lookup should be rejected.");
AssertTrue(
    relationshipReportFields.ValidatePaths(sampleDepartmentId, lookupSchema, relatedConfig with { Columns = new[] { new ListReportColumnDefinition("customer.", "Malformed") } }, relatedCatalog)
        .Any(error => error.Code == "report.relationship.path"),
    "Malformed related report paths should be rejected.");
AssertTrue(
    relationshipReportFields.ValidatePaths(sampleDepartmentId, lookupSchema, relatedConfig with { Columns = new[] { new ListReportColumnDefinition("status.credit_limit", "Not a lookup") } }, relatedCatalog)
        .Any(error => error.Code == "report.relationship.lookup"),
    "Related report paths should begin with a record lookup field.");
AssertTrue(
    relationshipReportFields.ValidatePaths(sampleDepartmentId, lookupSchema, relatedConfig with { Columns = new[] { new ListReportColumnDefinition("customer.unknown", "Unknown") } }, relatedCatalog)
        .Any(error => error.Code == "report.relationship.unknown"),
    "Unknown terminal related fields should be rejected.");
AssertTrue(
    relationshipReportFields.ValidatePaths(Guid.Parse("11111111-1111-1111-1111-111111111111"), lookupSchema, relatedConfig, relatedCatalog)
        .Any(error => error.Code == "report.relationship.cycle"),
    "Self-referencing report paths should be rejected as cyclic.");

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
var relatedExecution = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Customer credit",
    "Employee information",
    relatedConfig,
    reportingSchema,
    executionRecords,
    new RunListReportRequest(Page: 1, PageSize: 10, Search: "120000"),
    fieldMetadataById: relatedCatalog.Fields,
    resolvedValuesByRecordId: new Dictionary<Guid, IReadOnlyDictionary<string, ResolvedReportFieldValue>>
    {
        [executionRecords[0].Id] = new Dictionary<string, ResolvedReportFieldValue> { [relatedMetadata.Id] = new(80000m, "80000") },
        [executionRecords[1].Id] = new Dictionary<string, ResolvedReportFieldValue> { [relatedMetadata.Id] = new(120000m, "120000") },
        [executionRecords[2].Id] = new Dictionary<string, ResolvedReportFieldValue> { [relatedMetadata.Id] = new(200000m, "200000") }
    });
AssertEqual(1, relatedExecution.TotalCount, "Related report values should participate in typed saved filters and runtime search.");
AssertEqual(120000m, relatedExecution.Rows.Single().Cells[relatedMetadata.Id].Value, "Related report cells should preserve terminal raw values.");
AssertEqual("120000", relatedExecution.Rows.Single().Cells[relatedMetadata.Id].DisplayValue, "Related report cells should expose permission-safe display values.");
var relatedRuntimeExecution = ListReportExecutionEngine.ExecuteAll(
    reportSummary.Id,
    sampleDepartmentId,
    "Customer credit",
    "Employee information",
    relatedConfig with { Filters = Array.Empty<ListReportFilterDefinition>() },
    reportingSchema,
    executionRecords,
    new RunListReportRequest(
        SortFieldId: relatedMetadata.Id,
        SortDirection: ReportSortDirections.Desc,
        Filters: new Dictionary<string, string?> { [relatedMetadata.Id] = "000" }),
    fieldMetadataById: relatedCatalog.Fields,
    resolvedValuesByRecordId: new Dictionary<Guid, IReadOnlyDictionary<string, ResolvedReportFieldValue>>
    {
        [executionRecords[0].Id] = new Dictionary<string, ResolvedReportFieldValue> { [relatedMetadata.Id] = new(80000m, "80000") },
        [executionRecords[1].Id] = new Dictionary<string, ResolvedReportFieldValue> { [relatedMetadata.Id] = new(120000m, "120000") },
        [executionRecords[2].Id] = new Dictionary<string, ResolvedReportFieldValue> { [relatedMetadata.Id] = new(200000m, "200000") }
    });
AssertEqual(3, relatedRuntimeExecution.TotalCount, "Runtime related filters should use permission-safe related display values.");
AssertEqual(200000m, relatedRuntimeExecution.Rows.First().Cells[relatedMetadata.Id].Value, "Runtime related sorts should preserve terminal numeric ordering.");
var relatedCsvExport = ListReportCsvExporter.Export(relatedRuntimeExecution);
AssertTrue(relatedCsvExport.Content.Contains("200000", StringComparison.Ordinal), "Related report display values should flow through CSV export.");
var missingRelatedValueExecution = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Missing customer credit",
    "Employee information",
    relatedConfig with
    {
        Filters = new[] { new ListReportFilterDefinition(relatedMetadata.Id, ReportFilterOperators.IsEmpty) },
        Sort = Array.Empty<ListReportSortDefinition>()
    },
    reportingSchema,
    executionRecords,
    new RunListReportRequest(),
    fieldMetadataById: relatedCatalog.Fields,
    resolvedValuesByRecordId: new Dictionary<Guid, IReadOnlyDictionary<string, ResolvedReportFieldValue>>());
AssertEqual(3, missingRelatedValueExecution.TotalCount, "Missing or inaccessible related values should behave as empty values without exposing a reason.");

var runtimeFilteredReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig,
    reportingSchema,
    executionRecords,
    new RunListReportRequest(
        Page: 1,
        PageSize: 10,
        Filters: new Dictionary<string, string?> { ["employee_name"] = "jordan" }));
AssertEqual(1, runtimeFilteredReport.TotalCount, "Report execution should apply runtime column filters after saved filters.");
AssertEqual("Jordan Lee", runtimeFilteredReport.Rows.Single().Cells["employee_name"].DisplayValue, "Runtime column filters should match visible report cell text.");

var runtimeSortedReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig,
    reportingSchema,
    executionRecords,
    new RunListReportRequest(Page: 1, PageSize: 1, SortFieldId: "employee_name", SortDirection: ReportSortDirections.Desc));
AssertEqual("Jordan Lee", runtimeSortedReport.Rows.Single().Cells["employee_name"].DisplayValue, "Runtime sort should override saved report sort before pagination.");

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
var runtimeFullExecutionReport = ListReportExecutionEngine.ExecuteAll(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig,
    reportingSchema,
    executionRecords,
    new RunListReportRequest(
        Search: null,
        SortFieldId: "employee_name",
        SortDirection: ReportSortDirections.Asc,
        Filters: new Dictionary<string, string?> { ["employee_name"] = "j" }));
AssertEqual(2, runtimeFullExecutionReport.Rows.Count, "Full report execution should apply runtime column filters for CSV export and report PDF data.");
AssertEqual("Jane Cooper", runtimeFullExecutionReport.Rows.First().Cells["employee_name"].DisplayValue, "Full report execution should apply runtime sort for CSV export and report PDF data.");
var numericComparisonReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig with
    {
        Filters = new[]
        {
            new ListReportFilterDefinition(ReportableSystemFields.Status, ReportFilterOperators.Equal, RecordStatuses.Active),
            new ListReportFilterDefinition("salary", ReportFilterOperators.GreaterThan, "90000")
        }
    },
    reportingSchema,
    executionRecords,
    new RunListReportRequest(Page: 1, PageSize: 10));
AssertEqual(1, numericComparisonReport.TotalCount, "Saved numeric filters should compare numeric values instead of text.");
AssertEqual("Jane Cooper", numericComparisonReport.Rows.Single().Cells["employee_name"].DisplayValue, "Saved numeric filters should keep records above the configured threshold.");
var dateComparisonReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Open employees",
    "Employee information",
    executionConfig with
    {
        Filters = new[]
        {
            new ListReportFilterDefinition(ReportableSystemFields.Status, ReportFilterOperators.Equal, RecordStatuses.Active),
            new ListReportFilterDefinition(ReportableSystemFields.CreatedAt, ReportFilterOperators.After, sampleCreatedAt.AddSeconds(30).ToString("O"))
        }
    },
    reportingSchema,
    executionRecords,
    new RunListReportRequest(Page: 1, PageSize: 10));
AssertEqual(1, dateComparisonReport.TotalCount, "Saved date filters should compare date values instead of text.");
AssertEqual("Jane Cooper", dateComparisonReport.Rows.Single().Cells["employee_name"].DisplayValue, "Saved date filters should keep records after the configured date.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Reports", "ReportsEndpoints.cs"))
        .Contains("ExportListReportCsvAsync(\n                    httpContext.User,\n                    formId,\n                    reportId,\n                    new RunListReportRequest(1, 100, search, sortFieldId, sortDirection, GetReportFilterValues(httpContext))", StringComparison.Ordinal),
    "CSV export endpoints should pass runtime search, sort, and column filters into report export.");
AssertTrue(
    File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Printing", "PrintingEndpoints.cs"))
        .Contains("new RunListReportRequest(page ?? 1, pageSize ?? 100, search, sortFieldId, sortDirection, GetReportFilterValues(httpContext))", StringComparison.Ordinal),
    "Report PDF endpoints should pass runtime search, sort, and column filters into report execution.");

var lookupReportRecordId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var lookupReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Orders",
    "Order form",
    new ListReportConfigDefinition(
        1,
        new[] { new ListReportColumnDefinition("customer", "Customer", true, 180) },
        Array.Empty<ListReportFilterDefinition>(),
        Array.Empty<ListReportSortDefinition>()),
    lookupSchema,
    new[]
    {
        new FormRecord
        {
            Id = lookupReportRecordId,
            FormId = sampleDepartmentId,
            FormVersionId = publishedVersion.Id,
            Status = RecordStatuses.Active,
            ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
            {
                ["customer"] = "33333333-3333-3333-3333-333333333333"
            }),
            CreatedAt = sampleCreatedAt
        }
    },
    new RunListReportRequest(),
    displayValuesByRecordId: new Dictionary<Guid, IReadOnlyDictionary<string, string>>
    {
        [lookupReportRecordId] = new Dictionary<string, string> { ["customer"] = "Acme Corp" }
    });
AssertEqual("33333333-3333-3333-3333-333333333333", lookupReport.Rows.Single().Cells["customer"].Value, "Lookup report cells should preserve raw selected record ids.");
AssertEqual("Acme Corp", lookupReport.Rows.Single().Cells["customer"].DisplayValue, "Lookup report cells should display resolved lookup labels.");
var rawLookupFilterReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Orders",
    "Order form",
    new ListReportConfigDefinition(
        1,
        new[] { new ListReportColumnDefinition("customer", "Customer", true, 180) },
        new[] { new ListReportFilterDefinition("customer", ReportFilterOperators.Equal, "33333333-3333-3333-3333-333333333333") },
        Array.Empty<ListReportSortDefinition>()),
    lookupSchema,
    new[]
    {
        new FormRecord
        {
            Id = lookupReportRecordId,
            FormId = sampleDepartmentId,
            FormVersionId = publishedVersion.Id,
            Status = RecordStatuses.Active,
            ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
            {
                ["customer"] = "33333333-3333-3333-3333-333333333333"
            }),
            CreatedAt = sampleCreatedAt
        }
    },
    new RunListReportRequest(),
    displayValuesByRecordId: new Dictionary<Guid, IReadOnlyDictionary<string, string>>
    {
        [lookupReportRecordId] = new Dictionary<string, string> { ["customer"] = "Acme Corp" }
    });
AssertEqual(1, rawLookupFilterReport.TotalCount, "Saved lookup equality filters should match raw selected record ids.");
var labelLookupFilterReport = ListReportExecutionEngine.Execute(
    reportSummary.Id,
    sampleDepartmentId,
    "Orders",
    "Order form",
    new ListReportConfigDefinition(
        1,
        new[] { new ListReportColumnDefinition("customer", "Customer", true, 180) },
        new[] { new ListReportFilterDefinition("customer", ReportFilterOperators.Equal, "Acme Corp") },
        Array.Empty<ListReportSortDefinition>()),
    lookupSchema,
    new[]
    {
        new FormRecord
        {
            Id = lookupReportRecordId,
            FormId = sampleDepartmentId,
            FormVersionId = publishedVersion.Id,
            Status = RecordStatuses.Active,
            ValuesJson = JsonSerializer.SerializeToDocument(new Dictionary<string, object?>
            {
                ["customer"] = "33333333-3333-3333-3333-333333333333"
            }),
            CreatedAt = sampleCreatedAt
        }
    },
    new RunListReportRequest(),
    displayValuesByRecordId: new Dictionary<Guid, IReadOnlyDictionary<string, string>>
    {
        [lookupReportRecordId] = new Dictionary<string, string> { ["customer"] = "Acme Corp" }
    });
AssertEqual(1, labelLookupFilterReport.TotalCount, "Saved lookup equality filters should also match resolved display labels.");

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
AssertNotNull(typeof(ReportManagementService).GetMethod(nameof(ReportManagementService.GetListReportAsync)), "Report management should expose saved report detail loading.");
AssertNotNull(typeof(ReportManagementService).GetMethod(nameof(ReportManagementService.UpdateListReportAsync)), "Report management should expose saved report updates.");
AssertNotNull(typeof(ReportManagementService).GetMethod(nameof(ReportManagementService.DeleteListReportAsync)), "Report management should expose saved report deletion.");
var reportsEndpointsSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Reports", "ReportsEndpoints.cs"));
AssertTrue(reportsEndpointsSource.Contains("MapGet(\"/{reportId:guid}\"", StringComparison.Ordinal), "Reports endpoints should expose saved report detail loading.");
AssertTrue(reportsEndpointsSource.Contains("MapPut(\"/{reportId:guid}\"", StringComparison.Ordinal), "Reports endpoints should expose saved report updates.");
AssertTrue(reportsEndpointsSource.Contains("MapDelete(\"/{reportId:guid}\"", StringComparison.Ordinal), "Reports endpoints should expose saved report deletion.");
var reportManagementSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Reports", "ReportManagementService.cs"));
AssertTrue(reportManagementSource.Contains("\"report_updated\"", StringComparison.Ordinal), "Report updates should write audit entries.");
AssertTrue(reportManagementSource.Contains("\"report_deleted\"", StringComparison.Ordinal), "Report deletion should write audit entries.");
AssertTrue(reportManagementSource.Contains("ProjectOperationalActionsAsync", StringComparison.Ordinal), "Report execution should project permission-aware typed operational actions.");
AssertTrue(reportManagementSource.Contains("rowIds.Contains(record.Id)", StringComparison.Ordinal), "Report row action projection should scope permission queries to the returned page.");
AssertTrue(reportManagementSource.Contains("ApplyRecordAccessAsync", StringComparison.Ordinal), "Report row action projection should reuse authoritative record scopes and access policies.");
AssertFalse(reportsEndpointsSource.Contains("/actions/{actionId}", StringComparison.Ordinal), "Typed report actions should reuse authoritative destination endpoints instead of adding a generic action executor.");

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
var multiSeriesRequest = analyticsBreakdownRequest with
{
    Series = new[]
    {
        new DashboardChartSeriesDefinition("records", "Records", new ChartMetricDefinition(ChartMetricTypes.Count), "bar", "primary", "left"),
        new DashboardChartSeriesDefinition("salary", "Salary", new ChartMetricDefinition(ChartMetricTypes.Sum, "salary"), "line", "success", "right")
    }
};
AssertTrue(DashboardAnalyticsRequestValidator.Validate(reportingSchema, multiSeriesRequest).Valid, "Dashboard analytics should validate up to four typed series over one source.");
AssertFalse(DashboardAnalyticsRequestValidator.Validate(reportingSchema, multiSeriesRequest with
{
    Series = Enumerable.Range(0, 5).Select(index => new DashboardChartSeriesDefinition($"series-{index}", $"Series {index}", new ChartMetricDefinition(ChartMetricTypes.Count))).ToArray()
}).Valid, "Dashboard analytics should reject more than four series.");
AssertTrue(ChartWidgetConfigValidator.Validate(reportingSchema, new ChartWidgetConfigDefinition(
    ChartWidgetTypes.ChoiceBreakdown,
    new ChartMetricDefinition(ChartMetricTypes.Count),
    "status",
    Appearance: new DashboardChartAppearanceDefinition("warm", true, true, true, "warning", "currency", "CAD", 2))).Valid,
    "Dashboard chart config should accept bounded appearance and formatting settings.");
AssertFalse(ChartWidgetConfigValidator.Validate(reportingSchema, new ChartWidgetConfigDefinition(
    ChartWidgetTypes.ChoiceBreakdown,
    new ChartMetricDefinition(ChartMetricTypes.Count),
    "status",
    Appearance: new DashboardChartAppearanceDefinition("unsafe", DecimalPlaces: 8))).Valid,
    "Dashboard chart config should reject unsupported palettes and excessive decimals.");

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
try
{
    ensureVisibleAnalyticsRequest!.Invoke(null, new object[]
    {
        multiSeriesRequest with { Series = new[] { new DashboardChartSeriesDefinition("hidden", "Hidden", new ChartMetricDefinition(ChartMetricTypes.Sum, "salary")) } },
        new HashSet<string>(new[] { "salary" }, StringComparer.Ordinal)
    });
    throw new InvalidOperationException("Hidden dashboard series fields should be rejected.");
}
catch (System.Reflection.TargetInvocationException exception)
{
    var analyticsException = exception.InnerException as DashboardAnalyticsException;
    AssertNotNull(analyticsException, "Hidden dashboard series metrics should raise a dashboard analytics exception.");
    AssertEqual(403, analyticsException!.StatusCode, "Hidden dashboard series fields should be forbidden.");
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
var sampleDashboardSchema = DemoDataSeeder.CreateBusinessPerformanceSchema();
var sampleFieldIds = FormReportableFieldMetadata.GetReportableFields(sampleDashboardSchema).Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
AssertTrue(new[] { "title", "category", "region", "priority", "amount", "event_date", "status", "created_at" }.All(sampleFieldIds.Contains), "Business Performance sample data should expose every reportable field used by its dashboard.");
var operationalFieldIds = FormReportableFieldMetadata.GetReportableFields(DemoDataSeeder.CreateOperationalPerformanceSchema()).Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
AssertTrue(new[] { "module", "metric_key", "fiscal_year", "period_type", "period_label", "period_date", "product", "equipment", "actual_value", "target_value", "budget_value", "unit", "status" }.All(operationalFieldIds.Contains), "Operational sample data should expose every reportable field used by the comprehensive dashboard.");
var incidentFieldIds = FormReportableFieldMetadata.GetReportableFields(DemoDataSeeder.CreateHseIncidentSchema()).Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
AssertTrue(new[] { "incident_date", "location", "lost_hours", "incident_cost", "status" }.All(incidentFieldIds.Contains), "HSE sample data should expose every reportable field used by the comprehensive dashboard.");
var sampleAnalyticsRecords = Enumerable.Range(0, 48).Select(index => new FormRecord
{
    Id = Guid.Parse($"12000000-0000-0000-0000-{index + 1:000000000000}"),
    FormId = DemoDataSeeder.BusinessPerformanceFormId,
    FormVersionId = DemoDataSeeder.BusinessPerformanceFormVersionId,
    Status = new[] { "active", "pending", "approved", "closed" }[index % 4],
    CreatedAt = new DateTimeOffset(2025, index / 4 + 1, 15 + index % 4, 12, 0, 0, TimeSpan.Zero),
    ValuesJson = SerializeHarnessJson(new Dictionary<string, object?>
    {
        ["title"] = $"Business item {index + 1:00}",
        ["category"] = new[] { "Product", "Service", "Subscription" }[index % 3],
        ["region"] = new[] { "North", "South", "East", "West" }[index % 4],
        ["priority"] = new[] { "Low", "Medium", "High" }[index % 3],
        ["amount"] = 1000m + index * 125m + (index % 4) * 250m,
        ["event_date"] = new DateTimeOffset(2025, index / 4 + 1, 15, 12, 0, 0, TimeSpan.Zero).ToString("yyyy-MM-dd"),
        ["owner_name"] = "Demo owner"
    })
}).ToArray();
var sampleCount = ChartAggregationEngine.Execute(DemoDataSeeder.BusinessPerformanceFormId, "Business Performance Sample Data",
    new ChartWidgetConfigDefinition(ChartWidgetTypes.NumberCard, new ChartMetricDefinition(ChartMetricTypes.Count), Limit: 12), sampleDashboardSchema, sampleAnalyticsRecords);
var sampleSum = ChartAggregationEngine.Execute(DemoDataSeeder.BusinessPerformanceFormId, "Business Performance Sample Data",
    new ChartWidgetConfigDefinition(ChartWidgetTypes.NumberCard, new ChartMetricDefinition(ChartMetricTypes.Sum, "amount"), Limit: 12), sampleDashboardSchema, sampleAnalyticsRecords);
var sampleAverage = ChartAggregationEngine.Execute(DemoDataSeeder.BusinessPerformanceFormId, "Business Performance Sample Data",
    new ChartWidgetConfigDefinition(ChartWidgetTypes.NumberCard, new ChartMetricDefinition(ChartMetricTypes.Average, "amount"), Limit: 12), sampleDashboardSchema, sampleAnalyticsRecords);
var sampleStatus = ChartAggregationEngine.Execute(DemoDataSeeder.BusinessPerformanceFormId, "Business Performance Sample Data",
    new ChartWidgetConfigDefinition(ChartWidgetTypes.ChoiceBreakdown, new ChartMetricDefinition(ChartMetricTypes.Count), GroupByFieldId: "status", Limit: 12), sampleDashboardSchema, sampleAnalyticsRecords);
var sampleTrend = ChartAggregationEngine.Execute(DemoDataSeeder.BusinessPerformanceFormId, "Business Performance Sample Data",
    new ChartWidgetConfigDefinition(ChartWidgetTypes.DateTrend, new ChartMetricDefinition(ChartMetricTypes.Sum, "amount"), DateFieldId: "event_date", Limit: 12), sampleDashboardSchema, sampleAnalyticsRecords);
AssertEqual(48m, sampleCount.Series.Single().Value, "Business Performance sample record count should remain deterministic.");
AssertEqual(207000m, sampleSum.Series.Single().Value, "Business Performance sample total amount should remain deterministic.");
AssertEqual(4312.5m, sampleAverage.Series.Single().Value, "Business Performance sample average amount should remain deterministic.");
AssertTrue(sampleStatus.Series.All(point => point.Value == 12m), "Business Performance sample should contain twelve records per status.");
AssertEqual(12, sampleTrend.Series.Count, "Business Performance sample should produce twelve monthly trend points in the exact-date trend engine.");
AssertEqual(6250m, sampleTrend.Series.First().Value, "Business Performance sample January amount should remain deterministic.");
AssertEqual(28250m, sampleTrend.Series.Last().Value, "Business Performance sample December amount should remain deterministic.");
var filteredSample = ChartAggregationEngine.Execute(DemoDataSeeder.BusinessPerformanceFormId, "Business Performance Sample Data",
    new ChartWidgetConfigDefinition(ChartWidgetTypes.NumberCard, new ChartMetricDefinition(ChartMetricTypes.Count), Limit: 12), sampleDashboardSchema, sampleAnalyticsRecords,
    dashboardFilters: new[] { new DashboardAnalyticsFilterDefinition("region", new[] { "North" }), new DashboardAnalyticsFilterDefinition("event_date", Start: "2025-01-01", End: "2025-07-01") });
AssertEqual(6m, filteredSample.Series.Single().Value, "Shared dashboard filters should combine select values and inclusive-start/exclusive-end date bounds.");
AssertFalse(DashboardAnalyticsRequestValidator.Validate(sampleDashboardSchema, new DashboardAnalyticsRequest(
    DashboardAnalyticsWidgetTypes.Summary, new DashboardAnalyticsSourceDefinition(DemoDataSeeder.BusinessPerformanceFormId), new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Count),
    Filters: new[] { new DashboardAnalyticsFilterDefinition("owner_name", Start: "2025-01-01") })).Valid, "Date bounds should be rejected for non-date fields.");
var dashboardWithProvenance = dashboardConfig with
{
    TemplateProvenance = new DashboardTemplateProvenanceDefinition("business-performance-sample", 1, DateTimeOffset.Parse("2026-08-21T00:00:00Z"))
};
dashboardWithProvenance = dashboardWithProvenance with { Filters = new[] { new SavedDashboardFilterDefinition("region", "Region", "single_select", sampleDepartmentId, "department", new[] { "HR" }) } };
AssertTrue(DashboardDefinitionValidator.Validate(dashboardWithProvenance, dashboardLayout, dashboardSources).Valid, "Dashboard template provenance should remain valid informational metadata.");
var dashboardWithFilterDefaults = dashboardConfig with { Filters = new[] { new SavedDashboardFilterDefinition("status", "Status", "record_status", sampleDepartmentId, ReportableSystemFields.Status, new[] { RecordStatuses.Active, "pending" }, null, new SavedDashboardFilterValueDefinition(ReportableSystemFields.Status, new[] { RecordStatuses.Active }), true) } };
AssertTrue(DashboardDefinitionValidator.Validate(dashboardWithFilterDefaults, dashboardLayout, dashboardSources).Valid, "Dashboard filters should preserve bounded defaults and required state.");
var invalidDashboardFilterDefault = dashboardWithFilterDefaults with { Filters = new[] { dashboardWithFilterDefaults.Filters!.Single() with { DefaultValue = new SavedDashboardFilterValueDefinition("another-field", new[] { "unknown" }) } } };
AssertTrue(DashboardDefinitionValidator.Validate(invalidDashboardFilterDefault, dashboardLayout, dashboardSources).Errors.Any(error => error.Code == "dashboard.filter.default_invalid"), "Dashboard filters should reject defaults that do not match the configured field and options.");
var invalidDashboardFilterTarget = dashboardWithFilterDefaults with { Filters = new[] { dashboardWithFilterDefaults.Filters!.Single() with { SourceFormId = Guid.Parse("77777777-7777-7777-7777-777777777777"), ApplyToWidgetIds = new[] { "widget-1" } } } };
AssertTrue(DashboardDefinitionValidator.Validate(invalidDashboardFilterTarget, dashboardLayout, dashboardSources).Errors.Any(error => error.Code == "dashboard.filter.widget_source_mismatch"), "Dashboard filters should not target widgets backed by another form.");
var dashboardWithRecordInteraction = dashboardConfig with { Widgets = new[] { dashboardConfig.Widgets.Single() with { Interaction = new DashboardWidgetInteractionDefinition("records") } } };
AssertTrue(DashboardDefinitionValidator.Validate(dashboardWithRecordInteraction, dashboardLayout, dashboardSources).Valid, "Analytics widgets should support typed source-record drill-through.");
var reportInteractionId = dashboardSources.Single().Reports.Single().Id;
var dashboardWithReportInteraction = dashboardConfig with { Widgets = new[] { dashboardConfig.Widgets.Single() with { Interaction = new DashboardWidgetInteractionDefinition("report", reportInteractionId) } } };
AssertTrue(DashboardDefinitionValidator.Validate(dashboardWithReportInteraction, dashboardLayout, dashboardSources).Valid, "Analytics widgets should support permitted saved-report drill-through.");
var invalidDashboardInteraction = dashboardConfig with { Widgets = new[] { dashboardConfig.Widgets.Single() with { Interaction = new DashboardWidgetInteractionDefinition("report", Guid.Parse("88888888-8888-8888-8888-888888888888")) } } };
AssertTrue(DashboardDefinitionValidator.Validate(invalidDashboardInteraction, dashboardLayout, dashboardSources).Errors.Any(error => error.Code == "dashboard.interaction.report_missing"), "Dashboard drill-through should reject reports outside the widget source.");
AssertFalse(DashboardDefinitionValidator.Validate(dashboardConfig with { TemplateProvenance = new DashboardTemplateProvenanceDefinition("", 0, default) }, dashboardLayout, dashboardSources).Valid, "Invalid template provenance should be rejected without affecting legacy dashboards that omit it.");
var tooManySections = dashboardConfig with { Sections = Enumerable.Range(0, 17).Select(index => new SavedDashboardSectionDefinition($"section-{index}", $"Section {index}", index)).ToArray() };
AssertTrue(DashboardDefinitionValidator.Validate(tooManySections, dashboardLayout, dashboardSources).Errors.Any(error => error.Code == "dashboard.sections.limit"), "Dashboard validation should bound section counts.");
var invalidSectionIcon = dashboardConfig with { Sections = new[] { new SavedDashboardSectionDefinition("section-1", "Section", 0, "unknown-icon") }, Widgets = new[] { dashboardConfig.Widgets.Single() with { SectionId = "section-1" } } };
AssertTrue(DashboardDefinitionValidator.Validate(invalidSectionIcon, dashboardLayout, dashboardSources).Errors.Any(error => error.Code == "dashboard.section.icon_invalid"), "Dashboard validation should reject unregistered section icons.");

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
var legacyAdapterConfig = new SavedDashboardConfigDefinition(
    1,
    new[]
    {
        new SavedDashboardWidgetDefinition(
            "legacy-adapter",
            "Legacy adapter",
            null,
            null,
            "overview",
            new DashboardAdapterWidgetDefinition("installed-later", "summary", new Dictionary<string, object?> { ["period"] = "week" }))
    },
    new[] { new SavedDashboardSectionDefinition("overview", "Overview", 0) });
var legacyAdapterLayout = new SavedDashboardLayoutDefinition(1, new[] { new SavedDashboardWidgetLayoutDefinition("legacy-adapter", DashboardWidgetWidths.Full, 1) });
AssertTrue(
    DashboardDefinitionValidator.Validate(legacyAdapterConfig, legacyAdapterLayout, Array.Empty<DashboardSourceDefinition>()).Valid,
    "Legacy adapter widgets should support a null analytics source form without crashing dashboard reads.");
var unknownBuiltInAdapterConfig = legacyAdapterConfig with
{
    Widgets = new[]
    {
        legacyAdapterConfig.Widgets.Single() with
        {
            Adapter = new DashboardAdapterWidgetDefinition("sample-dashboard", "unregistered-view", new Dictionary<string, object?> { ["unregisteredSetting"] = "value" })
        }
    }
};
var validBuiltInAdapterConfig = legacyAdapterConfig with
{
    Widgets = new[]
    {
        legacyAdapterConfig.Widgets.Single() with
        {
            Adapter = new DashboardAdapterWidgetDefinition("sample-dashboard", "target_attainment", new Dictionary<string, object?>
            {
                ["actual"] = 92, ["target"] = 100, ["unit"] = "%", ["tone"] = "warning", ["sourceLabel"] = "Sample data"
            })
        }
    }
};
AssertTrue(DashboardDefinitionValidator.Validate(validBuiltInAdapterConfig, legacyAdapterLayout, Array.Empty<DashboardSourceDefinition>()).Valid, "Registered built-in adapter visualizations and settings should remain publishable.");
var unknownBuiltInAdapterValidation = DashboardDefinitionValidator.Validate(unknownBuiltInAdapterConfig, legacyAdapterLayout, Array.Empty<DashboardSourceDefinition>());
AssertTrue(unknownBuiltInAdapterValidation.Errors.Any(error => error.Code == "dashboard.adapter.visualization_unknown"), "Built-in adapters should reject unregistered visualizations.");
AssertTrue(unknownBuiltInAdapterValidation.Errors.Any(error => error.Code == "dashboard.adapter.setting_unknown"), "Built-in adapters should reject unregistered settings while legacy third-party adapters remain compatible.");

var createDashboardRequest = new CreateDashboardRequest(
    "Operations dashboard",
    "Saved widgets for V2 dashboards.",
    dashboardConfig,
    dashboardLayout);
AssertEqual("Operations dashboard", createDashboardRequest.Name, "Create dashboard requests should carry dashboard names.");
AssertEqual(1, createDashboardRequest.Config.Widgets.Count, "Create dashboard requests should carry widgets.");
var dashboardPublicationMutation = new DashboardPublicationMutationRequest("dashboard-stamp");
AssertEqual("dashboard-stamp", dashboardPublicationMutation.ConcurrencyStamp, "Dashboard publication requests should carry the browser concurrency stamp.");
var dashboardOwnerId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
var dashboardViewerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
var existingDashboardWithoutSettings = new DashboardDefinition
{
    Id = Guid.Parse("cccccccc-0000-0000-0000-000000000003"),
    Name = "Existing dashboard",
    ConfigJson = SerializeHarnessJson(dashboardConfig),
    LayoutJson = SerializeHarnessJson(dashboardLayout),
    CreatedById = dashboardOwnerId
};
var privateDashboard = new DashboardDefinition
{
    Id = Guid.Parse("dddddddd-0000-0000-0000-000000000004"),
    Name = "Private dashboard",
    ConfigJson = SerializeHarnessJson(dashboardConfig),
    LayoutJson = SerializeHarnessJson(dashboardLayout),
    CreatedById = dashboardOwnerId,
    Status = DashboardPublicationStatuses.Published,
    ExtraPropertiesJson = SerializeHarnessJson(new DashboardSettingsDefinition(DashboardVisibilityModes.Private, false))
};
var legacySettings = DashboardDefinitionAccess.ResolveSettings(existingDashboardWithoutSettings);
AssertEqual(DashboardVisibilityModes.Workspace, legacySettings.Visibility, "Legacy dashboards without settings should resolve to workspace visibility.");
AssertFalse(legacySettings.IsDefault, "Legacy dashboards should not become default dashboards implicitly.");
AssertFalse(
    DashboardDefinitionAccess.CanView(existingDashboardWithoutSettings, new DashboardAccessContext(dashboardViewerId, CanManageDashboards: false)),
    "Legacy dashboards should remain drafts and hidden from normal viewers.");
AssertFalse(
    DashboardDefinitionAccess.CanView(privateDashboard, new DashboardAccessContext(dashboardViewerId, CanManageDashboards: false)),
    "Private dashboards should not be visible to unrelated dashboard viewers.");
AssertTrue(
    DashboardDefinitionAccess.CanView(privateDashboard, new DashboardAccessContext(dashboardOwnerId, CanManageDashboards: false)),
    "Private dashboards should remain visible to their creator.");
AssertTrue(
    DashboardDefinitionAccess.CanView(privateDashboard, new DashboardAccessContext(dashboardViewerId, CanManageDashboards: true)),
    "Dashboard managers should retain management visibility over private dashboards.");
AssertFalse(
    DashboardDefinitionAccess.ValidateSettings(new DashboardSettingsDefinition(DashboardVisibilityModes.Private, true)).Valid,
    "Private dashboards should not be allowed to become the shared default dashboard.");
AssertTrue(DashboardSlugs.IsValid("operations-overview"), "Human-readable dashboard slugs should validate.");
AssertFalse(DashboardSlugs.IsValid("Operations Overview"), "Dashboard slugs should reject spaces and uppercase characters.");
AssertFalse(DashboardSlugs.IsValid("builder"), "Dashboard slugs should reject reserved route values.");
var permissionProtectedDashboard = new DashboardDefinition
{
    Id = Guid.NewGuid(), Name = "Protected", Status = DashboardPublicationStatuses.Published,
    ViewPermission = "dashboards.team.view", ConfigJson = SerializeHarnessJson(dashboardConfig), LayoutJson = SerializeHarnessJson(dashboardLayout)
};
AssertFalse(DashboardDefinitionAccess.CanView(permissionProtectedDashboard, new DashboardAccessContext(dashboardViewerId, false, new HashSet<string>())), "Dashboard view permissions should be backend enforced.");
AssertTrue(DashboardDefinitionAccess.CanView(permissionProtectedDashboard, new DashboardAccessContext(dashboardViewerId, false, new HashSet<string> { "dashboards.team.view" })), "Users with the configured dashboard permission should be able to view a published workspace dashboard.");
var sharedRoleId = Guid.NewGuid();
var sharedGroupId = Guid.NewGuid();
var audienceDashboard = new DashboardDefinition
{
    Id = Guid.NewGuid(), Name = "Audience dashboard", Status = DashboardPublicationStatuses.Published,
    ConfigJson = SerializeHarnessJson(dashboardConfig), LayoutJson = SerializeHarnessJson(dashboardLayout),
    ExtraPropertiesJson = SerializeHarnessJson(new DashboardSettingsDefinition(DashboardVisibilityModes.Workspace, false,
        new[] { dashboardOwnerId }, new[] { sharedRoleId }, new[] { sharedGroupId }))
};
AssertTrue(DashboardDefinitionAccess.CanView(audienceDashboard, new DashboardAccessContext(dashboardOwnerId, false)), "Explicit dashboard viewers should receive access.");
AssertTrue(DashboardDefinitionAccess.CanView(audienceDashboard, new DashboardAccessContext(dashboardViewerId, false, RoleIds: new HashSet<Guid> { sharedRoleId })), "A matching role should receive dashboard access.");
AssertTrue(DashboardDefinitionAccess.CanView(audienceDashboard, new DashboardAccessContext(dashboardViewerId, false, GroupIds: new HashSet<Guid> { sharedGroupId })), "A matching group should receive dashboard access.");
AssertFalse(DashboardDefinitionAccess.CanView(audienceDashboard, new DashboardAccessContext(dashboardViewerId, false)), "Unmatched viewers should not receive restricted dashboard access.");
AssertFalse(DashboardDefinitionAccess.ValidateSettings(new DashboardSettingsDefinition(DashboardVisibilityModes.Private, false, new[] { dashboardViewerId })).Valid, "Private dashboards should reject additional viewers.");
AssertTrue(DashboardMenuIcons.Approved.Contains("factory"), "Dashboard menus should use approved icon registry keys.");
AssertTypeAssignable<object, DashboardDefinitionService>();
AssertNotNull(typeof(DashboardDefinitionService).GetMethod(nameof(DashboardDefinitionService.DeleteAsync)), "Dashboard management should expose audited soft deletion for lifecycle cleanup.");
AssertNotNull(typeof(DashboardDefinitionService).GetMethod(nameof(DashboardDefinitionService.ListArchivedAsync)), "Dashboard management should expose archived dashboards to managers.");
AssertNotNull(typeof(DashboardDefinitionService).GetMethod(nameof(DashboardDefinitionService.RestoreArchivedAsync)), "Dashboard management should restore archived dashboards as drafts.");
AssertNotNull(typeof(DashboardDefinitionService).GetMethod(nameof(DashboardDefinitionService.PermanentlyDeleteAsync)), "Dashboard management should expose guarded permanent deletion.");
AssertEqual(30, new DashboardRecycleBinOptions().PermanentDeleteMinimumAgeDays, "Production recycle-bin retention should default to 30 days.");
var dashboardsEndpointsSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Dashboards", "DashboardsEndpoints.cs"));
AssertTrue(dashboardsEndpointsSource.Contains("MapDelete(\"/{dashboardId:guid}\"", StringComparison.Ordinal), "Dashboard endpoints should expose permission-protected soft deletion.");
AssertTrue(dashboardsEndpointsSource.Contains("MapGet(\"/archived\"", StringComparison.Ordinal), "Dashboard endpoints should expose a permission-protected recycle-bin list.");
AssertTrue(dashboardsEndpointsSource.Contains("MapPost(\"/{dashboardId:guid}/restore\"", StringComparison.Ordinal), "Dashboard endpoints should expose permission-protected archive restoration.");
AssertTrue(dashboardsEndpointsSource.Contains("MapDelete(\"/{dashboardId:guid}/permanent\"", StringComparison.Ordinal), "Dashboard endpoints should expose guarded permanent deletion.");
var dashboardServiceSource = File.ReadAllText(GetRepositoryFilePath("src", "api", "Modules", "Dashboards", "DashboardDefinitionService.cs"));
AssertTrue(dashboardServiceSource.Contains("ToSummaryDto(item.Dashboard, item.Snapshot!)", StringComparison.Ordinal), "Normal dashboard lists should project the published snapshot instead of editable draft data.");
AssertTrue(dashboardServiceSource.Contains("\"dashboard_deleted\"", StringComparison.Ordinal), "Dashboard deletion should write an audit entry.");
AssertTrue(dashboardServiceSource.Contains("\"dashboard_archive_restored\"", StringComparison.Ordinal), "Dashboard restoration should write an audit entry.");
AssertTrue(dashboardServiceSource.Contains("\"dashboard_permanently_deleted\"", StringComparison.Ordinal), "Permanent dashboard deletion should retain an audit entry.");
AssertTrue(dashboardServiceSource.Contains("ExecuteDeleteAsync", StringComparison.Ordinal), "Permanent dashboard deletion should deliberately bypass the soft-delete convention.");
AssertTypeAssignable<IPlatformApiModule, DashboardsModule>();

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
    }
}

static JsonDocument SerializeHarnessJson<T>(T value)
{
    return JsonSerializer.SerializeToDocument(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
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

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
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

static void AssertConcurrencyStamp<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");
    var property = entity.FindProperty(nameof(IHasConcurrencyStamp.ConcurrencyStamp))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should expose a concurrency stamp.");

    AssertTrue(property.IsConcurrencyToken, $"{typeof(TEntity).Name}.ConcurrencyStamp should be enforced by the database update predicate.");
}

static void AssertWorkspaceOwned<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model)
{
    var entity = model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should be mapped.");
    var workspaceProperty = entity.FindProperty(nameof(IWorkspaceOwned.WorkspaceId))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} should have workspace ownership.");

    AssertTrue(!workspaceProperty.IsNullable, $"{typeof(TEntity).Name}.WorkspaceId should be required.");
    AssertIndex<TEntity>(model, new[] { nameof(IWorkspaceOwned.WorkspaceId) }, $"{typeof(TEntity).Name}.WorkspaceId should be indexed.");
    AssertTrue(entity.GetDeclaredQueryFilters().Any(), $"{typeof(TEntity).Name} should have an active-workspace query filter.");
    AssertTrue(
        entity.GetForeignKeys().Any(foreignKey =>
            foreignKey.Properties.Count == 1
            && foreignKey.Properties[0].Name == nameof(IWorkspaceOwned.WorkspaceId)
            && foreignKey.PrincipalEntityType.ClrType == typeof(Workspace)),
        $"{typeof(TEntity).Name}.WorkspaceId should reference workspaces.");
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
