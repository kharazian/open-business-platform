namespace OpenBusinessPlatform.Api.Modules.Forms;

public static class ReportableFieldSources
{
    public const string Form = "form";
    public const string System = "system";
}

public static class ReportableSystemFields
{
    public const string Status = "status";
    public const string CreatedAt = "created_at";
    public const string CreatedById = "created_by_id";
    public const string UpdatedAt = "updated_at";
    public const string UpdatedById = "updated_by_id";
    public const string OwnerId = "owner_id";
    public const string DepartmentId = "department_id";
}

public sealed record ReportableFieldOptionMetadata(string Id, string Label, string Value);

public sealed record ReportableFieldMetadata(
    string Id,
    string Label,
    string Type,
    string Source,
    IReadOnlyList<ReportableFieldOptionMetadata> Options,
    bool Filterable,
    bool Sortable,
    bool Searchable,
    bool SupportsAggregation,
    bool SupportsChoiceGrouping);

public static class FormReportableFieldMetadata
{
    public static IReadOnlyList<ReportableFieldMetadata> SystemFields { get; } =
    [
        new(ReportableSystemFields.Status, "Record status", "status", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, true, false, true),
        new(ReportableSystemFields.CreatedAt, "Created date", "datetime", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.CreatedById, "Created by", "user", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.UpdatedAt, "Updated date", "datetime", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.UpdatedById, "Updated by", "user", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.OwnerId, "Owner", "user", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, false),
        new(ReportableSystemFields.DepartmentId, "Department", "department", ReportableFieldSources.System, Array.Empty<ReportableFieldOptionMetadata>(), true, true, false, false, true)
    ];

    public static IReadOnlyList<ReportableFieldMetadata> GetReportableFields(FormSchemaDefinition schema)
    {
        return schema.Fields.Select(ToReportableField).Concat(SystemFields).ToArray();
    }

    public static IReadOnlyDictionary<string, ReportableFieldMetadata> GetReportableFieldsById(FormSchemaDefinition schema)
    {
        return GetReportableFields(schema).ToDictionary(field => field.Id, StringComparer.Ordinal);
    }

    private static ReportableFieldMetadata ToReportableField(FormFieldDefinition field)
    {
        var isChoice = FormFieldTypes.IsChoice(field.Type);

        return new ReportableFieldMetadata(
            field.Id,
            field.Label,
            field.Type,
            ReportableFieldSources.Form,
            (field.Options ?? Array.Empty<FormFieldOptionDefinition>())
                .Select(option => new ReportableFieldOptionMetadata(option.Id, option.Label, option.Value))
                .ToArray(),
            Filterable: true,
            Sortable: true,
            Searchable: IsSearchable(field.Type),
            SupportsAggregation: field.Type == FormFieldTypes.Number,
            SupportsChoiceGrouping: isChoice);
    }

    private static bool IsSearchable(string type)
    {
        return type is FormFieldTypes.Text
            or FormFieldTypes.Textarea
            or FormFieldTypes.Email
            or FormFieldTypes.Phone
            or FormFieldTypes.Select
            or FormFieldTypes.Radio;
    }
}
