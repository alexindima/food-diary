---
id: workflow.review-diff
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiImpact.ps1
  - .llm-wiki/tools/Add-LlmWikiSourceReview.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskBaseline.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/wiki.ps1
  - AGENTS.md
---

# Review Change Context

`verify` reports its selected stages; only `verify-full` can record full verification after a matching complete generation check. Review the publication state separately from graph freshness, and reconcile affected Wiki narratives when their declared sources change.

Use the diff context command before implementation handoff or PR review:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1
```

By default it compares the working tree, index, and untracked source files with
`HEAD`. Untracked QA evidence under `output/playwright`, `output/maintenance`,
`output/qa` and `output/wiki-feedback` is reported as an operational artifact;
explicit `-ChangedPath` still includes it. For a committed change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1 `
  -BaseRef origin/master `
  -HeadRef HEAD
```

The `wiki.ps1 diff`, `impact`, and `verify` facade commands use the captured task
delta when the current session has a baseline and no explicit `-ChangedPath`.
`start` and `develop` capture pre-existing working changes in that baseline, so a
new task can have an empty delta while other chats still have uncommitted work.
For a shared-master review, pass the complete reviewed path set explicitly with
`-ChangedPath $sharedPaths`. An empty task delta does not establish that the full
shared working tree was reviewed.

For a task that owns only part of a shared working tree, pass its complete owned
path array to `task-status`, `task-refresh`, and `task-finish` explicitly:

```powershell
./.llm-wiki/wiki.ps1 task-finish -WorkspacePath $taskWorkspace -ChangedPath $ownedPaths
```

Finish preserves that array through both fresh status checks and refresh. It
still enforces every readiness gate and rejects explicit foreign paths. Without
`-ChangedPath`, finish keeps the task contract's existing Git comparison: the
default working-tree comparison includes the whole diff, while an explicitly
declared committed comparison remains intact. It does not infer scope from
allowed-path patterns or the session baseline. This bounded task completion
does not replace the full shared-master Wiki review above.

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

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.

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
it while those hashes match for sources requiring semantic review. Numeric line
coordinates in the two recognized navigation indexes do not invalidate an
otherwise current receipt. Contract values, paths and unknown schemas still
require review. JSON output includes `ReviewReason` and `ReviewRequiredSources`
to distinguish a missing receipt, changed page, and changed source content.
Reviewing a task's source subset retains still-current hashes for other declared
sources on the same unchanged page, together with their original `reviewReason`.
Changed page text, removed sources and changed source bytes cannot inherit that
coverage. Repeating the same review remains idempotent.

Generic usage guides cite their generators and query contracts. Cite current
generated records when the page actually asserts their data, rather than making
every metrics update require another review of unchanged command instructions.

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

The receipt ledger stores current per-page review evidence. Adding or replacing
a receipt records a decision; it does not by itself change the review workflow
documented here.

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
