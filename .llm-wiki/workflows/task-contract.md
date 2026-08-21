---
id: workflow-task-contract
kind: workflow
status: current
title: Enforce a task contract
summary: Detect accidental file changes outside an explicitly declared task scope.
tags:
  - workflow
  - scope
  - safety
sources:
  - .llm-wiki/tools/Manage-LlmWikiTaskContract.ps1
---

# Enforce a task contract

For a bounded implementation, declare the objective and allowed repository paths:

```powershell
./.llm-wiki/wiki.ps1 task-init `
  -Objective "Add fasting reminder preferences" `
  -AllowedPath @(
    "^FoodDiary\\.Application/Fasting/",
    "^FoodDiary\\.Web\\.Client/src/app/features/fasting/",
    "^tests/.*/Fasting/"
  )

./.llm-wiki/wiki.ps1 task-validate -FailOnOutOfScope
```

By default validation uses the current diff from the contract's captured base.
For a deterministic check inside a dirty or shared worktree, pass the exact
`-ChangedPath <path[]>` set; validation then evaluates only that explicit delta
against the same allow/exclude patterns.

The default contract lives under `.artifacts/` and is not committed. Patterns are
.NET regular expressions over repository-relative paths. Include expected tests,
documentation, generated artifacts, and contract snapshots in scope. This guard
detects accidental edits; it does not authorize destructive or external actions.
