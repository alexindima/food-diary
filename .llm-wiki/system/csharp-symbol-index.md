---
id: system.csharp-symbol-index
kind: system
status: current
sources:
  - .llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
  - .llm-wiki/tools/LlmWikiGitPaths.ps1
  - Directory.Build.props
---

# C# Symbol Index

Source discovery uses one Git-visible inventory of tracked and non-ignored untracked files. Deleted tracked paths and ignored build trees are excluded before filesystem traversal; declaration and DI extraction retain their existing filters.

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
```
