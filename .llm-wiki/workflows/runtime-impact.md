---
id: workflow-runtime-impact
kind: workflow
status: current
title: Review runtime and integration impact
summary: Find deployable services, workers, external clients, webhooks, and recurring jobs related to a change.
tags:
  - workflow
  - runtime
  - integrations
  - resilience
sources:
  - .llm-wiki/generated/runtime-topology.json
  - .llm-wiki/tools/Find-LlmWikiRuntimeTopology.ps1
  - .llm-wiki/policies/change-policies.json
---

# Review runtime and integration impact

```powershell
./.llm-wiki/wiki.ps1 topology
./.llm-wiki/wiki.ps1 topology -Query MailRelay
```

For matched clients, workers, jobs, and webhooks, review cancellation, timeout,
retry/backoff, idempotency/replay, duplicate delivery, ordering, partial failure,
dead-letter/recovery, shutdown behavior, health/readiness, and telemetry.
