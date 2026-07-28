---
id: workflow-index-pipeline
kind: workflow
status: current
title: Run the staged index pipeline
summary: Build or verify independent knowledge indexes concurrently while preserving their dependency order.
tags:
  - workflow
  - performance
  - ci
  - indexes
sources:
  - .llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1
  - .llm-wiki/wiki.ps1
  - .github/workflows/ci-tests.yml
---

# Run the Staged Index Pipeline

`wiki update`, `wiki verify`, and CI use the same dependency-aware pipeline:

1. Source indexes run concurrently: catalog, C# symbols, frontend, frontend contracts, domain/data, configuration, runtime, and sensitive data.
2. Backend contracts and quality wait for the symbol index; module pages wait for the catalog.
3. Architecture health waits for catalog, backend contracts, frontend contracts, and quality.

The default concurrency is four processes and can be changed for constrained environments:

```powershell
./.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -MaxConcurrency 2
./.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -Check -MaxConcurrency 4
```

Workers are isolated PowerShell processes with real exit-code propagation. A failed worker fails its stage and prevents dependent stages from running. Parallelism changes execution time only; every existing generator and freshness check still runs.

After these gates, CI publishes the compiled LLM Wiki change-review report to
the GitHub job summary so reviewers see the same scope, risk, and readiness
assessment.
