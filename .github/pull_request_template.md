## Summary

- 

## Validation

- [ ] `dotnet` / `npm` checks relevant to the change were run
- [ ] deploy or runtime-impacting changes were verified locally or on staging where appropriate
- [ ] `./.llm-wiki/wiki.ps1 policy` was reviewed for the change set
- [ ] evidence obligations are resolved or explicitly handed off
- [ ] change manifest is valid with evidence, or is explicitly not applicable
- [ ] every acceptance criterion is mapped and evidence-backed, or the matrix is explicitly not applicable
- [ ] release readiness is `ready`, or every conditional/blocked dimension is explicitly handed off

Evidence summary (optional):

<!-- Paste output from ./.llm-wiki/wiki.ps1 handoff when formal evidence was collected. -->
<!-- It includes changed paths, affected scopes/modules, checks, reviews, and unresolved obligations. -->
<!-- Manifest command: ./.llm-wiki/wiki.ps1 manifest-validate -RequireEvidence -FailOnInvalid -->
<!-- Acceptance command: ./.llm-wiki/wiki.ps1 acceptance-validate -RequireEvidence -FailOnInvalid -->
<!-- Readiness command: ./.llm-wiki/wiki.ps1 readiness -RequireManifest -RequireAcceptance -RequireEvidence -FailOnNotReady -->

## API Contract Review

- [ ] no backend HTTP contract change
- [ ] backend HTTP contract changed intentionally and relevant snapshots were reviewed/updated
- [ ] OpenAPI / error / payload snapshot impact was checked
- [ ] status-code, auth, and error-shape changes are called out below if applicable

Reference:

- `BACKEND_API_CONTRACT_GOVERNANCE.md`

Notes:

- 

## Security Review

Mark each touched area as `ok`, `risk accepted`, `follow-up`, or `n/a`.

- Authentication and session flows:
- Admin surface:
- Upload and asset flows:
- Telegram and external adapters:
- Proxy, network, and request trust:
- Data mutation safety:
- Secrets and deployment:
- Dependency and package posture:

Reference:

- `BACKEND_SECURITY_HARDENING.md`
- `../docs/security/THREAT_MODEL.md`

## Deploy / Operations Notes

- [ ] no deploy/runtime impact
- [ ] requires staging verification
- [ ] requires release/staging promotion security checklist
- [ ] requires secret/config change
- [ ] requires migration or data-shape review

Notes:

- 

## Architecture / Performance / Observability

- ADR review: `n/a`, `existing decision`, `new/superseding ADR`, or explanation
- Query/performance impact: `n/a`, tested path/cardinality/index evidence, or follow-up
- Telemetry impact: `n/a`, signals/outcomes verified, or follow-up
- Dependency impact: `n/a`, audited direct/transitive graph and compatibility, or follow-up
- Configuration impact: `n/a`, synchronized templates/validation/secrets/deployment values, or follow-up
- Rollout/rollback: standard path or link/summary of ordering, signals, and data-safe recovery
- Quality hotspots/test gaps: `n/a`, reviewed with focused coverage, or follow-up
- Runtime/integration impact: `n/a`, reviewed clients/workers/jobs/webhooks and resilience semantics, or follow-up
- Privacy/data lifecycle: `n/a`, reviewed collection/storage/sharing/logging/export/retention/deletion, or follow-up

Helpful commands:

- `./.llm-wiki/wiki.ps1 decision`
- `./.llm-wiki/wiki.ps1 plan -Objective "<outcome>"`
- `./.llm-wiki/wiki.ps1 manifest-validate -RequireEvidence -FailOnInvalid`
- `./.llm-wiki/wiki.ps1 test-plan`
- `./.llm-wiki/wiki.ps1 dependencies -BaseRef origin/master`
- `./.llm-wiki/wiki.ps1 rollout`
- `./.llm-wiki/wiki.ps1 hotspots`
- `./.llm-wiki/wiki.ps1 test-gaps`
- `./.llm-wiki/wiki.ps1 topology`
- `./.llm-wiki/wiki.ps1 privacy -PrivacyCategory logging`

## Risk Acceptance / Follow-Up

- 
