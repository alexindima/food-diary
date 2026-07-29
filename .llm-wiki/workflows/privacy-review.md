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
