---
id: workflow.frontend-contract-review
title: Frontend contract review
kind: workflow
status: current
sources:
  - .llm-wiki/generated/frontend-contract-index.json
  - .llm-wiki/tools/Find-LlmWikiFrontendContract.ps1
  - FoodDiary.Web.Client/AGENTS.md
---

# Frontend contract review

For a changed component, query its selector, inputs, outputs, template, direct spec, translations, and API calls:

```powershell
./.llm-wiki/wiki.ps1 ui -FrontendView components -Query Autocomplete
./.llm-wiki/wiki.ps1 ui -FrontendView consumers -Query fd-ui-autocomplete
./.llm-wiki/wiki.ps1 brief -ChangedPath FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/autocomplete/fd-ui-autocomplete.ts
```

Preserve or explicitly migrate public inputs and output payloads. Exercise loading, empty, error, disabled, and permission states where relevant. Verify accessible naming, semantics, keyboard navigation, focus transitions, and error announcements. Shared UI-kit changes need consumer-aware rendered evidence at representative viewport sizes.
