---
id: system.configuration-index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiConfigurationIndex.ps1
sources:
  - .llm-wiki/generated/configuration-index.json
---

# Configuration Index

The generated configuration index inventories `*Options` types, declared section
names and properties, flattened `appsettings*.json` keys, and variables declared
in root environment examples. Use it to find configuration consumers and detect
templates that may need synchronization. It records key names only and must
never contain secret values.

Use `./.llm-wiki/wiki.ps1 configuration -Query <term>` to filter option types,
flattened keys, and environment-variable names. Queries are token-based and do
not expose configuration values.
