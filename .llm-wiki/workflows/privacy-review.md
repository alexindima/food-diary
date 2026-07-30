---
id: workflow-privacy-review
kind: workflow
status: current
title: Review sensitive data lifecycle
summary: Find candidate sensitive fields and review collection, authorization, storage, sharing, logging, export, retention, and deletion.
tags:
  - workflow
  - privacy
  - sensitive-data
sources:
  - .llm-wiki/generated/sensitive-data-index.json
  - .llm-wiki/tools/Find-LlmWikiSensitiveData.ps1
  - docs/backend/PERSONAL_DATA_LIFECYCLE.md
  - docs/privacy/PRIVACY_RELEASE_CHECKLIST.md
---

# Review sensitive data lifecycle

```powershell
./.llm-wiki/wiki.ps1 privacy -Category credential
./.llm-wiki/wiki.ps1 privacy -Category logging
./.llm-wiki/wiki.ps1 privacy -Category boundaries -Query Export
```

For a changed field or flow, review purpose/minimization, consent or lawful
basis, ownership/authorization, encryption and secret handling, cache/queue/log
copies, provider sharing, export, retention/deletion, backups, telemetry, and
user-facing disclosure. Confirm every candidate against source semantics.
Plain fields named `Token` are classified as credential candidates as well as
more specific access, refresh, and hash forms; callers must still confirm the
field's semantics in source.

External identity credentials used to bridge an anonymous login attempt into an
authenticated linking request should remain in memory only. Do not place them
in URLs, router state persisted across reloads, browser storage, logs,
telemetry, queues, or error messages.

The index also reports `externalTransfers`: integration clients that combine
an absolute external HTTP destination with image, prompt, description, text,
food, nutrition, or similar sensitive parameters. Treat these entries as
provider-sharing review leads. Verify the actual payload, consent, retention,
logging, metadata, and provider policy in source.
