using OpenBusinessPlatform.Api.Modules.Integrations;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public static class ProcessingJobValidator
{
    public const int MaxNameLength = 160;
    public const int MaxRowsLimit = 5000;
    public const int MaxCsvBytes = 1024 * 1024;

    public static ProcessingJobValidationResult Validate(CreateProcessingJobRequest request)
    {
        var errors = new List<ProcessingJobValidationError>();
        if (request.AdditionalProperties is { Count: > 0 }) errors.Add(Error("$", "processing.properties", "Request contains unsupported properties."));
        var kind = request.Kind?.Trim().ToLowerInvariant() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > MaxNameLength) errors.Add(Error("name", "processing.name", $"Name must be between 1 and {MaxNameLength} characters."));
        if (!ProcessingJobKinds.Supported.Contains(kind)) errors.Add(Error("kind", "processing.kind", "Job kind is not supported."));
        ValidateConfig(kind, request.Config, errors);
        ValidateSchedule(kind, request.Schedule, errors);
        ValidateRetry(kind, request.RetryPolicy, errors);
        return new ProcessingJobValidationResult(errors);
    }

    public static ProcessingJobValidationResult ValidateManualRun(string kind, CreateProcessingJobRunRequest request)
    {
        var errors = new List<ProcessingJobValidationError>();
        if (request.AdditionalProperties is { Count: > 0 }) errors.Add(Error("$", "processing.run.properties", "Run request contains unsupported properties."));
        if (kind == ProcessingJobKinds.CsvRecordImport)
        {
            if (string.IsNullOrWhiteSpace(request.CsvContent)) errors.Add(Error("csvContent", "processing.run.csv_required", "CSV content is required."));
            else if (System.Text.Encoding.UTF8.GetByteCount(request.CsvContent) > MaxCsvBytes) errors.Add(Error("csvContent", "processing.run.csv_size", "CSV content must not exceed 1 MB."));
            if (request.FileName?.Length > 260) errors.Add(Error("fileName", "processing.run.file_name", "File name must not exceed 260 characters."));
        }
        else if (!string.IsNullOrWhiteSpace(request.CsvContent) || !string.IsNullOrWhiteSpace(request.FileName))
        {
            errors.Add(Error("csvContent", "processing.run.payload", "Export runs do not accept a runtime payload."));
        }
        return new ProcessingJobValidationResult(errors);
    }

    private static void ValidateConfig(string kind, ProcessingJobConfigDefinition? config, List<ProcessingJobValidationError> errors)
    {
        if (config is null) { errors.Add(Error("config", "processing.config.required", "Job config is required.")); return; }
        if (config.AdditionalProperties is { Count: > 0 }) errors.Add(Error("config", "processing.config.properties", "Job config contains unsupported properties."));
        if (config.FormId == Guid.Empty) errors.Add(Error("config.formId", "processing.config.form", "Form is required."));
        if (string.IsNullOrWhiteSpace(config.IntegrationKey) || config.IntegrationKey.Trim().Length > 120) errors.Add(Error("config.integrationKey", "processing.config.integration_key", "Integration key is required and must not exceed 120 characters."));

        if (kind == ProcessingJobKinds.CsvRecordImport)
        {
            if (config.Mapping?.FieldMappings is not { Count: > 0 }) errors.Add(Error("config.mapping", "processing.config.mapping", "CSV import mapping is required."));
            if (config.Mapping?.AdditionalProperties is { Count: > 0 }
                || config.Mapping?.FieldMappings.Any(mapping => mapping.AdditionalProperties is { Count: > 0 }) == true)
                errors.Add(Error("config.mapping", "processing.config.mapping_properties", "CSV import mapping contains unsupported properties."));
            if (config.ReportId is not null || config.SourceType is not null || config.Format is not null || config.Search is not null) errors.Add(Error("config", "processing.config.import_properties", "CSV import config contains export-only properties."));
        }
        else if (kind == ProcessingJobKinds.RecordExport)
        {
            var sourceType = config.SourceType?.Trim().ToLowerInvariant();
            var format = config.Format?.Trim().ToLowerInvariant();
            if (sourceType is null || !ExternalExportJobSourceTypes.Supported.Contains(sourceType)) errors.Add(Error("config.sourceType", "processing.config.source", "Export source type is invalid."));
            if (format is null || !ExternalExportJobFormats.Supported.Contains(format)) errors.Add(Error("config.format", "processing.config.format", "Export format is invalid."));
            if (sourceType == ExternalExportJobSourceTypes.ListReport && config.ReportId is null) errors.Add(Error("config.reportId", "processing.config.report", "List-report exports require a report."));
            if (sourceType == ExternalExportJobSourceTypes.FormRecords && config.ReportId is not null) errors.Add(Error("config.reportId", "processing.config.report_unexpected", "Form-record exports do not accept a report."));
            if (config.Mapping is not null) errors.Add(Error("config.mapping", "processing.config.mapping_unexpected", "Export config does not accept an import mapping."));
            if (config.MaxRows is < 1 or > MaxRowsLimit) errors.Add(Error("config.maxRows", "processing.config.max_rows", $"Maximum rows must be between 1 and {MaxRowsLimit}."));
        }
    }

    private static void ValidateSchedule(string kind, ProcessingJobScheduleDefinition? schedule, List<ProcessingJobValidationError> errors)
    {
        if (schedule is null) return;
        if (kind != ProcessingJobKinds.RecordExport) errors.Add(Error("schedule", "processing.schedule.kind", "Only record exports can be scheduled."));
        if (schedule.AdditionalProperties is { Count: > 0 }) errors.Add(Error("schedule", "processing.schedule.properties", "Schedule contains unsupported properties."));
        if (!ProcessingScheduleKinds.Supported.Contains(schedule.Kind?.Trim().ToLowerInvariant() ?? string.Empty)) errors.Add(Error("schedule.kind", "processing.schedule.kind_value", "Schedule kind is invalid."));
        if (schedule.Interval is < 1 or > 366) errors.Add(Error("schedule.interval", "processing.schedule.interval", "Schedule interval must be between 1 and 366."));
        if (schedule.DayOfWeek is < 0 or > 6) errors.Add(Error("schedule.dayOfWeek", "processing.schedule.day_of_week", "Day of week must be between 0 and 6."));
        if (schedule.DayOfMonth is < 1 or > 31) errors.Add(Error("schedule.dayOfMonth", "processing.schedule.day_of_month", "Day of month must be between 1 and 31."));
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone); }
        catch { errors.Add(Error("schedule.timeZone", "processing.schedule.time_zone", "Schedule time zone is invalid.")); }
    }

    private static void ValidateRetry(string kind, ProcessingJobRetryPolicyDefinition? retry, List<ProcessingJobValidationError> errors)
    {
        if (retry is null) return;
        if (retry.AdditionalProperties is { Count: > 0 }) errors.Add(Error("retryPolicy", "processing.retry.properties", "Retry policy contains unsupported properties."));
        if (kind != ProcessingJobKinds.RecordExport && retry.IsEnabled) errors.Add(Error("retryPolicy.isEnabled", "processing.retry.kind", "Only record exports can be retried."));
        if (retry.MaxAttempts is < 1 or > 5) errors.Add(Error("retryPolicy.maxAttempts", "processing.retry.max_attempts", "Maximum attempts must be between 1 and 5."));
        if (retry.DelaySeconds is < 30 or > 86400) errors.Add(Error("retryPolicy.delaySeconds", "processing.retry.delay", "Retry delay must be between 30 seconds and 24 hours."));
    }

    private static ProcessingJobValidationError Error(string path, string code, string message) => new(path, code, message);
}
