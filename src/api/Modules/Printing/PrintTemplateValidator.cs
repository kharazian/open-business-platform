using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public static class PrintTemplateValidator
{
    public static PrintTemplateValidationResult Validate(PrintTemplateConfig config, string templateType, FormSchemaDefinition? schema = null)
    {
        var errors = new List<PrintTemplateValidationError>();
        var validFieldIds = ResolveValidFieldIds(templateType, schema);

        if (config.SchemaVersion != 1)
        {
            errors.Add(Error("config.schemaVersion", "print_template.schema_version.unsupported", "Template schema version must be 1."));
        }

        if (!PrintTemplateTypes.Supported.Contains(config.Type) || !string.Equals(config.Type, templateType, StringComparison.Ordinal))
        {
            errors.Add(Error("config.type", "print_template.type.invalid", "Template config type must match the template type."));
        }

        if (string.IsNullOrWhiteSpace(config.Header.Title))
        {
            errors.Add(Error("config.header.title", "print_template.header.title_required", "Header title is required."));
        }

        ValidateLayout(config.Layout, errors);

        if (config.Sections.Count == 0)
        {
            errors.Add(Error("config.sections", "print_template.sections.required", "At least one section is required."));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < config.Sections.Count; index += 1)
        {
            var section = config.Sections[index];
            var path = $"config.sections[{index}]";

            if (string.IsNullOrWhiteSpace(section.Id))
            {
                errors.Add(Error($"{path}.id", "print_template.section.id_required", "Section id is required."));
            }
            else if (!ids.Add(section.Id.Trim()))
            {
                errors.Add(Error($"{path}.id", "print_template.section.id_duplicate", "Section ids must be unique."));
            }

            if (string.IsNullOrWhiteSpace(section.Title))
            {
                errors.Add(Error($"{path}.title", "print_template.section.title_required", "Section title is required."));
            }

            if (!PrintTemplateSectionKinds.Supported.Contains(section.Kind))
            {
                errors.Add(Error($"{path}.kind", "print_template.section.kind_invalid", "Section kind is not supported."));
            }

            if (templateType == PrintTemplateTypes.Record && section.Kind == PrintTemplateSectionKinds.Table)
            {
                errors.Add(Error($"{path}.kind", "print_template.section.table_record", "Record templates cannot use table sections."));
            }

            if (templateType == PrintTemplateTypes.Report && section.Kind == PrintTemplateSectionKinds.Fields)
            {
                errors.Add(Error($"{path}.kind", "print_template.section.fields_report", "Report templates cannot use field sections."));
            }

            if (validFieldIds is not null && section.Kind is PrintTemplateSectionKinds.Fields or PrintTemplateSectionKinds.Table)
            {
                ValidateFieldIds(section.FieldIds, validFieldIds, $"{path}.fieldIds", errors);
            }
        }

        return new PrintTemplateValidationResult(errors);
    }

    private static void ValidateLayout(PrintTemplateLayoutConfig? layout, List<PrintTemplateValidationError> errors)
    {
        if (layout is null)
        {
            return;
        }

        if (!PrintTemplatePageSizes.Supported.Contains(layout.PageSize?.Trim() ?? string.Empty))
        {
            errors.Add(Error("config.layout.pageSize", "print_template.layout.page_size_invalid", "Page size must be Letter or A4."));
        }

        if (!PrintTemplateOrientations.Supported.Contains(layout.Orientation?.Trim() ?? string.Empty))
        {
            errors.Add(Error("config.layout.orientation", "print_template.layout.orientation_invalid", "Orientation must be portrait or landscape."));
        }

        if (!PrintTemplateMargins.Supported.Contains(layout.Margin?.Trim() ?? string.Empty))
        {
            errors.Add(Error("config.layout.margin", "print_template.layout.margin_invalid", "Margin must be narrow, normal, or wide."));
        }
    }

    private static IReadOnlySet<string>? ResolveValidFieldIds(string templateType, FormSchemaDefinition? schema)
    {
        if (schema is null)
        {
            return null;
        }

        if (templateType == PrintTemplateTypes.Record)
        {
            return schema.Fields.Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
        }

        if (templateType == PrintTemplateTypes.Report)
        {
            return FormReportableFieldMetadata.GetReportableFieldsById(schema).Keys.ToHashSet(StringComparer.Ordinal);
        }

        return null;
    }

    private static void ValidateFieldIds(
        IReadOnlyList<string>? fieldIds,
        IReadOnlySet<string> validFieldIds,
        string path,
        List<PrintTemplateValidationError> errors)
    {
        if (fieldIds is null)
        {
            return;
        }

        for (var index = 0; index < fieldIds.Count; index += 1)
        {
            var fieldId = fieldIds[index].Trim();
            var fieldPath = $"{path}[{index}]";

            if (string.IsNullOrWhiteSpace(fieldId))
            {
                errors.Add(Error(fieldPath, "print_template.field.required", "Field id is required."));
                continue;
            }

            if (!validFieldIds.Contains(fieldId))
            {
                errors.Add(Error(fieldPath, "print_template.field.unknown", "Template field does not exist for this template scope."));
            }
        }
    }

    private static PrintTemplateValidationError Error(string path, string code, string message)
    {
        return new PrintTemplateValidationError(path, code, message);
    }
}
