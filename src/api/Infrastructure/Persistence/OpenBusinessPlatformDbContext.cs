using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenBusinessPlatform.Api.Domain.Common;
using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence;

public sealed class OpenBusinessPlatformDbContext : DbContext
{
    public OpenBusinessPlatformDbContext(DbContextOptions<OpenBusinessPlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RoleFormPermission> RoleFormPermissions => Set<RoleFormPermission>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<RoleReportPermission> RoleReportPermissions => Set<RoleReportPermission>();

    public DbSet<RoleFieldPermission> RoleFieldPermissions => Set<RoleFieldPermission>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();

    public DbSet<FormDefinition> Forms => Set<FormDefinition>();

    public DbSet<FormVersion> FormVersions => Set<FormVersion>();

    public DbSet<FormRecord> Records => Set<FormRecord>();

    public DbSet<ReportDefinition> Reports => Set<ReportDefinition>();

    public DbSet<DashboardDefinition> Dashboards => Set<DashboardDefinition>();

    public DbSet<TriggerDefinition> Triggers => Set<TriggerDefinition>();

    public DbSet<TriggerExecutionLog> TriggerLogs => Set<TriggerExecutionLog>();

    public DbSet<WorkflowDefinition> Workflows => Set<WorkflowDefinition>();

    public DbSet<WorkflowDefinitionVersion> WorkflowVersions => Set<WorkflowDefinitionVersion>();

    public DbSet<WorkflowHistoryEntry> WorkflowHistory => Set<WorkflowHistoryEntry>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    public override int SaveChanges()
    {
        ApplyEntityConventions();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyEntityConventions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyEntityConventions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyEntityConventions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureGroups(modelBuilder);
        ConfigureForms(modelBuilder);
        ConfigureRecords(modelBuilder);
        ConfigureReports(modelBuilder);
        ConfigureDashboards(modelBuilder);
        ConfigureTriggers(modelBuilder);
        ConfigureWorkflows(modelBuilder);
        ConfigureNotifications(modelBuilder);
        ConfigureNotificationPreferences(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
    }

    private void ApplyEntityConventions()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Entity is Entity<Guid> entity && entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            ApplyAuditConventions(entry, now);
            ApplyConcurrencyConvention(entry);
        }
    }

    private static void ApplyAuditConventions(EntityEntry entry, DateTimeOffset now)
    {
        if (entry.State == EntityState.Added && entry.Entity is IHasCreationTime creationTimed)
        {
            creationTimed.CreatedAt = now;
        }

        if (entry.State == EntityState.Modified && entry.Entity is IModificationAudited modificationAudited)
        {
            modificationAudited.UpdatedAt = now;
        }

        if (entry.State == EntityState.Deleted && entry.Entity is IDeletionAudited deletionAudited)
        {
            entry.State = EntityState.Modified;
            deletionAudited.IsDeleted = true;
            deletionAudited.DeletedAt = now;

            if (entry.Entity is IModificationAudited deletedModificationAudited)
            {
                deletedModificationAudited.UpdatedAt = now;
            }
        }
    }

    private static void ApplyConcurrencyConvention(EntityEntry entry)
    {
        if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && entry.Entity is IHasConcurrencyStamp concurrencyStamped)
        {
            concurrencyStamped.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            ConfigureAuditedAggregateRoot(entity, "users");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => new { user.ExternalProvider, user.ExternalUserId });
            entity.Property(user => user.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            entity.Property(user => user.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(user => user.ExternalProvider).HasColumnName("external_provider").HasMaxLength(80);
            entity.Property(user => user.ExternalUserId).HasColumnName("external_user_id").HasMaxLength(256);
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(512);
            entity.Property(user => user.PasswordUpdatedAt).HasColumnName("password_updated_at");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.ExpiresAt);
            entity.Property(token => token.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(token => token.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(token => token.UsedAt).HasColumnName("used_at");
            entity.Property(token => token.CreatedIp).HasColumnName("created_ip").HasMaxLength(80);
            entity.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();
            entity
                .HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            ConfigureAuditedAggregateRoot(entity, "roles");
            entity.HasIndex(role => role.Name).IsUnique();
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(role => role.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(role => role.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.Property(userRole => userRole.UserId).HasColumnName("user_id").HasColumnType("uuid");
            entity.Property(userRole => userRole.RoleId).HasColumnName("role_id").HasColumnType("uuid");
            entity
                .HasOne(userRole => userRole.User)
                .WithMany(user => user.Roles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(userRole => userRole.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(rolePermission => rolePermission.Id);
            entity.HasIndex(rolePermission => rolePermission.RoleId);
            entity.HasIndex(rolePermission => new { rolePermission.RoleId, rolePermission.Permission }).IsUnique();
            entity.Property(rolePermission => rolePermission.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(rolePermission => rolePermission.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
            entity.Property(rolePermission => rolePermission.Permission).HasColumnName("permission").HasMaxLength(160).IsRequired();
            entity
                .HasOne(rolePermission => rolePermission.Role)
                .WithMany(role => role.Permissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleFormPermission>(entity =>
        {
            entity.ToTable("role_form_permissions");
            entity.HasKey(roleFormPermission => roleFormPermission.Id);
            entity.HasIndex(roleFormPermission => roleFormPermission.RoleId);
            entity.HasIndex(roleFormPermission => roleFormPermission.FormId);
            entity.HasIndex(roleFormPermission => new { roleFormPermission.RoleId, roleFormPermission.FormId, roleFormPermission.Action }).IsUnique();
            entity.Property(roleFormPermission => roleFormPermission.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(roleFormPermission => roleFormPermission.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
            entity.Property(roleFormPermission => roleFormPermission.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(roleFormPermission => roleFormPermission.Action).HasColumnName("action").HasMaxLength(40).IsRequired();
            entity.Property(roleFormPermission => roleFormPermission.Scope).HasColumnName("scope").HasMaxLength(40).HasDefaultValue("all").IsRequired();
            entity
                .HasOne(roleFormPermission => roleFormPermission.Role)
                .WithMany(role => role.FormPermissions)
                .HasForeignKey(roleFormPermission => roleFormPermission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(roleFormPermission => roleFormPermission.Form)
                .WithMany()
                .HasForeignKey(roleFormPermission => roleFormPermission.FormId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            ConfigureAuditedAggregateRoot(entity, "departments");
            entity.HasIndex(department => department.ParentDepartmentId);
            entity.HasIndex(department => department.ManagerUserId);
            entity.Property(department => department.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(department => department.ParentDepartmentId).HasColumnName("parent_department_id").HasColumnType("uuid");
            entity.Property(department => department.ManagerUserId).HasColumnName("manager_user_id").HasColumnType("uuid");
            entity.Property(department => department.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity
                .HasOne(department => department.ParentDepartment)
                .WithMany(department => department.ChildDepartments)
                .HasForeignKey(department => department.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(department => department.ManagerUser)
                .WithMany()
                .HasForeignKey(department => department.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserDepartment>(entity =>
        {
            entity.ToTable("user_departments");
            entity.HasKey(userDepartment => new { userDepartment.UserId, userDepartment.DepartmentId });
            entity.Property(userDepartment => userDepartment.UserId).HasColumnName("user_id").HasColumnType("uuid");
            entity.Property(userDepartment => userDepartment.DepartmentId).HasColumnName("department_id").HasColumnType("uuid");
            entity.Property(userDepartment => userDepartment.IsPrimary).HasColumnName("is_primary");
            entity
                .HasOne(userDepartment => userDepartment.User)
                .WithMany(user => user.Departments)
                .HasForeignKey(userDepartment => userDepartment.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(userDepartment => userDepartment.Department)
                .WithMany(department => department.Users)
                .HasForeignKey(userDepartment => userDepartment.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureGroups(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {
            ConfigureAuditedAggregateRoot(entity, "groups");
            entity.HasIndex(group => group.Name).IsUnique();
            entity.Property(group => group.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(group => group.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(group => group.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("user_groups");
            entity.HasKey(userGroup => userGroup.Id);
            entity.HasIndex(userGroup => userGroup.UserId);
            entity.HasIndex(userGroup => userGroup.GroupId);
            entity.HasIndex(userGroup => new { userGroup.UserId, userGroup.GroupId }).IsUnique();
            entity.Property(userGroup => userGroup.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(userGroup => userGroup.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
            entity.Property(userGroup => userGroup.GroupId).HasColumnName("group_id").HasColumnType("uuid").IsRequired();
            entity
                .HasOne(userGroup => userGroup.User)
                .WithMany(user => user.Groups)
                .HasForeignKey(userGroup => userGroup.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(userGroup => userGroup.Group)
                .WithMany(group => group.Users)
                .HasForeignKey(userGroup => userGroup.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleReportPermission>(entity =>
        {
            entity.ToTable("role_report_permissions");
            entity.HasKey(permission => permission.Id);
            entity.HasIndex(permission => permission.RoleId);
            entity.HasIndex(permission => permission.ReportId);
            entity.HasIndex(permission => new { permission.RoleId, permission.ReportId, permission.Action }).IsUnique();
            entity.Property(permission => permission.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(permission => permission.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
            entity.Property(permission => permission.ReportId).HasColumnName("report_id").HasColumnType("uuid").IsRequired();
            entity.Property(permission => permission.Action).HasColumnName("action").HasMaxLength(40).IsRequired();
            entity
                .HasOne(permission => permission.Role)
                .WithMany(role => role.ReportPermissions)
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(permission => permission.Report)
                .WithMany()
                .HasForeignKey(permission => permission.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleFieldPermission>(entity =>
        {
            entity.ToTable("role_field_permissions");
            entity.HasKey(permission => permission.Id);
            entity.HasIndex(permission => permission.RoleId);
            entity.HasIndex(permission => permission.FormId);
            entity.HasIndex(permission => new { permission.RoleId, permission.FormId, permission.FieldId }).IsUnique();
            entity.Property(permission => permission.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(permission => permission.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
            entity.Property(permission => permission.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(permission => permission.FieldId).HasColumnName("field_id").HasMaxLength(120).IsRequired();
            entity.Property(permission => permission.Access).HasColumnName("access").HasMaxLength(40).IsRequired();
            entity
                .HasOne(permission => permission.Role)
                .WithMany(role => role.FieldPermissions)
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(permission => permission.Form)
                .WithMany()
                .HasForeignKey(permission => permission.FormId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureForms(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormDefinition>(entity =>
        {
            ConfigureFullAuditedAggregateRoot(entity, "forms");
            entity.HasIndex(form => form.Status);
            entity.HasIndex(form => form.CreatedById);
            entity.Property(form => form.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(form => form.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(form => form.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
            entity.Property(form => form.CurrentVersionId).HasColumnName("current_version_id").HasColumnType("uuid");
            entity.Property(form => form.DraftSchemaJson).HasColumnName("draft_schema_json").HasColumnType("jsonb");
            entity
                .HasOne(form => form.CurrentVersion)
                .WithMany()
                .HasForeignKey(form => form.CurrentVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FormVersion>(entity =>
        {
            ConfigureCreationAuditedEntity(entity, "form_versions");
            entity.HasIndex(version => new { version.FormId, version.VersionNumber }).IsUnique();
            entity.Property(version => version.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(version => version.VersionNumber).HasColumnName("version_number").IsRequired();
            entity.Property(version => version.SchemaJson).HasColumnName("schema_json").HasColumnType("jsonb").IsRequired();
            entity.Property(version => version.LayoutJson).HasColumnName("layout_json").HasColumnType("jsonb");
            entity.Property(version => version.ValidationJson).HasColumnName("validation_json").HasColumnType("jsonb");
            entity.Property(version => version.PublishedById).HasColumnName("published_by_id").HasColumnType("uuid");
            entity.Property(version => version.PublishedAt).HasColumnName("published_at");
            entity
                .HasOne(version => version.Form)
                .WithMany(form => form.Versions)
                .HasForeignKey(version => version.FormId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRecords(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormRecord>(entity =>
        {
            ConfigureFullAuditedAggregateRoot(entity, "records");
            entity.HasIndex(record => record.FormId);
            entity.HasIndex(record => record.FormVersionId);
            entity.HasIndex(record => record.Status);
            entity.HasIndex(record => record.OwnerId);
            entity.HasIndex(record => record.DepartmentId);
            entity.HasIndex(record => record.AssignedToUserId);
            entity.HasIndex(record => record.AssignedGroupId);
            entity.HasIndex(record => record.CreatedById);
            entity.HasIndex(record => record.CreatedAt);
            entity.Property(record => record.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(record => record.FormVersionId).HasColumnName("form_version_id").HasColumnType("uuid").IsRequired();
            entity.Property(record => record.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
            entity.Property(record => record.OwnerId).HasColumnName("owner_id").HasColumnType("uuid");
            entity.Property(record => record.DepartmentId).HasColumnName("department_id").HasColumnType("uuid");
            entity.Property(record => record.AssignedToUserId).HasColumnName("assigned_to_user_id").HasColumnType("uuid");
            entity.Property(record => record.AssignedGroupId).HasColumnName("assigned_group_id").HasColumnType("uuid");
            entity.Property(record => record.ValuesJson).HasColumnName("values_json").HasColumnType("jsonb").IsRequired();
            entity
                .HasOne(record => record.Form)
                .WithMany()
                .HasForeignKey(record => record.FormId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(record => record.FormVersion)
                .WithMany()
                .HasForeignKey(record => record.FormVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(record => record.Owner)
                .WithMany()
                .HasForeignKey(record => record.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity
                .HasOne(record => record.Department)
                .WithMany()
                .HasForeignKey(record => record.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity
                .HasOne(record => record.AssignedToUser)
                .WithMany()
                .HasForeignKey(record => record.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity
                .HasOne(record => record.AssignedGroup)
                .WithMany()
                .HasForeignKey(record => record.AssignedGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureReports(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportDefinition>(entity =>
        {
            ConfigureFullAuditedAggregateRoot(entity, "reports");
            entity.HasIndex(report => report.FormId);
            entity.HasIndex(report => report.Type);
            entity.HasIndex(report => report.CreatedById);
            entity.Property(report => report.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(report => report.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(report => report.Type).HasColumnName("type").HasMaxLength(40).IsRequired();
            entity.Property(report => report.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired();
            entity
                .HasOne(report => report.Form)
                .WithMany()
                .HasForeignKey(report => report.FormId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDashboards(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DashboardDefinition>(entity =>
        {
            ConfigureFullAuditedAggregateRoot(entity, "dashboards");
            entity.HasIndex(dashboard => dashboard.CreatedById);
            entity.HasIndex(dashboard => dashboard.Name);
            entity.Property(dashboard => dashboard.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(dashboard => dashboard.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(dashboard => dashboard.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired();
            entity.Property(dashboard => dashboard.LayoutJson).HasColumnName("layout_json").HasColumnType("jsonb").IsRequired();
        });
    }

    private static void ConfigureTriggers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TriggerDefinition>(entity =>
        {
            ConfigureFullAuditedAggregateRoot(entity, "triggers");
            entity.HasIndex(trigger => trigger.FormId);
            entity.HasIndex(trigger => trigger.EventName);
            entity.HasIndex(trigger => trigger.IsEnabled);
            entity.HasIndex(trigger => trigger.ScheduleNextRunAt);
            entity.Property(trigger => trigger.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(trigger => trigger.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(trigger => trigger.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(trigger => trigger.EventName).HasColumnName("event_name").HasMaxLength(80).IsRequired();
            entity.Property(trigger => trigger.ConditionsJson).HasColumnName("conditions_json").HasColumnType("jsonb").IsRequired();
            entity.Property(trigger => trigger.ActionsJson).HasColumnName("actions_json").HasColumnType("jsonb").IsRequired();
            entity.Property(trigger => trigger.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
            entity.Property(trigger => trigger.AutoRetryEnabled).HasColumnName("auto_retry_enabled").HasDefaultValue(true);
            entity.Property(trigger => trigger.AutoRetryMaxAttempts).HasColumnName("auto_retry_max_attempts").HasDefaultValue(3);
            entity.Property(trigger => trigger.AutoRetryDelaySeconds).HasColumnName("auto_retry_delay_seconds").HasDefaultValue(60);
            entity.Property(trigger => trigger.ScheduleJson).HasColumnName("schedule_json").HasColumnType("jsonb");
            entity.Property(trigger => trigger.ScheduleNextRunAt).HasColumnName("schedule_next_run_at");
            entity.Property(trigger => trigger.ScheduleLastRunAt).HasColumnName("schedule_last_run_at");
            entity
                .HasOne(trigger => trigger.Form)
                .WithMany()
                .HasForeignKey(trigger => trigger.FormId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TriggerExecutionLog>(entity =>
        {
            entity.ToTable("trigger_logs");
            entity.HasKey(log => log.Id);
            entity.HasIndex(log => log.TriggerId);
            entity.HasIndex(log => log.FormId);
            entity.HasIndex(log => log.EventName);
            entity.HasIndex(log => new { log.EntityType, log.EntityId });
            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => log.AutoRetryNextAttemptAt);
            entity.Property(log => log.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(log => log.TriggerId).HasColumnName("trigger_id").HasColumnType("uuid").IsRequired();
            entity.Property(log => log.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(log => log.EventName).HasColumnName("event_name").HasMaxLength(80).IsRequired();
            entity.Property(log => log.EntityType).HasColumnName("entity_type").HasMaxLength(80).IsRequired();
            entity.Property(log => log.EntityId).HasColumnName("entity_id").HasColumnType("uuid").IsRequired();
            entity.Property(log => log.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
            entity.Property(log => log.InputJson).HasColumnName("input_json").HasColumnType("jsonb").IsRequired();
            entity.Property(log => log.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
            entity.Property(log => log.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(log => log.StartedAt).HasColumnName("started_at").IsRequired();
            entity.Property(log => log.CompletedAt).HasColumnName("completed_at");
            entity.Property(log => log.AutoRetryAttemptCount).HasColumnName("auto_retry_attempt_count").HasDefaultValue(0);
            entity.Property(log => log.AutoRetryMaxAttempts).HasColumnName("auto_retry_max_attempts").HasDefaultValue(3);
            entity.Property(log => log.AutoRetryNextAttemptAt).HasColumnName("auto_retry_next_attempt_at");
            entity.Property(log => log.AutoRetryLockedAt).HasColumnName("auto_retry_locked_at");
            entity.Property(log => log.AutoRetryCompletedAt).HasColumnName("auto_retry_completed_at");
            entity.Property(log => log.AutoRetryExhaustedAt).HasColumnName("auto_retry_exhausted_at");
            entity.Property(log => log.AutoRetryDisabledAt).HasColumnName("auto_retry_disabled_at");
            entity.Property(log => log.CreatedAt).HasColumnName("created_at").IsRequired();
            entity
                .HasOne(log => log.Trigger)
                .WithMany()
                .HasForeignKey(log => log.TriggerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(log => log.Form)
                .WithMany()
                .HasForeignKey(log => log.FormId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.HasIndex(notification => notification.UserId);
            entity.HasIndex(notification => notification.ReadAt);
            entity.HasIndex(notification => notification.CreatedAt);
            entity.HasIndex(notification => new { notification.SourceType, notification.SourceId });
            entity.Property(notification => notification.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(notification => notification.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
            entity.Property(notification => notification.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.Body).HasColumnName("body").HasMaxLength(2000).IsRequired();
            entity.Property(notification => notification.SourceType).HasColumnName("source_type").HasMaxLength(80).IsRequired();
            entity.Property(notification => notification.SourceId).HasColumnName("source_id").HasColumnType("uuid");
            entity.Property(notification => notification.TriggerId).HasColumnName("trigger_id").HasColumnType("uuid");
            entity.Property(notification => notification.TriggerLogId).HasColumnName("trigger_log_id").HasColumnType("uuid");
            entity.Property(notification => notification.ActionId).HasColumnName("action_id").HasMaxLength(120);
            entity.Property(notification => notification.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(notification => notification.ReadAt).HasColumnName("read_at");
            entity.Property(notification => notification.CreatedAt).HasColumnName("created_at").IsRequired();
            entity
                .HasOne(notification => notification.User)
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureWorkflows(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            ConfigureFullAuditedAggregateRoot(entity, "workflow_definitions");
            entity.HasIndex(workflow => workflow.FormId);
            entity.HasIndex(workflow => workflow.Status);
            entity.HasIndex(workflow => workflow.IsEnabled);
            entity.Property(workflow => workflow.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(workflow => workflow.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(workflow => workflow.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(workflow => workflow.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
            entity.Property(workflow => workflow.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
            entity.Property(workflow => workflow.HasUnpublishedChanges).HasColumnName("has_unpublished_changes").HasDefaultValue(false);
            entity.Property(workflow => workflow.CurrentVersionId).HasColumnName("current_version_id").HasColumnType("uuid");
            entity.Property(workflow => workflow.DraftConfigJson).HasColumnName("draft_config_json").HasColumnType("jsonb").IsRequired();
            entity
                .HasOne(workflow => workflow.Form)
                .WithMany()
                .HasForeignKey(workflow => workflow.FormId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(workflow => workflow.CurrentVersion)
                .WithMany()
                .HasForeignKey(workflow => workflow.CurrentVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkflowDefinitionVersion>(entity =>
        {
            ConfigureCreationAuditedEntity(entity, "workflow_definition_versions");
            entity.HasIndex(version => version.WorkflowDefinitionId);
            entity.HasIndex(version => new { version.WorkflowDefinitionId, version.VersionNumber }).IsUnique();
            entity.Property(version => version.WorkflowDefinitionId).HasColumnName("workflow_definition_id").HasColumnType("uuid").IsRequired();
            entity.Property(version => version.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(version => version.VersionNumber).HasColumnName("version_number").IsRequired();
            entity.Property(version => version.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired();
            entity.Property(version => version.PublishedById).HasColumnName("published_by_id").HasColumnType("uuid");
            entity.Property(version => version.PublishedAt).HasColumnName("published_at");
            entity
                .HasOne(version => version.WorkflowDefinition)
                .WithMany(workflow => workflow.Versions)
                .HasForeignKey(version => version.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(version => version.Form)
                .WithMany()
                .HasForeignKey(version => version.FormId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkflowHistoryEntry>(entity =>
        {
            ConfigureCreationAuditedEntity(entity, "workflow_history");
            entity.HasIndex(history => history.WorkflowDefinitionId);
            entity.HasIndex(history => history.WorkflowDefinitionVersionId);
            entity.HasIndex(history => history.RecordId);
            entity.HasIndex(history => history.CreatedAt);
            entity.Property(history => history.WorkflowDefinitionId).HasColumnName("workflow_definition_id").HasColumnType("uuid").IsRequired();
            entity.Property(history => history.WorkflowDefinitionVersionId).HasColumnName("workflow_definition_version_id").HasColumnType("uuid").IsRequired();
            entity.Property(history => history.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(history => history.RecordId).HasColumnName("record_id").HasColumnType("uuid").IsRequired();
            entity.Property(history => history.FromStateKey).HasColumnName("from_state_key").HasMaxLength(80);
            entity.Property(history => history.ToStateKey).HasColumnName("to_state_key").HasMaxLength(80).IsRequired();
            entity.Property(history => history.TransitionKey).HasColumnName("transition_key").HasMaxLength(80);
            entity.Property(history => history.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
            entity.Property(history => history.ActorUserId).HasColumnName("actor_user_id").HasColumnType("uuid");
            entity.Property(history => history.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity
                .HasOne(history => history.WorkflowDefinition)
                .WithMany()
                .HasForeignKey(history => history.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(history => history.WorkflowDefinitionVersion)
                .WithMany()
                .HasForeignKey(history => history.WorkflowDefinitionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(history => history.Form)
                .WithMany()
                .HasForeignKey(history => history.FormId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(history => history.Record)
                .WithMany()
                .HasForeignKey(history => history.RecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotificationPreferences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.ToTable("notification_preferences");
            entity.HasKey(preference => preference.Id);
            entity.HasIndex(preference => preference.UserId).IsUnique();
            entity.HasIndex(preference => preference.UpdatedAt);
            entity.Property(preference => preference.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(preference => preference.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
            entity.Property(preference => preference.InAppEnabled).HasColumnName("in_app_enabled").IsRequired();
            entity.Property(preference => preference.ShowUnreadBadge).HasColumnName("show_unread_badge").IsRequired();
            entity.Property(preference => preference.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity
                .HasOne(preference => preference.User)
                .WithMany()
                .HasForeignKey(preference => preference.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(auditLog => auditLog.Id);
            entity.HasIndex(auditLog => new { auditLog.EntityType, auditLog.EntityId });
            entity.HasIndex(auditLog => auditLog.UserId);
            entity.HasIndex(auditLog => auditLog.CreatedAt);
            entity.Property(auditLog => auditLog.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(auditLog => auditLog.EntityType).HasColumnName("entity_type").HasMaxLength(80).IsRequired();
            entity.Property(auditLog => auditLog.EntityId).HasColumnName("entity_id").HasColumnType("uuid").IsRequired();
            entity.Property(auditLog => auditLog.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
            entity.Property(auditLog => auditLog.UserId).HasColumnName("user_id").HasColumnType("uuid");
            entity.Property(auditLog => auditLog.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
            entity.Property(auditLog => auditLog.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");
            entity.Property(auditLog => auditLog.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(auditLog => auditLog.CreatedAt).HasColumnName("created_at");
            entity
                .HasOne(auditLog => auditLog.User)
                .WithMany()
                .HasForeignKey(auditLog => auditLog.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureCreationAuditedEntity<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : CreationAuditedEntity<Guid>
    {
        entity.ToTable(tableName);
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id").HasColumnType("uuid");
        entity.Property(item => item.CreatedAt).HasColumnName("created_at");
        entity.Property(item => item.CreatedById).HasColumnName("created_by_id").HasColumnType("uuid");
    }

    private static void ConfigureAuditedAggregateRoot<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
    {
        entity.ToTable(tableName);
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id").HasColumnType("uuid");
        entity.Property(item => item.CreatedAt).HasColumnName("created_at");
        entity.Property(item => item.CreatedById).HasColumnName("created_by_id").HasColumnType("uuid");
        entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        entity.Property(item => item.UpdatedById).HasColumnName("updated_by_id").HasColumnType("uuid");
        entity.Property(item => item.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(40).IsRequired();
        entity.Property(item => item.ExtraPropertiesJson).HasColumnName("extra_properties_json").HasColumnType("jsonb");
    }

    private static void ConfigureFullAuditedAggregateRoot<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
    {
        ConfigureAuditedAggregateRoot(entity, tableName);
        entity.Property(item => item.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        entity.Property(item => item.DeletedAt).HasColumnName("deleted_at");
        entity.Property(item => item.DeletedById).HasColumnName("deleted_by_id").HasColumnType("uuid");
    }
}
