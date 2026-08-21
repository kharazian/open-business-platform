using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;
using OpenBusinessPlatform.Api.Modules.Workspaces;
using OpenBusinessPlatform.Api.Modules.Dashboard;
using OpenBusinessPlatform.Api.Modules.Dashboards;

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public const string DemoUserPassword = "DemoUser!2026";

    public static readonly Guid EmployeeInformationFormId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid EmployeeInformationFormVersionId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid BusinessPerformanceFormId = Guid.Parse("11000000-0000-0000-0000-000000000001");
    public static readonly Guid BusinessPerformanceFormVersionId = Guid.Parse("11000000-0000-0000-0000-000000000002");
    public static readonly Guid BusinessPerformanceDashboardId = Guid.Parse("11000000-0000-0000-0000-000000000003");
    public static readonly Guid OperationalPerformanceFormId = Guid.Parse("11000000-0000-0000-0000-000000000011");
    public static readonly Guid OperationalPerformanceFormVersionId = Guid.Parse("11000000-0000-0000-0000-000000000012");
    public static readonly Guid HseIncidentFormId = Guid.Parse("11000000-0000-0000-0000-000000000021");
    public static readonly Guid HseIncidentFormVersionId = Guid.Parse("11000000-0000-0000-0000-000000000022");

    public static readonly IReadOnlyList<DemoDepartmentDefinition> DemoDepartments = new[]
    {
        new DemoDepartmentDefinition(Guid.Parse("20000000-0000-0000-0000-000000000001"), "HR"),
        new DemoDepartmentDefinition(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Finance"),
        new DemoDepartmentDefinition(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Operations")
    };

    public static readonly IReadOnlyList<DemoUserDefinition> DemoUsers = new[]
    {
        new DemoUserDefinition(Guid.Parse("30000000-0000-0000-0000-000000000001"), "Demo Admin", "admin.demo@company.test", PlatformRoles.Admin, "Operations"),
        new DemoUserDefinition(Guid.Parse("30000000-0000-0000-0000-000000000002"), "Demo Builder", "builder.demo@company.test", PlatformRoles.Builder, "Operations"),
        new DemoUserDefinition(Guid.Parse("30000000-0000-0000-0000-000000000003"), "Demo User", "user.demo@company.test", PlatformRoles.User, "HR"),
        new DemoUserDefinition(Guid.Parse("30000000-0000-0000-0000-000000000004"), "Demo Viewer", "viewer.demo@company.test", PlatformRoles.Viewer, "Finance")
    };

    public static readonly IReadOnlyList<DemoEmployeeRecordDefinition> DemoEmployeeRecords = new[]
    {
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000001"), "Avery", "Stone", "avery.stone@company.test", "555-0101", "HR", "2025-01-06", "Full-time", "People operations specialist."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000002"), "Maya", "Patel", "maya.patel@company.test", "555-0102", "Finance", "2024-11-18", "Full-time", "Accounts payable lead."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000003"), "Noah", "Kim", "noah.kim@company.test", "555-0103", "Operations", "2025-02-03", "Contractor", "Warehouse systems contractor."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000004"), "Sofia", "Garcia", "sofia.garcia@company.test", "555-0104", "HR", "2023-08-21", "Part-time", "Recruiting coordinator."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000005"), "Ethan", "Brooks", "ethan.brooks@company.test", "555-0105", "Finance", "2024-03-12", "Full-time", "Financial analyst."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000006"), "Lina", "Chen", "lina.chen@company.test", "555-0106", "Operations", "2022-09-26", "Full-time", "Plant supervisor."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000007"), "Owen", "Reed", "owen.reed@company.test", "555-0107", "HR", "2025-04-14", "Contractor", "Benefits project support."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000008"), "Priya", "Nair", "priya.nair@company.test", "555-0108", "Finance", "2021-06-07", "Full-time", "Controller."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000009"), "Marcus", "Lee", "marcus.lee@company.test", "555-0109", "Operations", "2023-12-04", "Full-time", "Fleet coordinator."),
        new DemoEmployeeRecordDefinition(Guid.Parse("40000000-0000-0000-0000-000000000010"), "Iris", "Morgan", "iris.morgan@company.test", "555-0110", "Operations", "2024-07-15", "Part-time", "Facilities coordinator.")
    };

    private static readonly IReadOnlyDictionary<string, Guid> RoleIds = new Dictionary<string, Guid>
    {
        [PlatformRoles.Admin] = Guid.Parse("50000000-0000-0000-0000-000000000001"),
        [PlatformRoles.Builder] = Guid.Parse("50000000-0000-0000-0000-000000000002"),
        [PlatformRoles.User] = Guid.Parse("50000000-0000-0000-0000-000000000003"),
        [PlatformRoles.Viewer] = Guid.Parse("50000000-0000-0000-0000-000000000004")
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task SeedDevelopmentAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DemoDataSeeder));

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OpenBusinessPlatformDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<LocalPasswordHasher>();
            await SeedAsync(dbContext, passwordHasher, cancellationToken);
            logger.LogInformation("Demo seed data is ready.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Demo seed data was skipped. Apply migrations and ensure PostgreSQL is running to enable local startup data.");
        }
    }

    public static async Task SeedAsync(
        OpenBusinessPlatformDbContext dbContext,
        LocalPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var roles = await EnsureRolesAsync(dbContext, cancellationToken);
        await EnsureRolePermissionsAsync(dbContext, roles, cancellationToken);
        var departments = await EnsureDepartmentsAsync(dbContext, cancellationToken);
        var users = await EnsureUsersAsync(dbContext, passwordHasher, roles, departments, cancellationToken);
        await EnsureWorkspaceMembershipsAsync(dbContext, users, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var formVersion = await EnsureEmployeeInformationFormAsync(dbContext, cancellationToken);
        var businessFormVersion = await EnsureBusinessPerformanceFormAsync(dbContext, cancellationToken);
        var operationsFormVersion = await EnsureSampleFormAsync(dbContext, OperationalPerformanceFormId, OperationalPerformanceFormVersionId, "Operational Performance Sample Data", "Development-only deterministic operational facts for dashboard examples.", CreateOperationalPerformanceSchema(), cancellationToken);
        var incidentFormVersion = await EnsureSampleFormAsync(dbContext, HseIncidentFormId, HseIncidentFormVersionId, "HSE Incident Sample Data", "Development-only deterministic safety and incident facts for dashboard examples.", CreateHseIncidentSchema(), cancellationToken);
        await EnsureFormPermissionsAsync(dbContext, roles, formVersion.FormId, cancellationToken);
        await EnsureFormPermissionsAsync(dbContext, roles, businessFormVersion.FormId, cancellationToken);
        await EnsureFormPermissionsAsync(dbContext, roles, operationsFormVersion.FormId, cancellationToken);
        await EnsureFormPermissionsAsync(dbContext, roles, incidentFormVersion.FormId, cancellationToken);
        await EnsureEmployeeRecordsAsync(dbContext, formVersion, users, departments, cancellationToken);
        await EnsureBusinessPerformanceRecordsAsync(dbContext, businessFormVersion, users, departments, cancellationToken);
        await EnsureOperationalPerformanceRecordsAsync(dbContext, operationsFormVersion, users, departments, cancellationToken);
        await EnsureHseIncidentRecordsAsync(dbContext, incidentFormVersion, users, departments, cancellationToken);
        await EnsureBusinessPerformanceDashboardAsync(dbContext, businessFormVersion, operationsFormVersion, incidentFormVersion, users, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static FormSchemaDefinition CreateEmployeeInformationSchema()
    {
        var fields = new[]
        {
            new FormFieldDefinition("first_name", FormFieldTypes.Text, "First name", Required: true),
            new FormFieldDefinition("last_name", FormFieldTypes.Text, "Last name", Required: true),
            new FormFieldDefinition("email", FormFieldTypes.Email, "Email", Required: true),
            new FormFieldDefinition("phone", FormFieldTypes.Phone, "Phone"),
            new FormFieldDefinition(
                "department",
                FormFieldTypes.Select,
                "Department",
                Required: true,
                Options: DemoDepartments
                    .Select(department => new FormFieldOptionDefinition(
                        $"department_{NormalizeOptionId(department.Name)}",
                        department.Name,
                        department.Name))
                    .ToArray()),
            new FormFieldDefinition("start_date", FormFieldTypes.Date, "Start date", Required: true),
            new FormFieldDefinition(
                "employment_type",
                FormFieldTypes.Radio,
                "Employment type",
                Required: true,
                Options: new[]
                {
                    new FormFieldOptionDefinition("employment_full_time", "Full-time", "Full-time"),
                    new FormFieldOptionDefinition("employment_part_time", "Part-time", "Part-time"),
                    new FormFieldOptionDefinition("employment_contractor", "Contractor", "Contractor")
                }),
            new FormFieldDefinition("notes", FormFieldTypes.Textarea, "Notes")
        };

        return new FormSchemaDefinition(
            1,
            fields,
            new FormLayoutDefinition(new[]
            {
                new FormLayoutPageDefinition(
                    "page_employee",
                    "Employee",
                    "Employee information intake.",
                    new[]
                    {
                        new FormLayoutSectionDefinition(
                            "section_identity",
                            "Identity",
                            null,
                            new[]
                            {
                                CreateTwoColumnRow("row_name", "first_name", "last_name"),
                                CreateTwoColumnRow("row_contact", "email", "phone")
                            }),
                        new FormLayoutSectionDefinition(
                            "section_employment",
                            "Employment",
                            null,
                            new[]
                            {
                                CreateTwoColumnRow("row_department", "department", "start_date"),
                                CreateTwoColumnRow("row_type", "employment_type", "notes")
                            })
                    })
            }));
    }

    public static FormSchemaDefinition CreateBusinessPerformanceSchema()
    {
        var fields = new[]
        {
            new FormFieldDefinition("title", FormFieldTypes.Text, "Title", Required: true),
            Choice("category", "Category", "Product", "Service", "Subscription"),
            Choice("region", "Region", "North", "South", "East", "West"),
            Choice("priority", "Priority", "Low", "Medium", "High"),
            new FormFieldDefinition("amount", FormFieldTypes.Currency, "Amount", Required: true),
            new FormFieldDefinition("event_date", FormFieldTypes.Date, "Event date", Required: true),
            new FormFieldDefinition("owner_name", FormFieldTypes.Text, "Owner name")
        };
        return new FormSchemaDefinition(1, fields, new FormLayoutDefinition(new[]
        {
            new FormLayoutPageDefinition("page_business", "Business performance", "Deterministic development analytics data.", new[]
            {
                new FormLayoutSectionDefinition("section_details", "Details", null, new[]
                {
                    CreateTwoColumnRow("row_title_category", "title", "category"),
                    CreateTwoColumnRow("row_region_priority", "region", "priority"),
                    CreateTwoColumnRow("row_amount_date", "amount", "event_date"),
                    new FormLayoutRowDefinition("row_owner", new[] { new FormLayoutColumnDefinition("row_owner_full", new ResponsiveSpanDefinition(12, 12, 12), new[] { "owner_name" }) })
                })
            })
        }));
    }

    public static FormSchemaDefinition CreateOperationalPerformanceSchema()
    {
        var fields = new[]
        {
            new FormFieldDefinition("title", FormFieldTypes.Text, "Title", Required: true),
            Choice("module", "Module", "Loss", "Production", "Engineering", "Supply Chain", "QAQC"),
            Choice("metric_key", "Metric", "Total Loss", "Manufacturing Loss", "OEE", "Utilities", "Inventory Accuracy", "First-Time Release"),
            Choice("fiscal_year", "Fiscal year", "2025", "2026"),
            Choice("period_type", "Period type", "Week", "Month", "Quarter"),
            new FormFieldDefinition("period_label", FormFieldTypes.Text, "Period label", Required: true),
            new FormFieldDefinition("period_number", FormFieldTypes.Number, "Period number", Required: true),
            new FormFieldDefinition("period_date", FormFieldTypes.Date, "Period date", Required: true),
            Choice("product", "Product / recipe", "Classic", "Premium", "Light", "Specialty"),
            Choice("equipment", "Equipment", "Line 1", "Line 2", "Dryer", "Packaging"),
            new FormFieldDefinition("actual_value", FormFieldTypes.Number, "Actual value", Required: true),
            new FormFieldDefinition("target_value", FormFieldTypes.Number, "Target value", Required: true),
            new FormFieldDefinition("budget_value", FormFieldTypes.Number, "Budget value", Required: true),
            new FormFieldDefinition("numerator", FormFieldTypes.Number, "Numerator"),
            new FormFieldDefinition("denominator", FormFieldTypes.Number, "Denominator"),
            new FormFieldDefinition("unit", FormFieldTypes.Text, "Unit", Required: true)
        };
        return CreateSingleSectionSchema("page_operations", "Operational performance", "Deterministic period and target facts.", "section_operations", fields);
    }

    public static FormSchemaDefinition CreateHseIncidentSchema()
    {
        var fields = new[]
        {
            new FormFieldDefinition("title", FormFieldTypes.Text, "Title", Required: true),
            new FormFieldDefinition("incident_date", FormFieldTypes.Date, "Incident date", Required: true),
            Choice("incident_type", "Incident type", "Near miss", "First aid", "Medical treatment", "Lost time"),
            Choice("severity", "Severity", "Low", "Medium", "High", "Critical"),
            Choice("location", "Location", "Receiving", "Processing", "Packaging", "Warehouse"),
            Choice("body_part", "Body part", "Hand", "Back", "Eye", "Foot", "None"),
            new FormFieldDefinition("lost_hours", FormFieldTypes.Number, "Lost hours", Required: true),
            new FormFieldDefinition("lost_shifts", FormFieldTypes.Number, "Lost shifts", Required: true),
            new FormFieldDefinition("incident_cost", FormFieldTypes.Currency, "Incident cost", Required: true),
            new FormFieldDefinition("training_team", FormFieldTypes.Text, "Training team", Required: true),
            new FormFieldDefinition("training_assigned", FormFieldTypes.Number, "Training assigned", Required: true),
            new FormFieldDefinition("training_completed", FormFieldTypes.Number, "Training completed", Required: true)
        };
        return CreateSingleSectionSchema("page_hse", "HSE incidents", "Deterministic safety and training facts.", "section_hse", fields);
    }

    private static FormSchemaDefinition CreateSingleSectionSchema(string pageId, string title, string description, string sectionId, IReadOnlyList<FormFieldDefinition> fields)
    {
        var rows = fields.Chunk(2).Select((pair, index) => new FormLayoutRowDefinition($"row_{index + 1}", pair.Select((field, column) => new FormLayoutColumnDefinition($"row_{index + 1}_{column + 1}", new ResponsiveSpanDefinition(12, 6, 6), new[] { field.Id })).ToArray())).ToArray();
        return new FormSchemaDefinition(1, fields, new FormLayoutDefinition(new[] { new FormLayoutPageDefinition(pageId, title, description, new[] { new FormLayoutSectionDefinition(sectionId, title, null, rows) }) }));
    }

    private static FormFieldDefinition Choice(string id, string label, params string[] values) =>
        new(id, FormFieldTypes.Select, label, Required: true,
            Options: values.Select(value => new FormFieldOptionDefinition($"{id}_{NormalizeOptionId(value)}", value, value)).ToArray());

    private static async Task<Dictionary<string, Role>> EnsureRolesAsync(
        OpenBusinessPlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .Where(role => RoleIds.Keys.Contains(role.Name))
            .ToDictionaryAsync(role => role.Name, cancellationToken);

        foreach (var (name, id) in RoleIds)
        {
            if (roles.ContainsKey(name))
            {
                continue;
            }

            var role = new Role
            {
                Id = id,
                Name = name,
                Description = name switch
                {
                    PlatformRoles.Admin => "Demo administrators with full platform access.",
                    PlatformRoles.Builder => "Demo builders who can manage forms and records.",
                    PlatformRoles.User => "Demo users who can submit and view employee records.",
                    _ => "Demo viewers who can inspect employee records."
                },
                IsActive = true
            };

            dbContext.Roles.Add(role);
            roles[name] = role;
        }

        return roles;
    }

    private static async Task EnsureRolePermissionsAsync(
        OpenBusinessPlatformDbContext dbContext,
        IReadOnlyDictionary<string, Role> roles,
        CancellationToken cancellationToken)
    {
        await EnsureBuiltInPermissionsAsync(dbContext, roles[PlatformRoles.Admin], PlatformPermissions.AllBuiltInPermissions, cancellationToken);
        await EnsureBuiltInPermissionsAsync(
            dbContext,
            roles[PlatformRoles.Builder],
            new[]
            {
                PlatformPermissions.Menu.Dashboard,
                PlatformPermissions.Menu.Forms,
                PlatformPermissions.Menu.Reports,
                PlatformPermissions.Forms.Create,
                PlatformPermissions.Reports.Manage,
                PlatformPermissions.Dashboards.Manage
            },
            cancellationToken);
        await EnsureBuiltInPermissionsAsync(
            dbContext,
            roles[PlatformRoles.User],
            new[] { PlatformPermissions.Menu.Dashboard, PlatformPermissions.Menu.Forms },
            cancellationToken);
        await EnsureBuiltInPermissionsAsync(
            dbContext,
            roles[PlatformRoles.Viewer],
            new[] { PlatformPermissions.Menu.Dashboard, PlatformPermissions.Menu.Forms },
            cancellationToken);
    }

    private static async Task EnsureBuiltInPermissionsAsync(
        OpenBusinessPlatformDbContext dbContext,
        Role role,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions.AnyAsync(
                candidate => candidate.RoleId == role.Id && candidate.Permission == permission,
                cancellationToken);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    Permission = permission
                });
            }
        }
    }

    private static async Task<Dictionary<string, Department>> EnsureDepartmentsAsync(
        OpenBusinessPlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var departments = await dbContext.Departments
            .Where(department => DemoDepartments.Select(seed => seed.Name).Contains(department.Name))
            .ToDictionaryAsync(department => department.Name, cancellationToken);

        foreach (var seed in DemoDepartments)
        {
            if (departments.ContainsKey(seed.Name))
            {
                continue;
            }

            var department = new Department
            {
                Id = seed.Id,
                Name = seed.Name,
                IsActive = true
            };

            dbContext.Departments.Add(department);
            departments[seed.Name] = department;
        }

        return departments;
    }

    private static async Task<Dictionary<string, User>> EnsureUsersAsync(
        OpenBusinessPlatformDbContext dbContext,
        LocalPasswordHasher passwordHasher,
        IReadOnlyDictionary<string, Role> roles,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var emails = DemoUsers.Select(user => user.Email).ToArray();
        var users = await dbContext.Users
            .Where(user => emails.Contains(user.Email))
            .ToDictionaryAsync(user => user.Email, cancellationToken);

        foreach (var seed in DemoUsers)
        {
            if (!users.TryGetValue(seed.Email, out var user))
            {
                user = new User
                {
                    Id = seed.Id,
                    Name = seed.Name,
                    Email = seed.Email,
                    IsActive = true,
                    PasswordHash = passwordHasher.HashPassword(DemoUserPassword),
                    PasswordUpdatedAt = DateTimeOffset.UtcNow
                };

                dbContext.Users.Add(user);
                users[seed.Email] = user;
            }

            var role = roles[seed.RoleName];
            if (!await dbContext.UserRoles.AnyAsync(candidate => candidate.UserId == user.Id && candidate.RoleId == role.Id, cancellationToken))
            {
                dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }

            var department = departments[seed.DepartmentName];
            if (!await dbContext.UserDepartments.AnyAsync(candidate => candidate.UserId == user.Id && candidate.DepartmentId == department.Id, cancellationToken))
            {
                dbContext.UserDepartments.Add(new UserDepartment
                {
                    UserId = user.Id,
                    DepartmentId = department.Id,
                    IsPrimary = true
                });
            }
        }

        return users;
    }

    private static async Task EnsureWorkspaceMembershipsAsync(
        OpenBusinessPlatformDbContext dbContext,
        IReadOnlyDictionary<string, User> users,
        CancellationToken cancellationToken)
    {
        var userIds = users.Values.Select(user => user.Id).ToArray();
        var existingUserIds = await dbContext.WorkspaceMemberships
            .Where(membership =>
                membership.WorkspaceId == WorkspaceDefaults.WorkspaceId
                && userIds.Contains(membership.UserId))
            .Select(membership => membership.UserId)
            .ToArrayAsync(cancellationToken);
        var existing = existingUserIds.ToHashSet();
        var now = DateTimeOffset.UtcNow;

        foreach (var seed in DemoUsers.Where(seed => !existing.Contains(seed.Id)))
        {
            dbContext.WorkspaceMemberships.Add(new WorkspaceMembership
            {
                Id = Guid.NewGuid(),
                WorkspaceId = WorkspaceDefaults.WorkspaceId,
                UserId = seed.Id,
                Role = seed.RoleName == PlatformRoles.Admin
                    ? WorkspaceMembershipRoles.Admin
                    : WorkspaceMembershipRoles.Member,
                Status = WorkspaceMembershipStatuses.Active,
                IsDefault = true,
                InvitedAt = now,
                ActivatedAt = now
            });
        }
    }

    private static async Task<FormVersion> EnsureEmployeeInformationFormAsync(
        OpenBusinessPlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == EmployeeInformationFormId
                    || candidate.Name == "Employee Information Form",
                cancellationToken);

        if (form?.CurrentVersion is not null)
        {
            return form.CurrentVersion;
        }

        var schema = CreateEmployeeInformationSchema();

        if (form is null)
        {
            form = new FormDefinition
            {
                Id = EmployeeInformationFormId,
                Name = "Employee Information Form",
                Description = "Demo employee information intake form.",
                Status = FormStatuses.Draft,
                DraftSchemaJson = SerializeToDocument(schema)
            };

            dbContext.Forms.Add(form);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var versionNumber = await dbContext.FormVersions
            .Where(version => version.FormId == form.Id)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var formVersion = new FormVersion
        {
            Id = form.Id == EmployeeInformationFormId ? EmployeeInformationFormVersionId : Guid.NewGuid(),
            FormId = form.Id,
            VersionNumber = versionNumber + 1,
            SchemaJson = SerializeToDocument(schema),
            PublishedAt = DateTimeOffset.UtcNow
        };

        dbContext.FormVersions.Add(formVersion);
        form.CurrentVersionId = formVersion.Id;
        form.Status = FormStatuses.Published;
        form.DraftSchemaJson ??= SerializeToDocument(schema);
        await dbContext.SaveChangesAsync(cancellationToken);

        return formVersion;
    }

    private static async Task<FormVersion> EnsureBusinessPerformanceFormAsync(OpenBusinessPlatformDbContext dbContext, CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms.Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == BusinessPerformanceFormId || candidate.Name == "Business Performance Sample Data", cancellationToken);
        if (form?.CurrentVersion is not null) return form.CurrentVersion;
        var schema = CreateBusinessPerformanceSchema();
        if (form is null)
        {
            form = new FormDefinition
            {
                Id = BusinessPerformanceFormId,
                Name = "Business Performance Sample Data",
                Description = "Development-only deterministic records for the comprehensive dashboard sample.",
                Status = FormStatuses.Draft,
                DraftSchemaJson = SerializeToDocument(schema)
            };
            dbContext.Forms.Add(form);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        var versionNumber = await dbContext.FormVersions.Where(version => version.FormId == form.Id)
            .Select(version => (int?)version.VersionNumber).MaxAsync(cancellationToken) ?? 0;
        var version = new FormVersion
        {
            Id = form.Id == BusinessPerformanceFormId ? BusinessPerformanceFormVersionId : Guid.NewGuid(),
            FormId = form.Id,
            VersionNumber = versionNumber + 1,
            SchemaJson = SerializeToDocument(schema),
            PublishedAt = DateTimeOffset.UtcNow
        };
        dbContext.FormVersions.Add(version);
        form.CurrentVersionId = version.Id;
        form.Status = FormStatuses.Published;
        form.DraftSchemaJson ??= SerializeToDocument(schema);
        await dbContext.SaveChangesAsync(cancellationToken);
        return version;
    }

    private static async Task<FormVersion> EnsureSampleFormAsync(
        OpenBusinessPlatformDbContext dbContext,
        Guid formId,
        Guid versionId,
        string name,
        string description,
        FormSchemaDefinition schema,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms.Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId || candidate.Name == name, cancellationToken);
        if (form?.CurrentVersion is not null) return form.CurrentVersion;
        if (form is null)
        {
            form = new FormDefinition { Id = formId, Name = name, Description = description, Status = FormStatuses.Draft, DraftSchemaJson = SerializeToDocument(schema) };
            dbContext.Forms.Add(form);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        var versionNumber = await dbContext.FormVersions.Where(version => version.FormId == form.Id).Select(version => (int?)version.VersionNumber).MaxAsync(cancellationToken) ?? 0;
        var version = new FormVersion
        {
            Id = form.Id == formId ? versionId : Guid.NewGuid(),
            FormId = form.Id,
            VersionNumber = versionNumber + 1,
            SchemaJson = SerializeToDocument(schema),
            PublishedAt = DateTimeOffset.UtcNow
        };
        dbContext.FormVersions.Add(version);
        form.CurrentVersionId = version.Id;
        form.Status = FormStatuses.Published;
        form.DraftSchemaJson ??= SerializeToDocument(schema);
        await dbContext.SaveChangesAsync(cancellationToken);
        return version;
    }

    private static async Task EnsureFormPermissionsAsync(
        OpenBusinessPlatformDbContext dbContext,
        IReadOnlyDictionary<string, Role> roles,
        Guid formId,
        CancellationToken cancellationToken)
    {
        await EnsureFormActionsAsync(dbContext, roles[PlatformRoles.Admin], formId, PlatformPermissions.FormActions, cancellationToken);
        await EnsureFormActionsAsync(dbContext, roles[PlatformRoles.Builder], formId, PlatformPermissions.FormActions, cancellationToken);
        await EnsureFormActionsAsync(
            dbContext,
            roles[PlatformRoles.User],
            formId,
            new[] { PlatformPermissions.Form.Submit, PlatformPermissions.Form.View },
            cancellationToken);
        await EnsureFormActionsAsync(
            dbContext,
            roles[PlatformRoles.Viewer],
            formId,
            new[] { PlatformPermissions.Form.View },
            cancellationToken);
    }

    private static async Task EnsureFormActionsAsync(
        OpenBusinessPlatformDbContext dbContext,
        Role role,
        Guid formId,
        IEnumerable<string> actions,
        CancellationToken cancellationToken)
    {
        foreach (var action in actions)
        {
            var exists = await dbContext.RoleFormPermissions.AnyAsync(
                candidate => candidate.RoleId == role.Id
                    && candidate.FormId == formId
                    && candidate.Action == action,
                cancellationToken);

            if (!exists)
            {
                dbContext.RoleFormPermissions.Add(new RoleFormPermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    FormId = formId,
                    Action = action
                });
            }
        }
    }

    private static async Task EnsureEmployeeRecordsAsync(
        OpenBusinessPlatformDbContext dbContext,
        FormVersion formVersion,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var ownerId = users["user.demo@company.test"].Id;
        var creatorId = users["builder.demo@company.test"].Id;

        foreach (var seed in DemoEmployeeRecords)
        {
            var exists = await dbContext.Records.AnyAsync(record => record.Id == seed.Id, cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.Records.Add(new FormRecord
            {
                Id = seed.Id,
                FormId = formVersion.FormId,
                FormVersionId = formVersion.Id,
                Status = RecordStatuses.Active,
                OwnerId = ownerId,
                DepartmentId = departments[seed.Department].Id,
                ValuesJson = SerializeToDocument(seed.ToValues()),
                CreatedById = creatorId
            });
        }
    }

    private static async Task EnsureBusinessPerformanceRecordsAsync(
        OpenBusinessPlatformDbContext dbContext,
        FormVersion formVersion,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var creatorId = users["builder.demo@company.test"].Id;
        var owners = new[] { "Alex Morgan", "Jordan Lee", "Sam Patel", "Taylor Chen" };
        var categories = new[] { "Product", "Service", "Subscription" };
        var regions = new[] { "North", "South", "East", "West" };
        var priorities = new[] { "Low", "Medium", "High" };
        var statuses = new[] { "active", "pending", "approved", "closed" };
        var existingIds = (await dbContext.Records.Where(record => record.FormId == formVersion.FormId).Select(record => record.Id).ToArrayAsync(cancellationToken)).ToHashSet();
        for (var index = 0; index < 48; index++)
        {
            var id = Guid.Parse($"12000000-0000-0000-0000-{index + 1:000000000000}");
            if (existingIds.Contains(id)) continue;
            // The current generic trend engine groups exact dates. A shared monthly date keeps this
            // deterministic fixture at twelve meaningful points without sample-only analytics logic.
            var eventDate = new DateTimeOffset(2025, index / 4 + 1, 15, 12, 0, 0, TimeSpan.Zero);
            var amount = 1000m + index * 125m + (index % 4) * 250m;
            dbContext.Records.Add(new FormRecord
            {
                Id = id,
                FormId = formVersion.FormId,
                FormVersionId = formVersion.Id,
                Status = statuses[index % statuses.Length],
                OwnerId = creatorId,
                DepartmentId = departments[DemoDepartments[index % DemoDepartments.Count].Name].Id,
                CreatedAt = eventDate.AddDays(index % 4),
                CreatedById = creatorId,
                ValuesJson = SerializeToDocument(new Dictionary<string, object?>
                {
                    ["title"] = $"Business item {index + 1:00}",
                    ["category"] = categories[index % categories.Length],
                    ["region"] = regions[index % regions.Length],
                    ["priority"] = priorities[index % priorities.Length],
                    ["amount"] = amount,
                    ["event_date"] = eventDate.ToString("yyyy-MM-dd"),
                    ["owner_name"] = owners[index % owners.Length]
                })
            });
        }
    }

    private static async Task EnsureOperationalPerformanceRecordsAsync(
        OpenBusinessPlatformDbContext dbContext,
        FormVersion formVersion,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var creatorId = users["builder.demo@company.test"].Id;
        var modules = new[] { "Loss", "Production", "Engineering", "Supply Chain", "QAQC" };
        var metrics = new[] { "Total Loss", "Manufacturing Loss", "OEE", "Utilities", "Inventory Accuracy", "First-Time Release" };
        var products = new[] { "Classic", "Premium", "Light", "Specialty" };
        var equipment = new[] { "Line 1", "Line 2", "Dryer", "Packaging" };
        var periodTypes = new[] { "Month", "Week", "Quarter" };
        var statuses = new[] { "active", "approved", "pending", "closed" };
        var existingIds = (await dbContext.Records.Where(record => record.FormId == formVersion.FormId).Select(record => record.Id).ToArrayAsync(cancellationToken)).ToHashSet();
        for (var index = 0; index < 72; index++)
        {
            var id = Guid.Parse($"13000000-0000-0000-0000-{index + 1:000000000000}");
            if (existingIds.Contains(id)) continue;
            var period = index % 12 + 1;
            var year = index < 36 ? 2025 : 2026;
            var date = new DateTimeOffset(year, period, 15, 12, 0, 0, TimeSpan.Zero);
            var target = 75m + (index % 8) * 5m;
            var actual = target + (index % 5 - 2) * 3m;
            dbContext.Records.Add(new FormRecord
            {
                Id = id, FormId = formVersion.FormId, FormVersionId = formVersion.Id, Status = statuses[index % statuses.Length],
                OwnerId = creatorId, DepartmentId = departments["Operations"].Id, CreatedAt = date, CreatedById = creatorId,
                ValuesJson = SerializeToDocument(new Dictionary<string, object?>
                {
                    ["title"] = $"Operational fact {index + 1:00}", ["module"] = modules[index % modules.Length], ["metric_key"] = metrics[index % metrics.Length],
                    ["fiscal_year"] = year.ToString(), ["period_type"] = periodTypes[index % periodTypes.Length], ["period_label"] = $"{year}-{period:00}",
                    ["period_number"] = period, ["period_date"] = date.ToString("yyyy-MM-dd"), ["product"] = products[index % products.Length],
                    ["equipment"] = equipment[index % equipment.Length], ["actual_value"] = actual, ["target_value"] = target, ["budget_value"] = target + 4m,
                    ["numerator"] = actual * 10m, ["denominator"] = target * 10m, ["unit"] = index % 3 == 0 ? "%" : "t"
                })
            });
        }
    }

    private static async Task EnsureHseIncidentRecordsAsync(
        OpenBusinessPlatformDbContext dbContext,
        FormVersion formVersion,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var creatorId = users["builder.demo@company.test"].Id;
        var types = new[] { "Near miss", "First aid", "Medical treatment", "Lost time" };
        var severities = new[] { "Low", "Medium", "High", "Critical" };
        var locations = new[] { "Receiving", "Processing", "Packaging", "Warehouse" };
        var bodyParts = new[] { "None", "Hand", "Back", "Eye", "Foot" };
        var statuses = new[] { "active", "pending", "approved", "closed" };
        var existingIds = (await dbContext.Records.Where(record => record.FormId == formVersion.FormId).Select(record => record.Id).ToArrayAsync(cancellationToken)).ToHashSet();
        for (var index = 0; index < 36; index++)
        {
            var id = Guid.Parse($"14000000-0000-0000-0000-{index + 1:000000000000}");
            if (existingIds.Contains(id)) continue;
            var date = new DateTimeOffset(2026, index % 12 + 1, index % 20 + 1, 12, 0, 0, TimeSpan.Zero);
            var lostHours = index % 4 == 3 ? (index % 6 + 1) * 4 : 0;
            dbContext.Records.Add(new FormRecord
            {
                Id = id, FormId = formVersion.FormId, FormVersionId = formVersion.Id, Status = statuses[index % statuses.Length],
                OwnerId = creatorId, DepartmentId = departments["Operations"].Id, CreatedAt = date, CreatedById = creatorId,
                ValuesJson = SerializeToDocument(new Dictionary<string, object?>
                {
                    ["title"] = $"HSE incident {index + 1:00}", ["incident_date"] = date.ToString("yyyy-MM-dd"), ["incident_type"] = types[index % types.Length],
                    ["severity"] = severities[index % severities.Length], ["location"] = locations[index % locations.Length], ["body_part"] = bodyParts[index % bodyParts.Length],
                    ["lost_hours"] = lostHours, ["lost_shifts"] = lostHours / 8, ["incident_cost"] = 250m + index * 125m,
                    ["training_team"] = $"Team {index % 4 + 1}", ["training_assigned"] = 12 + index % 7, ["training_completed"] = 10 + index % 6
                })
            });
        }
    }

    private static async Task EnsureBusinessPerformanceDashboardAsync(
        OpenBusinessPlatformDbContext dbContext,
        FormVersion sourceVersion,
        FormVersion operationsVersion,
        FormVersion incidentVersion,
        IReadOnlyDictionary<string, User> users,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Dashboards.AnyAsync(item => item.Id == BusinessPerformanceDashboardId, cancellationToken)) return;
        var sourceSchema = sourceVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
        var requiredFields = new[] { "title", "category", "region", "priority", "amount", "event_date", "status", "created_at" };
        if (sourceSchema is null || !requiredFields.All(FormReportableFieldMetadata.GetReportableFieldsById(sourceSchema).ContainsKey)) return;
        var sourceFormId = sourceVersion.FormId;
        var operationsFormId = operationsVersion.FormId;
        var incidentFormId = incidentVersion.FormId;
        var sections = new[]
        {
            new SavedDashboardSectionDefinition("sample-executive", "Executive Overview", 0, "gauge"),
            new SavedDashboardSectionDefinition("sample-financial", "Financial Performance", 1, "badge-dollar-sign"),
            new SavedDashboardSectionDefinition("sample-loss", "Loss", 2, "trending-up"),
            new SavedDashboardSectionDefinition("sample-production", "Production", 3, "factory"),
            new SavedDashboardSectionDefinition("sample-engineering", "Engineering", 4, "wrench"),
            new SavedDashboardSectionDefinition("sample-supply", "Supply Chain", 5, "package-check"),
            new SavedDashboardSectionDefinition("sample-qaqc", "QAQC", 6, "shield-check"),
            new SavedDashboardSectionDefinition("sample-hse", "HSE", 7, "heart-pulse"),
            new SavedDashboardSectionDefinition("sample-trends", "Trends & Targets", 8, "chart-column"),
            new SavedDashboardSectionDefinition("sample-records", "Records & Drill-down", 9, "clipboard-list"),
            new SavedDashboardSectionDefinition("sample-health", "Data Health", 10, "activity")
        };
        var widgetSpecs = new[]
        {
            ("total-records", "Total records", "sample-executive", DashboardWidgetWidths.Small, sourceFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Count, (string?)null, (string?)null, (string?)null, Array.Empty<string>()),
            ("total-amount", "Total amount", "sample-executive", DashboardWidgetWidths.Small, sourceFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "amount", null, null, Array.Empty<string>()),
            ("average-amount", "Average amount", "sample-executive", DashboardWidgetWidths.Small, sourceFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Average, "amount", null, null, Array.Empty<string>()),
            ("records-by-status", "Records by status", "sample-executive", DashboardWidgetWidths.Medium, sourceFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Count, null, "status", null, Array.Empty<string>()),
            ("amount-category", "Amount by category", "sample-financial", DashboardWidgetWidths.Wide, sourceFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "amount", "category", null, Array.Empty<string>()),
            ("amount-time", "Amount over time", "sample-financial", DashboardWidgetWidths.Wide, sourceFormId, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Sum, "amount", null, "event_date", Array.Empty<string>()),
            ("loss-actual", "Total loss actual", "sample-loss", DashboardWidgetWidths.Small, operationsFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, null, Array.Empty<string>()),
            ("loss-metric", "Loss by metric", "sample-loss", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "actual_value", "metric_key", null, Array.Empty<string>()),
            ("production-product", "Production by product", "sample-production", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "actual_value", "product", null, Array.Empty<string>()),
            ("production-trend", "Production trend", "sample-production", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, "period_date", Array.Empty<string>()),
            ("engineering-equipment", "Engineering performance", "sample-engineering", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Average, "actual_value", "equipment", null, Array.Empty<string>()),
            ("engineering-trend", "Utilities and reliability trend", "sample-engineering", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Average, "actual_value", null, "period_date", Array.Empty<string>()),
            ("supply-product", "Inventory by product", "sample-supply", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Sum, "actual_value", "product", null, Array.Empty<string>()),
            ("supply-metric", "Supply-chain KPI families", "sample-supply", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Average, "actual_value", "metric_key", null, Array.Empty<string>()),
            ("qaqc-rate", "QAQC first-time release", "sample-qaqc", DashboardWidgetWidths.Small, operationsFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Average, "actual_value", null, null, Array.Empty<string>()),
            ("qaqc-detail", "Quality detail", "sample-qaqc", DashboardWidgetWidths.Full, operationsFormId, ChartWidgetTypes.Table, DashboardAnalyticsMetricTypes.Count, null, null, null, new[] { "period_label", "metric_key", "product", "actual_value", "target_value", "unit", "status" }),
            ("incident-count", "YTD incidents", "sample-hse", DashboardWidgetWidths.Small, incidentFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Count, null, null, null, Array.Empty<string>()),
            ("incident-cost", "Incident cost", "sample-hse", DashboardWidgetWidths.Small, incidentFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "incident_cost", null, null, Array.Empty<string>()),
            ("lost-hours", "Lost hours", "sample-hse", DashboardWidgetWidths.Small, incidentFormId, ChartWidgetTypes.NumberCard, DashboardAnalyticsMetricTypes.Sum, "lost_hours", null, null, Array.Empty<string>()),
            ("incidents-location", "Incidents by location", "sample-hse", DashboardWidgetWidths.Wide, incidentFormId, ChartWidgetTypes.ChoiceBreakdown, DashboardAnalyticsMetricTypes.Count, null, "location", null, Array.Empty<string>()),
            ("operations-time", "Operational actual over time", "sample-trends", DashboardWidgetWidths.Wide, operationsFormId, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Sum, "actual_value", null, "period_date", Array.Empty<string>()),
            ("business-time", "Business records over time", "sample-trends", DashboardWidgetWidths.Wide, sourceFormId, ChartWidgetTypes.DateTrend, DashboardAnalyticsMetricTypes.Count, null, null, "event_date", Array.Empty<string>()),
            ("recent-business", "Recent business records", "sample-records", DashboardWidgetWidths.Full, sourceFormId, ChartWidgetTypes.Table, DashboardAnalyticsMetricTypes.Count, null, null, null, new[] { "title", "category", "region", "priority", "amount", "status", "created_at" }),
            ("recent-operations", "Operational detail", "sample-records", DashboardWidgetWidths.Full, operationsFormId, ChartWidgetTypes.Table, DashboardAnalyticsMetricTypes.Count, null, null, null, new[] { "module", "metric_key", "period_label", "product", "actual_value", "target_value", "unit" })
        };
        var widgets = widgetSpecs.Select(spec => new SavedDashboardWidgetDefinition(
            $"sample-{spec.Item1}", spec.Item2, spec.Item5,
            new ChartWidgetConfigDefinition(spec.Item6, new ChartMetricDefinition(spec.Item7, spec.Item8), spec.Item9, spec.Item10, spec.Item11, spec.Item6 == ChartWidgetTypes.Table ? 20 : 12, null),
            spec.Item3)).ToArray();
        var adapterSpecs = new[]
        {
            ("executive-target", "Actual versus target", "sample-executive", DashboardWidgetWidths.Wide, "target_attainment"),
            ("finance-delta", "Net performance versus budget", "sample-financial", DashboardWidgetWidths.Small, "kpi_delta"),
            ("finance-waterfall", "Profitability waterfall", "sample-financial", DashboardWidgetWidths.Wide, "waterfall"),
            ("finance-heatmap", "Channel and product heatmap", "sample-financial", DashboardWidgetWidths.Wide, "heatmap"),
            ("loss-target", "Loss actual and standard", "sample-loss", DashboardWidgetWidths.Wide, "combo"),
            ("production-stack", "Product composition", "sample-production", DashboardWidgetWidths.Wide, "stacked_bar"),
            ("engineering-target", "Actual versus engineering standard", "sample-engineering", DashboardWidgetWidths.Wide, "target_line"),
            ("supply-attainment", "Service-level attainment", "sample-supply", DashboardWidgetWidths.Medium, "target_attainment"),
            ("incident-donut", "Incident location mix", "sample-hse", DashboardWidgetWidths.Medium, "donut"),
            ("actual-budget", "Actual and budget comparison", "sample-trends", DashboardWidgetWidths.Wide, "combo"),
            ("period-diagnostic", "Actual-through period coverage", "sample-trends", DashboardWidgetWidths.Medium, "status_panel"),
            ("detail-popup", "Period detail preview", "sample-records", DashboardWidgetWidths.Wide, "detail_popup"),
            ("source-health", "Source health", "sample-health", DashboardWidgetWidths.Wide, "data_health"),
            ("schema-health", "Schema and permissions", "sample-health", DashboardWidgetWidths.Wide, "status_panel")
        };
        var adapterSettings = new Dictionary<string, object?> { ["actual"] = 92, ["target"] = 100, ["labels"] = "Jan|Feb|Mar|Apr", ["values"] = "31|35|39|42", ["primary"] = "31|35|39|42", ["secondary"] = "30|34|38|43", ["unit"] = "%", ["status"] = "success", ["title"] = "Sample data ready", ["detail"] = "Three permitted deterministic sources validated.", ["sourceLabel"] = "Development sample sources" };
        widgets = widgets.Concat(adapterSpecs.Select(spec => new SavedDashboardWidgetDefinition($"sample-{spec.Item1}", spec.Item2, null, null, spec.Item3, new DashboardAdapterWidgetDefinition("sample-dashboard", spec.Item5, adapterSettings)))).ToArray();
        var filters = new[]
        {
            new SavedDashboardFilterDefinition("sample-filter-date", "Date range", "date_range", sourceFormId, "event_date"),
            new SavedDashboardFilterDefinition("sample-filter-status", "Status", "record_status", sourceFormId, "status", new[] { "active", "pending", "approved", "closed" }),
            new SavedDashboardFilterDefinition("sample-filter-category", "Category", "single_select", sourceFormId, "category", new[] { "Product", "Service", "Subscription" }),
            new SavedDashboardFilterDefinition("sample-filter-region", "Region", "single_select", sourceFormId, "region", new[] { "North", "South", "East", "West" })
            ,new SavedDashboardFilterDefinition("sample-filter-year", "Fiscal year", "single_select", operationsFormId, "fiscal_year", new[] { "2025", "2026" })
            ,new SavedDashboardFilterDefinition("sample-filter-period", "Period", "single_select", operationsFormId, "period_type", new[] { "Week", "Month", "Quarter" })
            ,new SavedDashboardFilterDefinition("sample-filter-product", "Product / recipe", "multi_select", operationsFormId, "product", new[] { "Classic", "Premium", "Light", "Specialty" })
            ,new SavedDashboardFilterDefinition("sample-filter-location", "HSE location", "single_select", incidentFormId, "location", new[] { "Receiving", "Processing", "Packaging", "Warehouse" })
        };
        var config = new SavedDashboardConfigDefinition(1, widgets, sections,
            new DashboardTemplateProvenanceDefinition("business-performance-sample", 2, new DateTimeOffset(2026, 8, 21, 0, 0, 0, TimeSpan.Zero)), filters);
        var layoutItems = widgetSpecs.Select((spec, index) => new SavedDashboardWidgetLayoutDefinition($"sample-{spec.Item1}", spec.Item4, index))
            .Concat(adapterSpecs.Select((spec, index) => new SavedDashboardWidgetLayoutDefinition($"sample-{spec.Item1}", spec.Item4, widgetSpecs.Length + index))).ToArray();
        var layout = new SavedDashboardLayoutDefinition(1, layoutItems);
        var creatorId = users["builder.demo@company.test"].Id;
        dbContext.Dashboards.Add(new DashboardDefinition
        {
            Id = BusinessPerformanceDashboardId,
            Name = "Business Performance Sample",
            Description = "A comprehensive reference dashboard using the standard permission-filtered analytics engine.",
            Status = DashboardPublicationStatuses.Published,
            Slug = "business-performance-sample",
            ShowInNavigation = false,
            PublishedAt = new DateTimeOffset(2026, 8, 21, 0, 0, 0, TimeSpan.Zero),
            PublishedById = creatorId,
            CreatedById = creatorId,
            ConfigJson = SerializeToDocument(config),
            LayoutJson = SerializeToDocument(layout),
            ExtraPropertiesJson = DashboardDefinitionAccess.SerializeSettings(new DashboardSettingsDefinition(DashboardVisibilityModes.Workspace, false))
        });
        dbContext.AuditLogs.AddRange(
            new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "Dashboard", EntityId = BusinessPerformanceDashboardId, Action = "dashboard_created", UserId = creatorId },
            new AuditLogEntry { Id = Guid.NewGuid(), EntityType = "Dashboard", EntityId = BusinessPerformanceDashboardId, Action = "dashboard_published", UserId = creatorId });
    }

    private static FormLayoutRowDefinition CreateTwoColumnRow(string id, string leftFieldId, string rightFieldId)
    {
        return new FormLayoutRowDefinition(
            id,
            new[]
            {
                new FormLayoutColumnDefinition($"{id}_left", new ResponsiveSpanDefinition(12, 6, 6), new[] { leftFieldId }),
                new FormLayoutColumnDefinition($"{id}_right", new ResponsiveSpanDefinition(12, 6, 6), new[] { rightFieldId })
            });
    }

    private static JsonDocument SerializeToDocument<T>(T value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static string NormalizeOptionId(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
    }
}

public sealed record DemoDepartmentDefinition(Guid Id, string Name);

public sealed record DemoUserDefinition(
    Guid Id,
    string Name,
    string Email,
    string RoleName,
    string DepartmentName);

public sealed record DemoEmployeeRecordDefinition(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Department,
    string StartDate,
    string EmploymentType,
    string Notes)
{
    public IReadOnlyDictionary<string, object?> ToValues()
    {
        return new Dictionary<string, object?>
        {
            ["first_name"] = FirstName,
            ["last_name"] = LastName,
            ["email"] = Email,
            ["phone"] = Phone,
            ["department"] = Department,
            ["start_date"] = StartDate,
            ["employment_type"] = EmploymentType,
            ["notes"] = Notes
        };
    }
}
