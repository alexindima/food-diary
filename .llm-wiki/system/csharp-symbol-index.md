---
id: system.csharp-symbol-index
kind: system
status: current
sources:
  - .llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
  - Directory.Build.props
---

# C# Symbol Index

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

The index excludes tests, EF migrations, generated files, `obj`, and `bin`.
Mappings are discovery hints, not proof of runtime resolution or inheritance.

```powershell
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1 -Check
```
