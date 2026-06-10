using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IncomingWebhookListenerValidator
{
    public static IncomingWebhookValidationResult Validate(
        UpsertIncomingWebhookListenerRequest request,
        FormSchemaDefinition schema)
    {
        var errors = new List<IncomingWebhookValidationError>();
        var fieldIds = schema.Fields.Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
        var mappedTargets = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new IncomingWebhookValidationError("name", "webhook_listener.name_required", "Listener name is required."));
        }

        if (!IsValidKey(request.ListenerKey))
        {
            errors.Add(new IncomingWebhookValidationError("listenerKey", "webhook_listener.key_invalid", "Listener key must be lowercase letters, numbers, or hyphens."));
        }

        if (!IncomingWebhookListenerActions.Supported.Contains(request.Action))
        {
            errors.Add(new IncomingWebhookValidationError("action", "webhook_listener.action_invalid", "Listener action is invalid."));
        }

        if (!IncomingWebhookListenerAuthModes.Supported.Contains(request.AuthMode))
        {
            errors.Add(new IncomingWebhookValidationError("authMode", "webhook_listener.auth_mode_invalid", "Listener auth mode is invalid."));
        }

        if (request.Mapping.FieldMappings.Count == 0)
        {
            errors.Add(new IncomingWebhookValidationError("mapping.fieldMappings", "webhook_listener.mapping_required", "At least one field mapping is required."));
        }

        foreach (var mapping in request.Mapping.FieldMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourcePath))
            {
                errors.Add(new IncomingWebhookValidationError("mapping.sourcePath", "webhook_listener.source_path_required", "Source path is required."));
            }

            if (string.IsNullOrWhiteSpace(mapping.TargetFieldId) || !fieldIds.Contains(mapping.TargetFieldId))
            {
                errors.Add(new IncomingWebhookValidationError("mapping.targetFieldId", "webhook_listener.target_field_unknown", "Mapped target field does not exist."));
            }
            else if (!mappedTargets.Add(mapping.TargetFieldId))
            {
                errors.Add(new IncomingWebhookValidationError("mapping.targetFieldId", "webhook_listener.target_field_duplicate", "Target fields can only be mapped once."));
            }
        }

        if (string.Equals(request.Action, IncomingWebhookListenerActions.Upsert, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(request.SafeLookupFieldId) || !fieldIds.Contains(request.SafeLookupFieldId))
            {
                errors.Add(new IncomingWebhookValidationError("safeLookupFieldId", "webhook_listener.safe_lookup_required", "Upsert requires an explicit safe lookup field."));
            }
            else if (!mappedTargets.Contains(request.SafeLookupFieldId))
            {
                errors.Add(new IncomingWebhookValidationError("safeLookupFieldId", "webhook_listener.safe_lookup_unmapped", "Safe lookup field must be included in the mapping."));
            }
        }

        return new IncomingWebhookValidationResult(errors);
    }

    private static bool IsValidKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120)
        {
            return false;
        }

        var previousHyphen = false;
        foreach (var character in value)
        {
            var valid = character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character == '-';

            if (!valid)
            {
                return false;
            }

            if (character == '-' && previousHyphen)
            {
                return false;
            }

            previousHyphen = character == '-';
        }

        return value[0] != '-' && value[^1] != '-';
    }
}
