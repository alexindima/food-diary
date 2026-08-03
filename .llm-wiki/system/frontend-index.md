---
id: system.frontend-index
kind: system
status: current
sources:
  - .llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1
  - FoodDiary.Web.Client/angular.json
  - FoodDiary.Web.Client/AGENTS.md
---

# Frontend Index

[`frontend-index.json`](../generated/frontend-index.json) provides deterministic
Angular workspace discovery:

- client and admin features;
- exported TypeScript classes classified as components, directives, pipes,
  services, facades, API clients, resolvers, and guards;
- component selectors and source locations;
- literal route paths from route files;
- focused `*.spec.ts` files per feature;
- English/Russian locale-file presence and recursive JSON-property counts.

The locale counts are a fast structural signal, not a replacement for
`npm run check:i18n`. Dynamic routes and functional exports that are not
exported classes may require direct source inspection.

```powershell
./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1
./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1 -Check
```

Local `verify-fast` may reuse a content-addressed receipt when TypeScript and
locale inputs, generator/helper implementation, and generated output are
unchanged. Strict `wiki verify` always recomputes the index.
