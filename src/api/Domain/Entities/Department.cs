using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class Department : AuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties, IIsActive
{
    public string Name { get; set; } = string.Empty;

    public Guid? ParentDepartmentId { get; set; }

    public Department? ParentDepartment { get; set; }

    public Guid? ManagerUserId { get; set; }

    public User? ManagerUser { get; set; }

    public bool IsActive { get; set; } = true;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }

    public ICollection<Department> ChildDepartments { get; } = new List<Department>();

    public ICollection<UserDepartment> Users { get; } = new List<UserDepartment>();
}
