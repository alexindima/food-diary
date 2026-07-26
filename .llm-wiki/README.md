# FoodDiary LLM Wiki

This directory is a compiled navigation and knowledge layer for coding agents.
It summarizes repository knowledge, but it is not a source of truth.

## Authority Model

When sources disagree, use this precedence:

1. Executable code, tests, project manifests, and runtime configuration.
2. Accepted ADRs for the decisions they record.
3. Current living documentation under `docs/`.
4. Applicable `AGENTS.md` instructions.
5. Pages in this directory.

`AGENTS.md` has a special role: it is authoritative for how an agent must work in
its scope even when a wiki page provides broader context.

## Page Contract

Every knowledge page except this README must start with front matter containing:

```yaml
---
id: stable.unique.id
kind: index|system|module|workflow
status: current|draft|stale
generated_by: optional/path/to/deterministic-generator
sources:
  - path/to/source
---
```

Rules:

- `id` is stable and unique.
- `sources` use repository-relative paths and must exist.
- Important claims link to their canonical source.
- A wiki page must not silently introduce a new architectural rule.
- Generated pages declare `generated_by` and are validated by that generator's
  check mode instead of manual freshness review.
- Set `status: stale` when the sources no longer support the summary.
- Prefer updating an existing page over creating an overlapping page.

## Usage

Start at [index.md](index.md). Follow the smallest relevant set of pages, then
open the cited source files before changing code.

The unified developer entrypoint is:

```powershell
./.llm-wiki/wiki.ps1 help
./.llm-wiki/wiki.ps1 update
./.llm-wiki/wiki.ps1 verify
./.llm-wiki/wiki.ps1 context -Module Billing -ChangeType Api
./.llm-wiki/wiki.ps1 diff
```

`verify` checks page structure, generated catalogs, generated module pages,
symbol extraction, freshness, and developer-tool smoke scenarios.

Verify the wiki from the repository root:

```powershell
./.llm-wiki/tools/Test-LlmWiki.ps1
```

Regenerate and verify the machine-readable repository catalog:

```powershell
./.llm-wiki/tools/Build-LlmWikiCatalog.ps1
./.llm-wiki/tools/Build-LlmWikiCatalog.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiModulePages.ps1
./.llm-wiki/tools/Build-LlmWikiModulePages.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1
./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1 -Check
./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1
./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1 -Check
```

Build a compact task context:

```powershell
./.llm-wiki/tools/Find-LlmWikiContext.ps1 -Module Billing -ChangeType Api
```

Analyze the current change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1
```

Review pages affected by local changes:

```powershell
./.llm-wiki/tools/Get-LlmWikiImpact.ps1
```

Enforce freshness for a Git change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiImpact.ps1 -BaseRef origin/main -HeadRef HEAD -FailOnUnreviewed
```

When a declared source changes, the corresponding page must also change in the
same change set or already have `status: stale`. A page edit means its summary
was reviewed; use `stale` when it cannot yet be reconciled with the source.

The verifier is deterministic. Generating or refreshing prose may use an LLM,
but CI verification must not require a model or network access.
