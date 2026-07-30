---
id: workflow.review-diff
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiImpact.ps1
  - .llm-wiki/tools/Add-LlmWikiSourceReview.ps1
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

The receipt ledger is append-only review evidence. Adding a receipt records a
decision; it does not by itself change the review workflow documented here.

Freshness output prints both the page path and its internal ID:

```text
.llm-wiki/workflows/frontend-contract-review.md
[id: workflow.frontend-contract-review; current, needs review]
```

Copy the displayed ID directly into `wiki.ps1 review -Id ...`; no front-matter
lookup is required.
