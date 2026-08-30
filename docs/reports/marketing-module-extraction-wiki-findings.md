# Marketing Module Extraction: Wiki Findings

## Useful evidence

- `start` correctly classified the change as critical, identified Database impact, and produced acceptance criteria covering application relocation, composition roots, cleanup registration, cancellation, retry safety, dependency policy, and graph acyclicity.
- `research` found the Billing conversion port, cleanup service, EF configuration, repository, and relevant Cycles/Images extraction precedents.
- `brief` identified all eight central Marketing-focused test files that required ownership decisions.

## False positives and false negatives

- `test-plan` recommended 12 MailInbox/MailRelay test files and seven unrelated mail test projects. It omitted every Marketing application/domain/job/presentation test named by `brief`. This is a repeatable cross-module ranking defect, not a Marketing-specific generator rule to patch.
- `topology -Query "MarketingAttributionCleanupJob AddMarketingModule IBillingMarketingConversionRecorder"` returned no records even though source and JobManager registration contain those symbols. The JSON fallback therefore missed the recurring cleanup job and all three executable composition roots.
- `privacy` returned unrelated refresh-token, impersonation, billing and fasting candidates, but missed `MarketingAttributionEvent` fields (`UserId`, `AnonymousId`, `SessionId`, referrer and UTM values), its 365-day retention policy, and cleanup implementation.
- `dependencies` reported zero changes because it ran before edits; it was not useful as a prospective dependency-impact analysis.
- `rollout` emitted only generic deployment guidance and did not identify the central DbContext/model-builder seam, Docker restore-copy updates, or the no-migration compatibility requirement.

## Environment limitation

Every relevant Wiki command reported unavailable TypeScript prerequisites and used the read-only JSON baseline. Source, tests, docs, ADRs, solution metadata, and Git precedents were treated as authoritative and used to verify or reject Wiki claims.

## Generator changes

No Wiki generator was modified. The observed omissions span test ranking, topology, and privacy indexes; fixing any one output specifically for Marketing would hide the underlying repeatable indexing/ranking defects.
