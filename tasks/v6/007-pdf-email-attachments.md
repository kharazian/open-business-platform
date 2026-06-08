# V6 Task 007: PDF Email Attachments

## Goal

Attach generated record PDFs from published print templates to trigger email actions.

## Requirements

- Extend the shared email sender contract with PDF attachment metadata.
- Keep password recovery and existing trigger/workflow email sends compatible.
- Allow `send_email` trigger actions to select one same-form record print template with a published version.
- Reject scheduled-trigger PDF attachments because they do not have a current record context.
- Generate the PDF at trigger execution time from the latest published record template version.
- Add audit metadata for PDFs attached through trigger email delivery.
- Add trigger builder serialization, validation, and UI controls for the selected record PDF attachment.

## Acceptance Criteria

- [x] Email messages can carry `application/pdf` attachments.
- [x] Trigger validation accepts published same-form record print template attachments.
- [x] Trigger validation rejects missing, unpublished, non-record, cross-form, or scheduled PDF attachments.
- [x] Trigger execution attaches generated record PDFs to `send_email` messages.
- [x] Trigger builder saves and reloads the selected print template id.
- [x] The trigger UI exposes published record print templates on email actions.
- [x] Tests/builds pass.

## Out of Scope

- Scheduled trigger PDF attachments.
- Report PDF email attachments.
- Multiple PDF attachments per email action.
- Long-running background attachment jobs.
- Per-recipient field-level rendering.
