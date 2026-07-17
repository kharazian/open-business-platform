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
    public const string RecordLookup = "recordLookup";
    public const string FileUpload = "fileUpload";
    public const string Currency = "currency";
    public const string Percent = "percent";
    public const string Rating = "rating";
    public const string Url = "url";
    public const string Time = "time";
    public const string Datetime = "datetime";
    public const string UserPicker = "userPicker";
    public const string DepartmentPicker = "departmentPicker";
    public const string SubTable = "subTable";
    public const string Address = "address";
    public const string Autonumber = "autonumber";

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
        Radio,
        RecordLookup,
        FileUpload,
        Currency,
        Percent,
        Rating,
        Url,
        Time,
        Datetime,
        UserPicker,
        DepartmentPicker,
        SubTable,
        Address,
        Autonumber
    };

    public static bool IsChoice(string type)
    {
        return string.Equals(type, Select, StringComparison.Ordinal)
            || string.Equals(type, Radio, StringComparison.Ordinal);
    }

    public static bool IsNumeric(string type)
    {
        return string.Equals(type, Number, StringComparison.Ordinal)
            || string.Equals(type, Currency, StringComparison.Ordinal)
            || string.Equals(type, Percent, StringComparison.Ordinal)
            || string.Equals(type, Rating, StringComparison.Ordinal);
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

public sealed record FormFieldLookupDefinition(
    string SourceType,
    string SourceFormId,
    IReadOnlyList<string> LabelFieldIds,
    IReadOnlyList<string> SearchFieldIds,
    IReadOnlyList<FormFieldLookupFilterDefinition>? Filters = null);

public sealed record FormFieldLookupFilterDefinition(
    string SourceFieldId,
    string ValueFromFieldId);

public sealed record FormFieldSubTableDefinition(
    string SourceType,
    string ChildFormId,
    string ParentLookupFieldId,
    IReadOnlyList<string> DisplayColumnFieldIds,
    bool AllowInlineCreate = false,
    bool AllowInlineEdit = false,
    bool AllowInlineDelete = false,
    int? MinRows = null,
    int? MaxRows = null);

public static class FormAddressSubfields
{
    public const string Line1 = "line1";
    public const string Line2 = "line2";
    public const string City = "city";
    public const string Region = "region";
    public const string PostalCode = "postalCode";
    public const string Country = "country";
    public const string Latitude = "latitude";
    public const string Longitude = "longitude";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Line1, Line2, City, Region, PostalCode, Country, Latitude, Longitude
    };
}

public sealed record FormFieldAddressDefinition(IReadOnlyList<string>? RequiredSubfields = null);
public sealed record FormFieldAutonumberDefinition(string? Prefix = null, string? Suffix = null, long StartAt = 1, int Padding = 0);

public static class FormAutonumberLimits
{
    public const long MaxStartAt = 999_999_999_999_999;
    public const int MaxPadding = 18;
    public const int MaxAffixLength = 40;
}

public sealed record FormFieldDefinition(
    string Id,
    string Type,
    string Label,
    bool Required = false,
    string? Placeholder = null,
    string? HelpText = null,
    object? DefaultValue = null,
    IReadOnlyList<FormFieldOptionDefinition>? Options = null,
    FormFieldValidationDefinition? Validation = null,
    FormFieldLookupDefinition? Lookup = null,
    FormFieldSubTableDefinition? SubTable = null,
    FormFieldAddressDefinition? Address = null,
    FormFieldAutonumberDefinition? Autonumber = null);

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
