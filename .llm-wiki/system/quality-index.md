---
id: system.quality-index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiQualityIndex.ps1
sources:
  - .llm-wiki/generated/quality-index.json
  - .llm-wiki/generated/csharp-symbol-index.json
---

# Quality Index

The generated quality index ranks structural hotspots from non-blank lines,
decision-point proxies, critical handler/controller/validator symbols, and
critical symbols without direct test-source references. It also inventories
explicit TODO/FIXME/HACK and warning-suppression markers outside generated,
migration, dependency, and test artifacts.

`testReferenceCount` means that the exact symbol name appears in a test source
file. It is a navigation signal, not execution, branch, line, mutation, or
behavioral coverage.

Local `verify-fast` may reuse a content-addressed receipt when source inputs,
generator implementation, symbol index, and generated output are byte-for-byte
unchanged. Strict `wiki verify` always performs the full computation.
