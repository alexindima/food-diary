# Documentation Index

This directory contains long-form repository documentation. Keep root-level markdown limited to entrypoint files such as `README.md` and `AGENTS.md`.

## Agent Knowledge Layer

- `.llm-wiki/index.md` - compiled, source-linked repository map for coding agents. It is a derived navigation layer; this documentation, scoped `AGENTS.md`, ADRs, tests, and code remain authoritative.
- `ai/CODE_REVIEW.md` - shared correctness, architecture, security, compatibility, frontend, and evidence rubric for AI-assisted review.
- `.llm-wiki/generated/configuration-index.json` - generated key-name-only map of options, appsettings, and environment examples.
- `.llm-wiki/generated/quality-index.json` - generated structural hotspot, test-reference, and explicit debt inventory.
- `.llm-wiki/generated/runtime-topology.json` - generated Compose service, worker, HTTP client, webhook, and recurring-job inventory.
- `.llm-wiki/generated/sensitive-data-index.json` - generated name-based sensitive-field and boundary review inventory without runtime values.

## Architecture

- `ARCHITECTURE.md` - system architecture, deployable units, and dependency boundaries.
- `BACKEND_MODULE_MAP.md` - backend project/module map and placement guidance.
- `backend/BACKEND_MODULE_OWNERSHIP.md` - business-module data ownership and allowed interaction types.
- `architecture/module-dependencies.json` - executable, acyclic Application module dependency graph.
- `TESTING_STRATEGY.md` - test project responsibilities and when to run each suite.
- `adr/README.md` - architecture decision record index, lifecycle, and authoring guidance.

## Backend Operations And Governance

- `backend/ARCHITECTURE_IMPROVEMENT_ROADMAP.md`
- `backend/BACKEND_API_CONTRACT_GOVERNANCE.md`
- `backend/BACKEND_CRITICAL_FLOW_MATRIX.md`
- `backend/BACKEND_DEFINITION_OF_DONE.md`
- `backend/BACKEND_EVENT_TAXONOMY.md`
- `backend/BACKEND_FEATURE_FIRST_COMMON_INVENTORY.md`
- `backend/BACKEND_MIGRATION_SAFETY.md`
- `backend/BACKEND_OBSERVABILITY_BASELINE.md`
- `backend/BACKEND_PERFORMANCE_REVIEW.md`
- `backend/BACKEND_RUNBOOKS.md`
- `backend/MARKETING_ATTRIBUTION_RUNBOOK.md`
- `backend/BACKEND_SECURITY_HARDENING.md`
- `security/THREAT_MODEL.md` - repository-wide assets, trust boundaries, attacker stories, and severity calibration.
- `backend/BACKEND_TIME_POLICY.md`

## Frontend

- `frontend/FRONTEND_ARCHITECTURE.md`
- `frontend/FRONTEND_OBSERVABILITY_BASELINE.md`
- `frontend/DESIGN_SYSTEM_REVIEW.md` - living page inventory, token contract, visual QA matrix, and findings ledger.

## Privacy

- `privacy/PRIVACY_RELEASE_CHECKLIST.md` - owner facts, engineering evidence, and legal sign-off required before treating the privacy policy as release-ready.

## Plans

`plans/` contains active product, feature, SEO, and integration plans. Remove implemented or stale plans once durable decisions are captured in ADRs, current guides, or project instructions.

## Archive

`archive/` contains outdated or historical root documents that are not current operational guidance. Do not use archived files as source of truth unless a current guide explicitly references them.

Codex Cloud smoke test completed.
