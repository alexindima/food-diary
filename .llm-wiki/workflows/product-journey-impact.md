---
id: workflow-product-journey-impact
kind: workflow
status: current
title: Map a change to FoodDiary product journeys
summary: Connect changed paths and task intent to durable end-to-end user scenarios, review areas, and evidence hints.
tags:
  - workflow
  - testing
  - product
  - journeys
sources:
  - .llm-wiki/knowledge/product-journeys.json
  - .llm-wiki/tools/Find-LlmWikiProductJourney.ps1
  - .llm-wiki/tools/Invoke-LlmWikiDeliveryWorkflow.ps1
---

# FoodDiary Product Journey Impact

Use the project-specific journey catalog to avoid validating only the edited unit
while missing the user flow around it:

```powershell
./.llm-wiki/wiki.ps1 journeys `
  -Intent "Fix the dietologist invitation email link" `
  -PlannedPath 'FoodDiary.Application/Dietologist','FoodDiary.Web.Client/src/app/features/dietologist'
```

The catalog covers authentication and account linking, dietologist collaboration,
AI photo analysis, meal tracking, the food catalog, billing, transactional mail,
and Telegram. Each journey declares stable scenario identifiers, elevated review
areas, risk, and evidence hints.

Journey matches are reviewed navigation evidence. They do not prove that a path is
executed at runtime and never override code, tests, accepted ADRs, current docs, or
scoped instructions. Map applicable scenario IDs into the governed acceptance
matrix and resolve them with current evidence.

When paths overlap multiple journeys, validate the boundary scenarios as well as
the primary one. For example, a dietologist invitation email change normally
touches both `FD-DIET` and `FD-MAIL`; an external account-linking change belongs to
`FD-AUTH` and may also require email or profile-state verification.

Review the catalog when a durable top-level product flow is introduced, removed,
or materially changes its trust boundary. Do not add every component or endpoint:
journeys describe observable user outcomes, not repository structure.

Alias matching uses token boundaries. A workflow word such as `replan` must not
match the billing alias `plan`. For frontend work, use `ui-trace` first when the
runtime-owning component is uncertain; journey scoring should consume confirmed
paths instead of making an early component guess authoritative.
