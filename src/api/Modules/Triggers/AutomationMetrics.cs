using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class AutomationMetrics
{
    public const string ContentType = "text/plain; version=0.0.4; charset=utf-8";

    public static string Format(AutomationOutboxSnapshot snapshot, AutomationHealthOptions options)
    {
        var normalized = options.Normalize();
        var builder = new StringBuilder();
        AppendGauge(builder, "obp_trigger_outbox_messages", "Trigger event outbox messages by delivery status.", snapshot.PendingCount, ("status", "pending"));
        AppendGauge(builder, "obp_trigger_outbox_messages", null, snapshot.ProcessingCount, ("status", "processing"));
        AppendGauge(builder, "obp_trigger_outbox_messages", null, snapshot.CompletedCount, ("status", "completed"));
        AppendGauge(builder, "obp_trigger_outbox_messages", null, snapshot.DeadLetterCount, ("status", "dead_letter"));
        AppendGauge(builder, "obp_trigger_outbox_retry_backlog", "Pending trigger event messages with at least one failed delivery attempt.", snapshot.RetryBacklogCount);
        AppendGauge(builder, "obp_trigger_outbox_oldest_pending_age_seconds", "Age of the oldest pending trigger event message.", snapshot.OldestPendingAgeSeconds);
        AppendGauge(builder, "obp_trigger_outbox_pending_age_warning_seconds", "Configured pending-age warning threshold.", normalized.PendingAgeWarningSeconds);
        AppendGauge(builder, "obp_trigger_outbox_dead_letter_warning_count", "Configured dead-letter warning threshold.", normalized.DeadLetterWarningCount);
        AppendGauge(builder, "obp_automation_health", "Automation delivery health where 1 is healthy and 0 is degraded.", snapshot.Status == "healthy" ? 1 : 0);
        return builder.ToString();
    }

    public static bool IsAccessAllowed(
        bool isDevelopment,
        string? authorizationHeader,
        AutomationHealthOptions options)
    {
        var normalized = options.Normalize();

        if (!normalized.MetricsEnabled)
        {
            return false;
        }

        if (normalized.MetricsToken is null)
        {
            return isDevelopment;
        }

        const string bearerPrefix = "Bearer ";
        if (authorizationHeader is null || !authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var supplied = authorizationHeader[bearerPrefix.Length..].Trim();
        var expectedBytes = Encoding.UTF8.GetBytes(normalized.MetricsToken);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private static void AppendGauge(
        StringBuilder builder,
        string name,
        string? help,
        double value,
        params (string Name, string Value)[] labels)
    {
        if (help is not null)
        {
            builder.Append("# HELP ").Append(name).Append(' ').AppendLine(help);
            builder.Append("# TYPE ").Append(name).AppendLine(" gauge");
        }

        builder.Append(name);
        if (labels.Length > 0)
        {
            builder.Append('{');
            builder.Append(string.Join(',', labels.Select(label => $"{label.Name}=\"{label.Value}\"")));
            builder.Append('}');
        }

        builder.Append(' ').AppendLine(value.ToString("0.###", CultureInfo.InvariantCulture));
    }
}
