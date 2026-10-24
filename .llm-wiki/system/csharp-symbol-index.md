---
id: system.csharp-symbol-index
kind: system
status: current
sources:
  - .llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
  - .llm-wiki/tools/LlmWikiGitPaths.ps1
  - .llm-wiki/tools/LlmWikiIndexCache.ps1
  - .llm-wiki/tools/Test-LlmWikiSymbolIndexCache.ps1
  - Directory.Build.props
---

# C# Symbol Index

Source discovery requests only C# paths from Git, including case variants and
non-ignored untracked files. Deleted tracked paths and ignored build trees are
excluded before file reads; declaration and DI extraction retain their filters.

`-ReuseUnchangedCheck` reuses a successful local receipt only when the complete
production source path/content set, generator and helpers, PowerShell/runtime,
culture, default encoding and exact output bytes still match. Missing/corrupt
receipts or changed inputs fall back to full extraction. Missing/stale output
still fails check mode. Full extraction checks the input set/content again before
publishing a receipt; equivalent custom JSON formatting can pass full checking
without becoming eligible for reuse. Omit the flag to force extraction.
The pipeline forwards the opt-in through `-ReuseUnchangedChecks`.

[`csharp-symbol-index.json`](../generated/csharp-symbol-index.json) is a
deterministic navigation index for production C# code.

It extracts:

- public and internal classes, interfaces, records, structs, and enums;
- semantic roles inferred from names and folders, including handlers,
  validators, repositories, services, commands, queries, entities, and value
  objects;
- interface-to-implementation candidates following the `IName` → `Name`
  convention;
- literal `AddScoped`, `AddTransient`, and `AddSingleton` registrations.

The index excludes tests, dependency sources under `node_modules`, EF
migrations, generated files, `obj`, and `bin`. This prevents bundled native
build helpers from appearing as application contracts.
Mappings are discovery hints, not proof of runtime resolution or inheritance.

```powershell
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1 -Check -ReuseUnchangedCheck
```
