using System.Globalization;
using System.Text;

namespace OpenBusinessPlatform.Api.Modules.CreatorAnalysis;

public sealed class CreatorExportAnalyzer
{
    private static readonly IReadOnlyDictionary<string, string[]> CredentialTerms = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["password"] = ["password", "passwd", "pwd"],
        ["secret"] = ["secret", "client_secret"],
        ["token"] = ["token", "access_token", "refresh_token"],
        ["api_key"] = ["api_key", "apikey", "api-key"],
        ["private_key"] = ["private_key", "private key", "begin private key"],
        ["authorization"] = ["authorization", "bearer "],
        ["connection_credential"] = ["connectionstring", "connection_string", "credential"]
    };

    private static readonly IReadOnlyDictionary<string, (string Type, string Status, string? Module, string? Proposed, string Reason)> ConstructCatalog =
        new Dictionary<string, (string, string, string?, string?, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["application"] = ("application", CreatorAnalysisStatuses.ManualReview, null, null, "application_container_requires_design"),
            ["component"] = ("component", CreatorAnalysisStatuses.ManualReview, null, null, "component_requires_review"),
            ["form"] = ("form", CreatorAnalysisStatuses.Supported, "forms", "form", "supported_form_candidate"),
            ["report"] = ("report", CreatorAnalysisStatuses.ManualReview, "reports", "list_report", "report_style_requires_review"),
            ["view"] = ("report", CreatorAnalysisStatuses.ManualReview, "reports", "list_report", "report_style_requires_review"),
            ["lookup"] = ("relationship", CreatorAnalysisStatuses.ManualReview, "forms", "recordLookup", "lookup_target_requires_mapping"),
            ["relationship"] = ("relationship", CreatorAnalysisStatuses.ManualReview, "forms", "recordLookup", "lookup_target_requires_mapping"),
            ["workflow"] = ("workflow", CreatorAnalysisStatuses.ManualReview, "workflows", null, "workflow_requires_redesign"),
            ["function"] = ("function", CreatorAnalysisStatuses.Unsafe, null, null, "source_code_not_executed"),
            ["script"] = ("function", CreatorAnalysisStatuses.Unsafe, null, null, "source_code_not_executed"),
            ["schedule"] = ("schedule", CreatorAnalysisStatuses.ManualReview, "processing", null, "schedule_requires_redesign"),
            ["page"] = ("page", CreatorAnalysisStatuses.Unsupported, null, null, "custom_page_unsupported"),
            ["permission"] = ("permission", CreatorAnalysisStatuses.ManualReview, "permissions", null, "permission_requires_security_review"),
            ["role"] = ("permission", CreatorAnalysisStatuses.ManualReview, "permissions", null, "permission_requires_security_review"),
            ["connection"] = ("connection", CreatorAnalysisStatuses.Unsafe, "integrations", null, "connection_credentials_not_imported"),
            ["oauth"] = ("connection", CreatorAnalysisStatuses.Unsafe, "integrations", null, "connection_credentials_not_imported"),
            ["data"] = ("data", CreatorAnalysisStatuses.Unsafe, null, null, "source_data_not_imported"),
            ["record"] = ("data", CreatorAnalysisStatuses.Unsafe, null, null, "source_data_not_imported")
        };

    private static readonly IReadOnlyDictionary<string, (string Status, string? Proposed, string Reason)> FieldCatalog =
        new Dictionary<string, (string, string?, string)>(StringComparer.Ordinal)
        {
            ["text"] = (CreatorAnalysisStatuses.Supported, "text", "supported_field_candidate"),
            ["singleline"] = (CreatorAnalysisStatuses.Supported, "text", "supported_field_candidate"),
            ["textarea"] = (CreatorAnalysisStatuses.Supported, "textarea", "supported_field_candidate"),
            ["multiline"] = (CreatorAnalysisStatuses.Supported, "textarea", "supported_field_candidate"),
            ["email"] = (CreatorAnalysisStatuses.Supported, "email", "supported_field_candidate"),
            ["phone"] = (CreatorAnalysisStatuses.Supported, "phone", "supported_field_candidate"),
            ["number"] = (CreatorAnalysisStatuses.Supported, "number", "supported_field_candidate"),
            ["decimal"] = (CreatorAnalysisStatuses.Supported, "number", "supported_field_candidate"),
            ["currency"] = (CreatorAnalysisStatuses.Supported, "currency", "supported_field_candidate"),
            ["percent"] = (CreatorAnalysisStatuses.Supported, "percent", "supported_field_candidate"),
            ["date"] = (CreatorAnalysisStatuses.Supported, "date", "supported_field_candidate"),
            ["datetime"] = (CreatorAnalysisStatuses.Supported, "datetime", "supported_field_candidate"),
            ["time"] = (CreatorAnalysisStatuses.Supported, "time", "supported_field_candidate"),
            ["url"] = (CreatorAnalysisStatuses.Supported, "url", "supported_field_candidate"),
            ["checkbox"] = (CreatorAnalysisStatuses.Supported, "checkbox", "supported_field_candidate"),
            ["decisionbox"] = (CreatorAnalysisStatuses.Supported, "checkbox", "supported_field_candidate"),
            ["dropdown"] = (CreatorAnalysisStatuses.Supported, "select", "supported_field_candidate"),
            ["singleselect"] = (CreatorAnalysisStatuses.Supported, "select", "supported_field_candidate"),
            ["radio"] = (CreatorAnalysisStatuses.Supported, "radio", "supported_field_candidate"),
            ["autonumber"] = (CreatorAnalysisStatuses.Supported, "autonumber", "supported_field_candidate"),
            ["address"] = (CreatorAnalysisStatuses.Supported, "address", "supported_field_candidate"),
            ["fileupload"] = (CreatorAnalysisStatuses.Supported, "fileUpload", "file_constraints_require_review"),
            ["lookup"] = (CreatorAnalysisStatuses.ManualReview, "recordLookup", "lookup_target_requires_mapping"),
            ["multiselect"] = (CreatorAnalysisStatuses.ManualReview, null, "multi_select_requires_redesign"),
            ["subform"] = (CreatorAnalysisStatuses.ManualReview, "subTable", "subform_requires_mapping"),
            ["grid"] = (CreatorAnalysisStatuses.ManualReview, "subTable", "subform_requires_mapping"),
            ["user"] = (CreatorAnalysisStatuses.ManualReview, "userPicker", "identity_picker_requires_mapping"),
            ["users"] = (CreatorAnalysisStatuses.ManualReview, "userPicker", "identity_picker_requires_mapping"),
            ["organization"] = (CreatorAnalysisStatuses.ManualReview, "departmentPicker", "identity_picker_requires_mapping")
        };

    private static readonly IReadOnlyDictionary<string, (string Severity, string Message)> ReasonCatalog =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["application_container_requires_design"] = ("info", "The source application container requires manual platform organization."),
            ["component_requires_review"] = ("warning", "The component requires manual compatibility review."),
            ["report_style_requires_review"] = ("warning", "Only list-style reports are direct candidates; verify this report style manually."),
            ["lookup_target_requires_mapping"] = ("warning", "Lookup targets require an explicit platform form mapping."),
            ["workflow_requires_redesign"] = ("warning", "Workflow semantics require manual redesign using typed platform capabilities."),
            ["source_code_not_executed"] = ("error", "Source functions and scripts are not executed or translated."),
            ["schedule_requires_redesign"] = ("warning", "Scheduled behavior requires manual redesign with bounded platform jobs."),
            ["custom_page_unsupported"] = ("warning", "Custom source pages are not supported by this analysis slice."),
            ["permission_requires_security_review"] = ("warning", "Source permissions are advisory and require manual security review."),
            ["connection_credentials_not_imported"] = ("error", "Connections and credentials are not imported or exposed."),
            ["source_data_not_imported"] = ("error", "Source records and literal data are not imported or exposed."),
            ["field_type_requires_review"] = ("warning", "The field type requires manual compatibility review."),
            ["file_constraints_require_review"] = ("info", "File upload constraints require manual review before any future mapping."),
            ["multi_select_requires_redesign"] = ("warning", "Multi-select cardinality requires manual redesign."),
            ["subform_requires_mapping"] = ("warning", "Subform rows require an explicit child-data mapping."),
            ["identity_picker_requires_mapping"] = ("warning", "Source identities require explicit platform user or department mapping."),
            ["unknown_construct"] = ("warning", "The source construct is not recognized by this analyzer version."),
            ["malformed_structure"] = ("warning", "The source structure appears incomplete or malformed."),
            ["credential_signal_detected"] = ("error", "Potential credential material was detected and suppressed."),
            ["analysis_limit_reached"] = ("warning", "The compatibility report reached a configured analysis limit.")
        };

    public CreatorAnalysisReportDto Analyze(string source, int byteCount)
    {
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var credentialCounts = DetectCredentialSignals(lines, out var sensitiveLines);
        var state = new AnalysisState(byteCount, lines.Length);
        var inBlockComment = false;
        var braces = 0;
        var parentheses = 0;
        string? pendingFieldName = null;
        var pendingFieldLine = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            var raw = lines[index];
            var lineNumber = index + 1;
            var masked = MaskLine(raw, ref inBlockComment);
            braces += masked.Count(character => character == '{') - masked.Count(character => character == '}');
            parentheses += masked.Count(character => character == '(') - masked.Count(character => character == ')');
            if (braces < 0 || parentheses < 0)
            {
                state.Complete = false;
                braces = Math.Max(0, braces);
                parentheses = Math.Max(0, parentheses);
                state.AddFinding("warning", CreatorAnalysisStatuses.Unknown, "malformed_structure", null);
            }

            var trimmed = masked.Trim();
            if (trimmed.Length == 0) continue;
            var token = FirstToken(trimmed);

            if (token.Equals("field", StringComparison.OrdinalIgnoreCase))
            {
                var remainder = ExtractRemainder(raw, token.Length);
                var words = remainder.Split([' ', '\t', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var sourceType = words.Length > 1 ? words[^1] : string.Empty;
                var displayName = words.Length > 1 ? string.Join(' ', words[..^1]) : remainder;
                AddField(state, displayName, sourceType, lineNumber, sensitiveLines.Contains(lineNumber));
                continue;
            }

            if (TryTypeAssignment(trimmed, raw, out var assignedType))
            {
                AddField(state, pendingFieldName ?? $"Field {state.ObservedConstructs + 1}", assignedType, pendingFieldLine == 0 ? lineNumber : pendingFieldLine,
                    sensitiveLines.Contains(lineNumber) || sensitiveLines.Contains(pendingFieldLine));
                pendingFieldName = null;
                pendingFieldLine = 0;
                continue;
            }

            if (ConstructCatalog.TryGetValue(token, out var catalog))
            {
                var name = catalog.Type == "data"
                    ? "[redacted]"
                    : SafeName(ExtractRemainder(raw, token.Length), catalog.Type, state.ObservedConstructs + 1, sensitiveLines.Contains(lineNumber));
                var status = catalog.Status;
                var reason = catalog.Reason;
                if (catalog.Type == "report" && ContainsWord(trimmed, "list"))
                {
                    status = CreatorAnalysisStatuses.Supported;
                    reason = "supported_report_candidate";
                }
                state.AddConstruct(catalog.Type, name, lineNumber, status, catalog.Module, catalog.Proposed, reason);
                continue;
            }

            if (IsPlainIdentifier(trimmed))
            {
                pendingFieldName = raw.Trim();
                pendingFieldLine = lineNumber;
                continue;
            }

            if (trimmed.EndsWith('{') && token.Length > 0)
            {
                var name = SafeName(ExtractRemainder(raw, token.Length), "construct", state.ObservedConstructs + 1, sensitiveLines.Contains(lineNumber));
                state.AddConstruct("unknown", name, lineNumber, CreatorAnalysisStatuses.Unknown, null, null, "unknown_construct");
            }
        }

        if (inBlockComment || braces != 0 || parentheses != 0)
        {
            state.Complete = false;
            state.AddFinding("warning", CreatorAnalysisStatuses.Unknown, "malformed_structure", null);
        }

        foreach (var signal in credentialCounts.OrderBy(item => item.Key, StringComparer.Ordinal))
            state.AddFinding("error", CreatorAnalysisStatuses.Unsafe, "credential_signal_detected", null);

        state.FinalizeLimits();
        return state.ToReport(credentialCounts);
    }

    private static void AddField(AnalysisState state, string name, string sourceType, int line, bool sensitive)
    {
        var normalized = NormalizeType(sourceType);
        if (!FieldCatalog.TryGetValue(normalized, out var catalog))
            catalog = (CreatorAnalysisStatuses.ManualReview, null, "field_type_requires_review");
        state.AddConstruct("field", SafeName(name, "field", state.ObservedConstructs + 1, sensitive), line, catalog.Status, "forms", catalog.Proposed, catalog.Reason);
    }

    private static Dictionary<string, int> DetectCredentialSignals(string[] lines, out HashSet<int> sensitiveLines)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        sensitiveLines = [];
        for (var index = 0; index < lines.Length; index++)
        {
            var lower = lines[index].ToLowerInvariant();
            foreach (var item in CredentialTerms)
            {
                if (!item.Value.Any(lower.Contains)) continue;
                counts[item.Key] = counts.GetValueOrDefault(item.Key) + 1;
                sensitiveLines.Add(index + 1);
            }
            if (ContainsHighEntropyToken(lines[index]))
            {
                counts["high_entropy_literal"] = counts.GetValueOrDefault("high_entropy_literal") + 1;
                sensitiveLines.Add(index + 1);
            }
        }
        return counts;
    }

    private static bool ContainsHighEntropyToken(string line)
    {
        var length = 0;
        var lower = false;
        var upper = false;
        var digit = false;
        var symbol = false;
        foreach (var character in line)
        {
            if (char.IsLetterOrDigit(character) || character is '+' or '/' or '_' or '-' or '=')
            {
                length++;
                lower |= char.IsLower(character);
                upper |= char.IsUpper(character);
                digit |= char.IsDigit(character);
                symbol |= !char.IsLetterOrDigit(character);
                if (length >= 32 && (lower ? 1 : 0) + (upper ? 1 : 0) + (digit ? 1 : 0) + (symbol ? 1 : 0) >= 3) return true;
            }
            else
            {
                length = 0;
                lower = upper = digit = symbol = false;
            }
        }
        return false;
    }

    private static string MaskLine(string line, ref bool inBlockComment)
    {
        var output = new char[line.Length];
        var quote = '\0';
        var escaped = false;
        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];
            var next = index + 1 < line.Length ? line[index + 1] : '\0';
            if (inBlockComment)
            {
                if (current == '*' && next == '/') { inBlockComment = false; index++; }
                continue;
            }
            if (quote != '\0')
            {
                if (!escaped && current == quote) { quote = '\0'; output[index] = current; }
                escaped = !escaped && current == '\\';
                if (current != '\\') escaped = false;
                continue;
            }
            if (current == '/' && next == '*') { inBlockComment = true; index++; continue; }
            if (current == '/' && next == '/') break;
            if (current is '\'' or '"') { quote = current; output[index] = current; continue; }
            output[index] = current;
        }
        return new string(output);
    }

    private static bool TryTypeAssignment(string masked, string raw, out string value)
    {
        value = string.Empty;
        var equals = masked.IndexOf('=');
        if (equals < 0 || !masked[..equals].Trim().Equals("type", StringComparison.OrdinalIgnoreCase)) return false;
        value = raw[(equals + 1)..].Trim().Trim('"', '\'', ';', ',', ')', '}', ' ');
        return value.Length > 0;
    }

    private static string FirstToken(string value)
    {
        var length = 0;
        while (length < value.Length && (char.IsLetter(value[length]) || value[length] == '_')) length++;
        return value[..length];
    }

    private static string ExtractRemainder(string raw, int tokenLength) => tokenLength >= raw.TrimStart().Length
        ? string.Empty
        : raw.TrimStart()[tokenLength..].Trim().Trim('{', '(', '[', ':', '=', '"', '\'', ' ', '\t');

    private static string SafeName(string value, string type, int ordinal, bool sensitive)
    {
        var trimmed = value.Trim().Trim('"', '\'', '{', '}', '(', ')', '[', ']', ';', ',', ' ');
        if (trimmed.Length == 0) return $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(type)} {ordinal}";
        var lower = trimmed.ToLowerInvariant();
        if (sensitive || trimmed.Length > 160 || trimmed.Any(char.IsControl) || trimmed.Contains('@') || trimmed.Contains('=')
            || lower.Contains("http://", StringComparison.Ordinal) || lower.Contains("https://", StringComparison.Ordinal)
            || CredentialTerms.Values.SelectMany(value => value).Any(lower.Contains)
            || trimmed.Any(character => !char.IsLetterOrDigit(character) && character is not ' ' and not '_' and not '-' and not '.')) return "[redacted]";
        return string.Join(' ', trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeType(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static bool ContainsWord(string value, string word) => value.Split([' ', '\t', ':', '=', '{', '(', '['], StringSplitOptions.RemoveEmptyEntries)
        .Any(item => item.Equals(word, StringComparison.OrdinalIgnoreCase));
    private static bool IsPlainIdentifier(string value) => value.Length is > 0 and <= 160
        && value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or ' ')
        && !value.Contains(' ');

    private sealed class AnalysisState(int byteCount, int lineCount)
    {
        private readonly List<CreatorAnalysisConstructDto> constructs = [];
        private readonly List<CreatorAnalysisFindingDto> findings = [];
        private readonly Dictionary<string, int> statuses = CreatorAnalysisStatuses.All.ToDictionary(status => status, _ => 0, StringComparer.Ordinal);
        public int ObservedConstructs { get; private set; }
        public int ObservedFindings { get; private set; }
        public bool Complete { get; set; } = true;
        public bool Truncated { get; private set; }

        public void AddConstruct(string type, string name, int line, string status, string? module, string? proposed, string reason)
        {
            ObservedConstructs++;
            statuses[status] = statuses.GetValueOrDefault(status) + 1;
            string? id = null;
            if (constructs.Count < CreatorAnalysisLimits.MaxConstructs)
            {
                id = $"construct-{ObservedConstructs}";
                constructs.Add(new(id, type, name, line, line, status, module, proposed));
            }
            else { Truncated = true; Complete = false; }
            if (status != CreatorAnalysisStatuses.Supported) AddFinding(ReasonCatalog[reason].Severity, status, reason, id);
            else if (reason == "file_constraints_require_review") AddFinding(ReasonCatalog[reason].Severity, CreatorAnalysisStatuses.ManualReview, reason, id);
        }

        public void AddFinding(string severity, string status, string reason, string? constructId)
        {
            ObservedFindings++;
            if (findings.Count < CreatorAnalysisLimits.MaxFindings)
                findings.Add(new($"finding-{ObservedFindings}", severity, status, reason, constructId, ReasonCatalog[reason].Message));
            else { Truncated = true; Complete = false; }
        }

        public void FinalizeLimits()
        {
            if (!Truncated) return;
            if (findings.Count < CreatorAnalysisLimits.MaxFindings)
                findings.Add(new($"finding-{++ObservedFindings}", "warning", CreatorAnalysisStatuses.ManualReview, "analysis_limit_reached", null, ReasonCatalog["analysis_limit_reached"].Message));
        }

        public CreatorAnalysisReportDto ToReport(IReadOnlyDictionary<string, int> credentials) => new(
            CreatorAnalysisLimits.AnalyzerVersion, false, Complete, Truncated,
            new(byteCount, lineCount), new(ObservedConstructs, ObservedFindings, statuses),
            credentials.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => new CreatorCredentialSignalDto(item.Key, item.Value)).ToArray(),
            constructs, findings);
    }
}
