using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Identity;

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public const string DemoUserPassword = "DemoUser!2026";

    public static readonly Guid EmployeeInformationFormId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid EmployeeInformationFormVersionId = Guid.Parse("10000000-0000-0000-0000-000000000002");

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
        await dbContext.SaveChangesAsync(cancellationToken);

        var formVersion = await EnsureEmployeeInformationFormAsync(dbContext, cancellationToken);
        await EnsureFormPermissionsAsync(dbContext, roles, formVersion.FormId, cancellationToken);
        await EnsureEmployeeRecordsAsync(dbContext, formVersion, users, departments, cancellationToken);

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
                PlatformPermissions.Reports.Manage
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
