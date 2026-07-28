---
id: system.sensitive-data-index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiSensitiveDataIndex.ps1
sources:
  - .llm-wiki/generated/sensitive-data-index.json
  - docs/backend/PERSONAL_DATA_LIFECYCLE.md
  - docs/privacy/PRIVACY_RELEASE_CHECKLIST.md
---

# Sensitive Data Index

The generated index finds name-based candidate credential, identity, health,
financial, and private-content fields, highlights boundary DTO/integration files,
and records possible logging review leads. It stores names and source locations,
never runtime values.

Every item is a candidate requiring source inspection. Name matching cannot prove
that a field contains personal data or that a logging call emits its value.
