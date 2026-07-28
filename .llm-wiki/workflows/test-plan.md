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

The plan combines focused existing test files, executable commands, and scenario
prompts for backend, HTTP contracts, authorization, persistence, migrations,
frontend states, localization, security, and observability. Scenario prompts
still require the implementer to choose concrete inputs and assertions from the
changed behavior.
