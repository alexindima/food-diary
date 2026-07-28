---
id: system.backend-contract-index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiBackendContractIndex.ps1
sources:
  - .llm-wiki/generated/backend-contract-index.json
  - .llm-wiki/generated/csharp-symbol-index.json
---

# Backend Contract Index

The generated index maps commands, queries, events, requests, responses, and interfaces to production and test consumers.

```powershell
./.llm-wiki/wiki.ps1 contracts -BackendContractView consumers -Query StartFastingCommand
./.llm-wiki/wiki.ps1 contracts -BackendContractView production -Query IUserReadRepository
./.llm-wiki/wiki.ps1 contracts -BackendContractView tests -Query StartFastingCommand
```

Duplicate short type names are marked ambiguous because static text indexing cannot fully resolve namespaces. Confirm ambiguous edges in source before making compatibility decisions.
