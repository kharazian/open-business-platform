using Microsoft.EntityFrameworkCore;
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
using OpenBusinessPlatform.Api.Modules.Records;
using OpenBusinessPlatform.Api.Modules.Reports;

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
AssertTable<Department>(model, "departments");
AssertTable<UserDepartment>(model, "user_departments");
AssertTable<FormDefinition>(model, "forms");
AssertTable<FormVersion>(model, "form_versions");
AssertTable<FormRecord>(model, "records");
AssertTable<ReportDefinition>(model, "reports");
AssertTable<DashboardDefinition>(model, "dashboards");
AssertTable<AuditLogEntry>(model, "audit_logs");

AssertTypeAssignable<AuditedAggregateRoot<Guid>, User>();
AssertTypeAssignable<Entity<Guid>, PasswordResetToken>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, Role>();
AssertTypeAssignable<Entity<Guid>, RolePermission>();
AssertTypeAssignable<Entity<Guid>, RoleFormPermission>();
AssertTypeAssignable<AuditedAggregateRoot<Guid>, Department>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, FormDefinition>();
AssertTypeAssignable<CreationAuditedEntity<Guid>, FormVersion>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, FormRecord>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, ReportDefinition>();
AssertTypeAssignable<FullAuditedAggregateRoot<Guid>, DashboardDefinition>();
AssertTypeAssignable<Entity<Guid>, AuditLogEntry>();

AssertGuidId<User>(model);
AssertGuidId<PasswordResetToken>(model);
AssertGuidId<Role>(model);
AssertGuidId<RolePermission>(model);
AssertGuidId<RoleFormPermission>(model);
AssertGuidId<Department>(model);
AssertGuidId<FormDefinition>(model);
AssertGuidId<FormVersion>(model);
AssertGuidId<FormRecord>(model);
AssertGuidId<ReportDefinition>(model);
AssertGuidId<DashboardDefinition>(model);
AssertGuidId<AuditLogEntry>(model);

AssertUniqueIndex<User>(model, new[] { nameof(User.Email) }, "Users should have a unique email index.");
AssertUniqueIndex<Role>(model, new[] { nameof(Role.Name) }, "Roles should have a unique role name index.");
AssertUniqueIndex<RolePermission>(model, new[] { nameof(RolePermission.RoleId), nameof(RolePermission.Permission) }, "Role permissions should be unique per role/permission.");
AssertUniqueIndex<RoleFormPermission>(model, new[] { nameof(RoleFormPermission.RoleId), nameof(RoleFormPermission.FormId), nameof(RoleFormPermission.Action) }, "Role form permissions should be unique per role/form/action.");
AssertUniqueIndex<FormVersion>(model, new[] { nameof(FormVersion.FormId), nameof(FormVersion.VersionNumber) }, "Form versions should be unique per form/version number.");

AssertJsonColumn<FormVersion>(model, nameof(FormVersion.SchemaJson));
AssertJsonColumn<FormVersion>(model, nameof(FormVersion.LayoutJson));
AssertJsonColumn<FormVersion>(model, nameof(FormVersion.ValidationJson));
AssertJsonColumn<FormRecord>(model, nameof(FormRecord.ValuesJson));
AssertJsonColumn<ReportDefinition>(model, nameof(ReportDefinition.ConfigJson));
AssertJsonColumn<DashboardDefinition>(model, nameof(DashboardDefinition.ConfigJson));
AssertJsonColumn<DashboardDefinition>(model, nameof(DashboardDefinition.LayoutJson));
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

AssertColumn<User>(model, nameof(User.PasswordHash), "password_hash", "Users should store a password hash column.");
AssertColumn<User>(model, nameof(User.PasswordUpdatedAt), "password_updated_at", "Users should store password update metadata.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.UserId), "user_id", "Password reset tokens should be tied to users.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.TokenHash), "token_hash", "Password reset tokens should store only token hashes.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.ExpiresAt), "expires_at", "Password reset tokens should expire.");
AssertColumn<PasswordResetToken>(model, nameof(PasswordResetToken.UsedAt), "used_at", "Password reset tokens should track use.");

AssertUniqueIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.TokenHash) }, "Password reset token hashes should be unique.");
AssertIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.UserId) }, "Password reset tokens should be indexed by user.");
AssertIndex<PasswordResetToken>(model, new[] { nameof(PasswordResetToken.ExpiresAt) }, "Password reset tokens should be indexed by expiry.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.FormId) }, "Records should be indexed by form.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.FormVersionId) }, "Records should be indexed by form version.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.Status) }, "Records should be indexed by status.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.OwnerId) }, "Records should be indexed by owner.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.DepartmentId) }, "Records should be indexed by department.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.CreatedById) }, "Records should be indexed by creator.");
AssertIndex<FormRecord>(model, new[] { nameof(FormRecord.CreatedAt) }, "Records should be indexed by created date.");
AssertIndex<ReportDefinition>(model, new[] { nameof(ReportDefinition.FormId) }, "Reports should be indexed by form.");
AssertIndex<ReportDefinition>(model, new[] { nameof(ReportDefinition.Type) }, "Reports should be indexed by type.");
AssertIndex<ReportDefinition>(model, new[] { nameof(ReportDefinition.CreatedById) }, "Reports should be indexed by creator.");
AssertIndex<DashboardDefinition>(model, new[] { nameof(DashboardDefinition.CreatedById) }, "Dashboards should be indexed by creator.");
AssertIndex<DashboardDefinition>(model, new[] { nameof(DashboardDefinition.Name) }, "Dashboards should be indexed by name.");
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

var resetLink = PasswordRecoveryEmailFactory.BuildResetLink("http://localhost:5174/reset-password", rawResetToken);
AssertTrue(resetLink.StartsWith("http://localhost:5174/reset-password?token=", StringComparison.Ordinal), "Password reset links should point to the configured reset page.");
AssertTrue(resetLink.Contains(Uri.EscapeDataString(rawResetToken), StringComparison.Ordinal), "Password reset links should include the raw token only in the email URL.");

var recoveryEmail = PasswordRecoveryEmailFactory.CreateResetEmail("jane@company.test", resetLink, TimeSpan.FromMinutes(60));
AssertEqual("jane@company.test", recoveryEmail.ToEmail, "Password recovery emails should target the requested user email.");
AssertTrue(recoveryEmail.Subject.Contains("password", StringComparison.OrdinalIgnoreCase), "Password recovery emails should describe the password reset.");
AssertTrue(recoveryEmail.TextBody.Contains(resetLink, StringComparison.Ordinal), "Password recovery emails should include the reset link.");

var demoSchema = DemoDataSeeder.CreateEmployeeInformationSchema();
AssertEqual(8, demoSchema.Fields.Count, "Demo seed data should include the V1 employee information fields.");
AssertTrue(demoSchema.Fields.Any(field => field.Id == "email" && field.Type == FormFieldTypes.Email), "Demo employee form should include an email field.");
AssertEqual(4, DemoDataSeeder.DemoUsers.Count, "Demo seed data should include admin, builder, user, and viewer accounts.");
AssertEqual(3, DemoDataSeeder.DemoDepartments.Count, "Demo seed data should include HR, Finance, and Operations departments.");
AssertEqual(10, DemoDataSeeder.DemoEmployeeRecords.Count, "Demo seed data should include ten employee records.");

AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Menu.UsersAccess), "Built-in permissions should include Users & Access menu visibility.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Users.Manage), "Built-in permissions should include user management.");
AssertTrue(PlatformPermissions.AllBuiltInPermissions.Contains(PlatformPermissions.Reports.Manage), "Built-in permissions should include report management.");
AssertTrue(PlatformPermissions.FormActions.Contains(PlatformPermissions.Form.View), "Form actions should include view.");

var bootstrapPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.NameIdentifier, BootstrapAdminUserDirectory.BootstrapAdminId),
    new Claim(ClaimTypes.Role, PlatformRoles.Admin)
}, "Test"));
var permissionService = new PermissionService(dbContext);
AssertTrue(await permissionService.CanAsync(bootstrapPrincipal, PlatformPermissions.Users.Manage, CancellationToken.None), "Bootstrap admin should have user management permission.");
AssertTrue(await permissionService.CanAccessFormAsync(bootstrapPrincipal, Guid.NewGuid(), PlatformPermissions.Form.Manage, CancellationToken.None), "Bootstrap admin should have form management permission.");

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

var createUser = new CreateUserRequest("Jane Cooper", "jane@company.test", "temporary-password-1", new[] { sampleRoleId }, new[] { sampleDepartmentId }, true);
AssertEqual(true, createUser.IsActive, "Create user request should carry active state.");
AssertEqual("temporary-password-1", createUser.Password, "Create user request should carry the initial password.");

var updateUser = new UpdateUserRequest("Jane Cooper", true, new[] { sampleRoleId }, new[] { sampleDepartmentId }, "user-stamp");
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
    new[] { new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.View) });
AssertEqual(sampleRoleId, rolePermissions.RoleId, "Role permissions DTO should expose the role id.");
AssertEqual(PlatformPermissions.Form.View, rolePermissions.FormPermissions.Single().Action, "Role permissions DTO should expose form actions.");

var updateRolePermissions = new UpdateRolePermissionsRequest(
    new[] { PlatformPermissions.Menu.UsersAccess, PlatformPermissions.Users.Manage },
    new[] { new RoleFormPermissionDto(sampleDepartmentId, PlatformPermissions.Form.Manage) });
AssertTrue(updateRolePermissions.Permissions.Contains(PlatformPermissions.Users.Manage), "Update role permissions request should carry global permissions.");

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
    recordDto.Values,
    publishableSchema,
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
