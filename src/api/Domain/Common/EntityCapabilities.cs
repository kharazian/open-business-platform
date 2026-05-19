using System.Text.Json;

namespace OpenBusinessPlatform.Api.Domain.Common;

public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}

public interface IHasExtraProperties
{
    JsonDocument? ExtraPropertiesJson { get; set; }
}

public interface IIsActive
{
    bool IsActive { get; set; }
}

public interface IHasSortOrder
{
    int SortOrder { get; set; }
}
