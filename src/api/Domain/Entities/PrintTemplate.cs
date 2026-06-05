using System.Text.Json;
using OpenBusinessPlatform.Api.Domain.Common;

namespace OpenBusinessPlatform.Api.Domain.Entities;

public sealed class PrintTemplate : FullAuditedAggregateRoot<Guid>, IHasConcurrencyStamp, IHasExtraProperties
{
    public Guid FormId { get; set; }

    public FormDefinition? Form { get; set; }

    public Guid? ReportId { get; set; }

    public ReportDefinition? Report { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = PrintTemplateTypes.Record;

    public JsonDocument ConfigJson { get; set; } = null!;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public JsonDocument? ExtraPropertiesJson { get; set; }
}

public static class PrintTemplateTypes
{
    public const string Record = "record";
    public const string Report = "report";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Record,
        Report
    };
}
