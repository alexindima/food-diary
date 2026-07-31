---
id: index
kind: index
status: current
sources:
  - AGENTS.md
  - docs/README.md
  - docs/ARCHITECTURE.md
  - docs/BACKEND_MODULE_MAP.md
---

# FoodDiary Knowledge Index

FoodDiary is a modular monolith with separately deployed supporting services.
This index routes agents to compiled context; the linked sources remain
authoritative.

## System

- [Knowledge model](system/knowledge-model.md) — authority, provenance, and
  update rules for this wiki.
- [Architecture](system/architecture.md) — deployable units, layers, and the
  main dependency boundaries.
- [Repository catalog](system/repository-catalog.md) — generated project,
  module, HTTP, test, and documentation inventory.
- [C# symbol index](system/csharp-symbol-index.md) — handlers, validators,
  repositories, services, domain types, and DI registrations.
- [Backend contract index](system/backend-contract-index.md) — commands,
  queries, events, DTOs, interfaces, and their production/test consumers.
- [Frontend index](system/frontend-index.md) — Angular features, routes,
  symbols, tests, and localization pairs.
- [Frontend contract index](system/frontend-contract-index.md) — component
  selectors, signal inputs/outputs, API calls, translation usage, and spec gaps.
- [Domain and data index](system/domain-data-index.md) — domain types,
  guarded invariants, EF mappings, indexes, and relationships.
- [Configuration index](system/configuration-index.md) — options, section names,
  appsettings keys, and environment-variable names.
- [Quality index](system/quality-index.md) — structural hotspots, test-reference
  gaps, and explicit debt markers.
- [Runtime topology](system/runtime-topology.md) — deployable services, workers,
  external clients, webhooks, and recurring jobs.
- [Sensitive data index](system/sensitive-data-index.md) — candidate credential,
  identity, health, financial, private-content, boundary, and logging surfaces.
- [Architecture health index](system/architecture-health-index.md) — enforced
  dependency drift plus carefully labelled removal, spec, and test-gap candidates.

## Areas

- [Primary backend](modules/primary-backend.md) — core FoodDiary backend
  projects and module interaction.
- [Application module index](generated/modules/index.md) — generated pages for
  all modules with dependencies, consumers, endpoints, source areas, and tests.
- [Mail services](modules/mail-services.md) — MailRelay and MailInbox service
  boundaries.
- [Frontend](modules/frontend.md) — Angular application, admin application,
  UI kit, and tour engine.

## Workflows

- [Route a change through adaptive AI development](workflows/adaptive-development.md)
- [Map changes to FoodDiary product journeys](workflows/product-journey-impact.md)
- [Generate an implementation plan](workflows/implementation-plan.md)
- [Compile a change packet](workflows/change-packet.md)
- [Map acceptance criteria to evidence](workflows/acceptance-evidence-matrix.md)
- [Evaluate release readiness](workflows/release-readiness.md)
- [Publish a change review report](workflows/review-report.md)
- [Start a governed AI task workspace](workflows/task-workspace.md)
- [Govern a change with a manifest](workflows/change-manifest.md)
- [Prove acceptance criteria from the implemented change](workflows/proof-of-change.md)
- [Analyze and expand acceptance requirements](workflows/requirement-intelligence.md)
- [Simulate change impact before implementation](workflows/impact-simulation.md)
- [Run a controlled repair loop](workflows/controlled-repair-loop.md)
- [Predict and calibrate verification failures](workflows/failure-prediction.md)
- [Prioritize verification by expected engineering cost](workflows/cost-aware-verification.md)
- [Learn from verification outcomes](workflows/verification-telemetry.md)
- [Protect agent context from instruction injection](workflows/context-security.md)
- [Explain confidence in an AI task result](workflows/confidence-ledger.md)
- [Independently critique an AI-authored change](workflows/independent-change-critique.md)
- [Learn from completed AI tasks](workflows/post-task-retrospective.md)
- [Promote repeated task learnings under review](workflows/controlled-learning-promotion.md)
- [Run the staged index pipeline](workflows/index-pipeline.md)
- [Build a task brief](workflows/task-brief.md)
- [Generate a change-aware test plan](workflows/test-plan.md)
- [Review architecture decision context](workflows/decision-context.md)
- [Review performance and observability risk](workflows/performance-observability-review.md)
- [Review dependencies and rollout](workflows/dependency-rollout.md)
- [Review structural hotspots and test gaps](workflows/quality-risk.md)
- [Review runtime and integration impact](workflows/runtime-impact.md)
- [Review sensitive data lifecycle](workflows/privacy-review.md)
- [Enforce a task contract](workflows/task-contract.md)
- [Review API compatibility](workflows/api-compatibility.md)
- [Review ownership and downstream impact](workflows/ownership-impact.md)
- [Trace a backend feature](workflows/trace-feature.md)
- [Reuse failure knowledge](workflows/failure-knowledge.md)
- [Promote task learnings into durable memory](workflows/durable-memory.md)
- [Capture frontend visual evidence](workflows/frontend-visual-evidence.md)
- [Review frontend contracts](workflows/frontend-contract-review.md)
- [Review domain and data contracts](workflows/domain-data-review.md)
- [Review backend contract consumers](workflows/backend-contract-review.md)
- [Review architecture drift and removal candidates](workflows/architecture-health-review.md)

- [Query repository context](workflows/query-context.md) — build a compact,
  ranked context packet for a module or change type.
- [Review change context](workflows/review-diff.md) — infer affected modules,
  guides, wiki pages, risks, and checks directly from a Git diff.
- [Change policy](workflows/change-policy.md) — turn changed paths into
  deterministic checks, review obligations, and structural invariants.
- [Development evidence](workflows/evidence-bundle.md) — record checks and
  review decisions, validate completion, and generate a handoff summary.
- [AI development evals](workflows/evals.md) — regression cases for diff
  classification, policy routing, and structural violations.

## Canonical Entrypoints

- [Repository instructions](../AGENTS.md)
- [Documentation index](../docs/README.md)
- [Architecture](../docs/ARCHITECTURE.md)
- [Backend module map](../docs/BACKEND_MODULE_MAP.md)
- [Testing strategy](../docs/TESTING_STRATEGY.md)
- [ADR index](../docs/adr/README.md)

## Query Protocol

1. Open the page matching the task area.
2. Follow its `sources` and inline links.
3. Read the nearest applicable `AGENTS.md`.
4. Confirm change-sensitive facts in code, tests, manifests, or configuration.
5. If the wiki conflicts with stronger evidence, follow the stronger evidence
   and mark or update the wiki page.
