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

Multi-step authentication flows should also trace transient state across
components, services, and navigation. API-call discovery covers direct
`HttpClient` calls and `get/post/put/patch/delete` helpers invoked by classes
extending `ApiService`. Inherited calls record the owning public method, base
URL expression, endpoint argument, and combined URL expression, so queries such
as `linkGoogle` and `google/link` resolve the same call.

Account-settings components that expose external sign-in providers should treat
provider status as a public UI contract: review the profile response field, the
connected and unconnected render branches, the credential output, the consuming
page, and the unavailable-provider fallback together. Keep the provider
credential transient and route the HTTP mutation through the owning facade.
