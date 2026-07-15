using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerActionResumePolicy
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlySet<string> GetCompletedActionIds(JsonDocument? resultJson)
    {
        var completed = new HashSet<string>(StringComparer.Ordinal);
        if (resultJson is null || resultJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            return completed;
        }

        if (resultJson.RootElement.TryGetProperty("resume", out var resume)
            && resume.ValueKind == JsonValueKind.Object
            && resume.TryGetProperty("completedActionIds", out var checkpointIds)
            && checkpointIds.ValueKind == JsonValueKind.Array)
        {
            foreach (var checkpointId in checkpointIds.EnumerateArray())
            {
                if (checkpointId.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(checkpointId.GetString()))
                {
                    completed.Add(checkpointId.GetString()!);
                }
            }
        }

        if (!resultJson.RootElement.TryGetProperty("actions", out var actions) || actions.ValueKind != JsonValueKind.Array)
        {
            return completed;
        }

        foreach (var action in actions.EnumerateArray())
        {
            if (action.ValueKind != JsonValueKind.Object
                || action.TryGetProperty("errorMessage", out _)
                || !action.TryGetProperty("actionId", out var actionId)
                || actionId.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(actionId.GetString()))
            {
                continue;
            }

            completed.Add(actionId.GetString()!);
        }

        return completed;
    }

    public static JsonDocument MergeCompletedActions(JsonDocument? sourceResult, JsonDocument? attemptResult)
    {
        var completed = GetCompletedActionIds(sourceResult)
            .Concat(GetCompletedActionIds(attemptResult))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(actionId => actionId, StringComparer.Ordinal)
            .ToArray();
        var root = sourceResult is null
            ? new JsonObject()
            : JsonNode.Parse(sourceResult.RootElement.GetRawText())?.AsObject() ?? new JsonObject();

        root["resume"] = JsonSerializer.SerializeToNode(new
        {
            completedActionIds = completed
        }, JsonOptions);

        return JsonSerializer.SerializeToDocument(root, JsonOptions);
    }
}
