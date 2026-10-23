---
id: system.frontend-contract-index
title: Frontend contract index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiFrontendContractIndex.ps1
sources:
  - .llm-wiki/generated/frontend-contract-index.json
  - .llm-wiki/tools/Build-LlmWikiFrontendContractIndex.ps1
  - .llm-wiki/wiki.ps1
---

# Frontend contract index

The generated index makes Angular component boundaries queryable: selectors, signal inputs, outputs, neighboring specs, direct HTTP calls, template translation keys, and downstream selector consumers with their bound inputs and handled outputs.

Use `./.llm-wiki/wiki.ps1 ui -FrontendView components -Query <name>` before changing a shared component. Use `spec-gaps`, `api`, or `translations` to narrow the view.

Use `./.llm-wiki/wiki.ps1 ui -FrontendView consumers -Query <selector>` to inspect the blast radius of a public UI contract.

Regenerate with `./.llm-wiki/wiki.ps1 frontend-contract`; freshness is enforced by `verify` and CI.

Local `verify-fast` and ordinary `wiki verify` may reuse a content-addressed
receipt when TypeScript and template inputs, generator/helper implementation,
and generated output are unchanged. `wiki verify-strict-affected` recomputes
the selected affected checks without cache reuse.
