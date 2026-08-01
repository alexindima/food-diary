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
  - .llm-wiki/tools/Test-LlmWiki.ps1
  - .llm-wiki/tools/Test-LlmWikiLint.ps1
  - .llm-wiki/tools/Test-LlmWikiPortable.ps1
  - .llm-wiki/tools/Test-LlmWikiLinux.ps1
  - .llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1
  - .llm-wiki/tools/Invoke-LlmWikiFullVerification.ps1
  - .llm-wiki/wiki.ps1
  - .github/workflows/ci-tests.yml
---

# Run the Staged Index Pipeline

`wiki lint` is the fast prerequisite for both verification commands and CI. It
checks page contracts, sources, generated ownership, local links and anchors,
and high-confidence credential signatures before expensive index work begins.
Its isolated regression fixtures run immediately after it in the verification
gates.

The unified `wiki.ps1` entrypoint also pins ISO timestamps to JSON strings on
PowerShell versions that otherwise coerce them into `DateTime`. Nested tools
therefore compute the same deterministic hashes on Windows and Linux.
It forwards `-ProposedPath` to the direct `brief` and `test-plan` planning
commands so they can classify intended files before a Git diff exists.
The alias `-PlannedPath` accepts normal PowerShell arrays and a
semicolon-delimited convenience form. Context and privacy commands receive the
same normalized scope.

`wiki update`, `wiki verify`, `wiki verify-full`, and CI then use the same
dependency-aware index pipeline:

1. Source indexes run concurrently: catalog, C# symbols, frontend, frontend contracts, domain/data, configuration, runtime, and sensitive data.
2. Backend contracts and quality wait for the symbol index; module pages wait for the catalog.
3. Architecture health waits for catalog, backend contracts, frontend contracts, and quality.

The default concurrency is four processes and can be changed for constrained environments:

```powershell
./.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -MaxConcurrency 2
./.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -Check -MaxConcurrency 4
```

During iteration, select only indexes affected by the current diff:

```powershell
./.llm-wiki/wiki.ps1 update -AffectedOnly
./.llm-wiki/wiki.ps1 verify -AffectedOnly
./.llm-wiki/wiki.ps1 verify-fast
```

`verify-fast` is the explicit local iteration gate. It runs lint, the
dependency-aware affected-index check, change policy, and source-impact review,
then reminds the caller to run the full `verify` before handoff. If any index
check is stale, the pipeline emits one canonical `wiki.ps1 update` repair
command in addition to the focused `update -AffectedOnly` option.

Use `-BaseRef <ref>` for a committed range or `-ChangedPath <path[]>` for an
explicit scope. The conservative dependency map still runs derived indexes and
architecture health when their source indexes can change. Final handoff and CI
continue to use the full pipeline.

An Angular `*.spec.ts`-only change selects quality plus its downstream
architecture-health check. It does not run frontend source, frontend contract,
or sensitive-data generators because test content cannot change those indexes.

Workers are isolated PowerShell processes with real exit-code propagation. A failed worker fails its stage and prevents dependent stages from running. Parallelism changes execution time only; every existing generator and freshness check still runs.

`wiki verify` is the interactive gate. `wiki verify-full` and CI additionally
run the portable contract and complete stateful tool-smoke suite. Full
verification completes the index freshness checks before starting the stateful
tools. This prevents tool-smoke readers from observing generated files while
index workers are replacing them. Index workers remain concurrent within their
dependency-aware stage, and every non-zero exit is propagated. CI runs this gate
in its own job alongside backend, PostgreSQL, dependency, and frontend jobs, so
Wiki verification does not serialize the backend test pipeline.

Use the focused commands before the full gate:

```powershell
./.llm-wiki/wiki.ps1 smoke -SmokeGroup portable
./.llm-wiki/wiki.ps1 smoke -SmokeGroup linux
./.llm-wiki/wiki.ps1 smoke -SmokeGroup tools
```

The Linux group uses the pinned
`mcr.microsoft.com/powershell:7.5-ubuntu-24.04` Docker image on non-Linux
workstations and directly executes the portable group on Linux.

After these gates, CI publishes the compiled LLM Wiki change-review report to
the GitHub job summary so reviewers see the same scope, risk, and readiness
assessment.
