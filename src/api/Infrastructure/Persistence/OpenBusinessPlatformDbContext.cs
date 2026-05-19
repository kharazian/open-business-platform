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

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RoleFormPermission> RoleFormPermissions => Set<RoleFormPermission>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();

    public DbSet<FormDefinition> Forms => Set<FormDefinition>();

    public DbSet<FormVersion> FormVersions => Set<FormVersion>();

    public DbSet<FormRecord> Records => Set<FormRecord>();

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
        ConfigureForms(modelBuilder);
        ConfigureRecords(modelBuilder);
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
            entity.HasIndex(record => record.CreatedById);
            entity.HasIndex(record => record.CreatedAt);
            entity.Property(record => record.FormId).HasColumnName("form_id").HasColumnType("uuid").IsRequired();
            entity.Property(record => record.FormVersionId).HasColumnName("form_version_id").HasColumnType("uuid").IsRequired();
            entity.Property(record => record.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
            entity.Property(record => record.OwnerId).HasColumnName("owner_id").HasColumnType("uuid");
            entity.Property(record => record.DepartmentId).HasColumnName("department_id").HasColumnType("uuid");
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
