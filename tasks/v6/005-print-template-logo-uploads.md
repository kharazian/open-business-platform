# V6 Task 005: Print Template Logo Uploads

## Goal

Let template builders add small local logo images to print template headers without adding a server-side asset store or PDF dependency.

## Requirements

- Keep the existing `config.header.logoUrl` contract.
- Allow safe `http`, `https`, and image `data:` logo sources.
- Reject unsafe logo sources such as `javascript:` URLs.
- Add browser-side logo file validation for supported image types and small file sizes.
- Convert accepted uploaded logo files to data URLs and store them in the print template draft config.
- Show a logo preview and remove action in `/printing`.
- Keep published template versions snapshotting the logo source through existing template config versioning.

## Acceptance Criteria

- [x] Backend validation accepts safe logo URLs/data URLs and rejects unsafe logo sources.
- [x] Frontend validation rejects unsupported logo file types and oversized logo files.
- [x] `/printing` can upload, preview, and remove a logo from the template header.
- [x] Existing URL-based logo entry still works.
- [x] Tests/builds pass.

## Out of Scope

- Server-side asset storage.
- Image cropping/editing.
- SVG upload support.
- Server-side binary PDF generation.
- PDF email attachments.
