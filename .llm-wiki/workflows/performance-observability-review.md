---
id: workflow-performance-observability-review
kind: workflow
status: current
title: Review performance and observability risk
summary: Apply repository-specific query and telemetry guardrails to changed data-access and critical-flow code.
tags:
  - workflow
  - performance
  - observability
sources:
  - docs/backend/BACKEND_PERFORMANCE_REVIEW.md
  - docs/backend/BACKEND_OBSERVABILITY_BASELINE.md
  - .llm-wiki/policies/change-policies.json
---

# Review performance and observability risk

For persistence and query changes, review:

- no-tracking for read-only paths;
- query count and accidental N+1 behavior;
- `Include` fanout and split-query needs;
- bounded pagination and stable ordering;
- index fit for the actual filter and ordering;
- PostgreSQL translation and realistic-cardinality integration coverage.

For critical business/provider/job flows, review:

- stable success and failure outcomes;
- useful duration and retry/fallback signals;
- low-cardinality metric dimensions;
- correlation across transport, handler, provider, and job boundaries;
- exclusion of tokens, secrets, personal data, and unbounded identifiers.

Use `test-plan` for concrete test scenarios and resolve the resulting evidence
obligations before handoff.
