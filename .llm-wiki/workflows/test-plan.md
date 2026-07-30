---
id: workflow-test-plan
kind: workflow
status: current
title: Generate a change-aware test plan
summary: Select focused tests, required commands, and behavior/risk scenarios from the current diff.
tags:
  - workflow
  - testing
  - planning
sources:
  - docs/TESTING_STRATEGY.md
  - .llm-wiki/tools/Get-LlmWikiTestPlan.ps1
  - .llm-wiki/policies/change-policies.json
---

# Generate a change-aware test plan

When invoked through a task brief or change packet, the test planner reuses the
already classified diff and policy result instead of rescanning the same paths.

```powershell
./.llm-wiki/wiki.ps1 test-plan
```

Use `-ProposedPath <path>` before code exists; it may be repeated or supplied as
an array. The planner combines proposed and changed paths and preserves the
proposed paths in JSON output. When it resolves an existing Angular spec, its
focused command uses the project's `test:ci:*` script with Angular's supported
`--include` option.

The plan combines focused existing test files, executable commands, and scenario
prompts for backend, HTTP contracts, authorization, persistence, migrations,
frontend states, localization, security, and observability. Scenario prompts
still require the implementer to choose concrete inputs and assertions from the
changed behavior.

Focused tests are ranked by evidence: explicitly changed tests first, then a
changed production file's sibling spec, tests that directly reference changed
symbols, and finally broad downstream context. JSON includes
`focusedTestDetails` with the rank reason. Use `-Compact` to retain actionable
tests, commands, and scenarios while reducing context volume.

Focused Angular commands are derived from the actual `package.json` script and
the project's `angular.json` test builder. `--include` is emitted only for the
verified `@angular/build:unit-test` builder; otherwise the planner falls back to
the full project test script and says why in `commandEvidence`.
