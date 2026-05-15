namespace OpenBusinessPlatform.Api.Modules.Forms;

public static class FormFieldTypes
{
    public const string Text = "text";
    public const string Textarea = "textarea";
    public const string Number = "number";
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Date = "date";
    public const string Select = "select";
    public const string Checkbox = "checkbox";
    public const string Radio = "radio";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Text,
        Textarea,
        Number,
        Email,
        Phone,
        Date,
        Select,
        Checkbox,
        Radio
    };

    public static bool IsChoice(string type)
    {
        return string.Equals(type, Select, StringComparison.Ordinal)
            || string.Equals(type, Radio, StringComparison.Ordinal);
    }
}

public sealed record FormFieldOptionDefinition(
    string Id,
    string Label,
    string Value);

public sealed record FormFieldValidationDefinition(
    int? MinLength = null,
    int? MaxLength = null,
    decimal? Min = null,
    decimal? Max = null,
    string? Pattern = null);

public sealed record FormFieldDefinition(
    string Id,
    string Type,
    string Label,
    bool Required = false,
    string? Placeholder = null,
    string? HelpText = null,
    object? DefaultValue = null,
    IReadOnlyList<FormFieldOptionDefinition>? Options = null,
    FormFieldValidationDefinition? Validation = null);

public sealed record ResponsiveSpanDefinition(
    int Mobile,
    int Tablet,
    int Desktop);

public sealed record FormLayoutColumnDefinition(
    string Id,
    ResponsiveSpanDefinition Span,
    IReadOnlyList<string> Fields);

public sealed record FormLayoutRowDefinition(
    string Id,
    IReadOnlyList<FormLayoutColumnDefinition> Columns);

public sealed record FormLayoutSectionDefinition(
    string Id,
    string? Title,
    string? Description,
    IReadOnlyList<FormLayoutRowDefinition> Rows);

public sealed record FormLayoutPageDefinition(
    string Id,
    string? Title,
    string? Description,
    IReadOnlyList<FormLayoutSectionDefinition> Sections);

public sealed record FormLayoutDefinition(
    IReadOnlyList<FormLayoutPageDefinition> Pages);

public sealed record FormSchemaDefinition(
    int SchemaVersion,
    IReadOnlyList<FormFieldDefinition> Fields,
    FormLayoutDefinition Layout);

public sealed record FormVersionDefinition(
    string Id,
    string FormId,
    int VersionNumber,
    FormSchemaDefinition Schema,
    string? PublishedBy,
    DateTimeOffset PublishedAt);

public sealed record FormRecordValuesDefinition(
    string FormVersionId,
    IReadOnlyDictionary<string, object?> Values);

public sealed record FormValidationError(
    string Path,
    string Code,
    string Message);

public sealed record FormValidationResult(IReadOnlyList<FormValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}
