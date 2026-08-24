---
id: workflow.review-diff
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiImpact.ps1
  - .llm-wiki/tools/Add-LlmWikiSourceReview.ps1
  - .llm-wiki/wiki.ps1
  - AGENTS.md
---

# Review Change Context

Use the diff context command before implementation handoff or PR review:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1
```

By default it compares the working tree, index, and untracked files with
`HEAD`. For a committed change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1 `
  -BaseRef origin/master `
  -HeadRef HEAD
```

The result identifies:

- backend, API, frontend, database, test, documentation, localization, and
  contract scopes;
- affected application modules and their dependency context;
- changed .NET projects and applicable scoped guides;
- wiki pages requiring review;
- focused test files and recommended verification commands;
- generated catalogs or module pages that need regeneration;
- contract, localization, migration, and cross-module warnings.

Use `-Format Json` when another agent or automation will consume the packet.
The output is advisory: actual requirements still come from scoped
`AGENTS.md`, source code, tests, contracts, and the user's request.

Repository-catalog metadata and changed C# symbols come from the refreshed
SQLite compiled-index projection by default. The query selects exact changed
paths before transporting symbol payloads and exposes source hashes, candidate
counts, SQL duration, and end-to-end round-trip duration in `compiledIndex`.
A missing or stale projection fails with the `graph-build` recovery command;
`-CompiledIndexSource Json` is an explicit parity/diagnostic baseline, not an
automatic fallback.

After reviewing the packet, run `wiki.ps1 policy` to enforce structural rules.
For higher-risk or handed-off work, initialize an evidence bundle and resolve
every required check and review obligation before completion.

When a declared source changed but review confirms that a workflow page needs
no textual update, record that decision instead of adding a mechanical note:

```powershell
./.llm-wiki/wiki.ps1 review `
  -Id workflow-change-manifest `
  -Reason "Compact brief output does not change manifest semantics."
```

The receipt stores the page and changed-source SHA-256 hashes. Freshness accepts
it only while those hashes still match; a later source or page edit
automatically requires a new review.

Review the current affected set with reasons grouped by review area:

```powershell
./.llm-wiki/wiki.ps1 review-affected `
  -ReviewAreaReason "api-compatibility=OpenAPI and consumers were checked." `
  -ReviewAreaReason "privacy-security=No sensitive-data boundary changed."
```

The command recomputes the current impact set, prints pending pages by area, and
records the applicable rationale only for still-pending pages. A single
`-Reason` spanning multiple areas is rejected unless
`-AllowSharedReviewReason` is supplied explicitly. Page IDs may also be used as
the left side of `-ReviewAreaReason`; pass an ID array to `review` when pages need
fully separate handling.

The receipt ledger is append-only review evidence. Adding a receipt records a
decision; it does not by itself change the review workflow documented here.

Freshness output prints both the page path and its internal ID:

```text
.llm-wiki/workflows/frontend-contract-review.md
[id: workflow.frontend-contract-review; current, needs review]
```

Copy the displayed ID directly into `wiki.ps1 review -Id ...`; no front-matter
lookup is required.

For assertions and automation, use
`Get-LlmWikiImpact.ps1 -Format Json`. The structured result exposes
`impactCount`, `unreviewedCount`, and each affected page's stable `Id`, path,
changed sources, and review state. Human-readable `Write-Host` output is not a
machine contract.
