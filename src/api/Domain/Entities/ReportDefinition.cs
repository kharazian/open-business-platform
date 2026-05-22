using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class ReportDefinition : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = ReportTypes.List;

    public JsonDocument ConfigJson { get; set; } = null!;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public static class ReportTypes
{
    public const string List = "list";
}
