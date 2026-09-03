# Identity Telegram replay persistence

Baseline: clean master `e304b1148fd901a8531883ba45db11ba33441bfd`.
Scope is physical ownership of the existing guard, record, mapping and scoped DI,
plus focused tests. No new project, package, public HTTP behavior or migration is
intended. JWT/SSO, provider signature validation, UserRepository and outbox are out
of scope.

## Boundary and compatibility

TelegramVerify, TelegramLoginWidget and LinkTelegram are the three Identity
callers. They retain signature/age validation before consuming the assertion;
widget fingerprints still use the existing `widget:` prefix. The guard retains
SHA256 over UTF-8, lowercase hexadecimal storage, DELETE for expiry at/before the
configured clock, and INSERT ON CONFLICT DO NOTHING. Only the fingerprint and
caller-supplied expiry are stored. No raw assertion logging, new collection,
sharing, retention policy or external provider call is introduced.

The guard/registration moves to existing Identity Infrastructure. The technical
ConsumedTelegramAssertion record/configuration moves to existing Identity
PersistenceModel, not Domain. Their CLR namespaces and source bodies are
preserved. The model project does not reference central Infrastructure; the
central context already calls ApplyIdentityPersistenceModel. Thus moving the
mapping needs no context change or dependency cycle. Table/key/column/index,
migrations and snapshot remain unchanged. All hosts already compose
AddInfrastructure plus AddIdentityPersistence, preserving scoped lifetime and
the configured TimeProvider. Coordinated host rebuilds remain required.

The guard deliberately does not itself validate signature or reject an expired
input: that responsibility remains with its callers. The extracted original
test's successful insertion of an immediately expired record is retained, followed
by cleanup. No stronger replay/transaction promise is introduced by relocation.

## Verification intent

Move the isolated PostgreSQL case out of DietologistPersistenceIntegrationTests,
leaving its other three mixed methods and shared audit clock intact. Retain all
four original return-value assertions; additionally inspect persisted digest,
expiry and cleaned rows. Independent-context concurrent attempts must yield one
success and three replays. Additional real-provider tests cover expired/exact
boundary/unexpired rows and requested cancellation without consuming a token.
DI tests require scoped instances and exact adapter/model assembly owners.

Full solution build, module and consumer suites, architecture, full shared
PostgreSQL and HTTP suites, EF no-pending comparison and NuGet audit are recorded
separately under `.artifacts/identity-replay-evidence`. No coverage collector,
deployment, push or production access is part of this task.

## Wiki

Native start captured clean HEAD; bounded source research and design confirmed
existing owners and no new ADR-level decision. Exactly two frozen100 expected
paths follow the unchanged source move; queries, IDs, cohorts, thresholds, ranking
and historical measurements remain unchanged. The Users manifest's obsolete
central Authentication seam is updated to the actual module guard path. The
report and native evidence distinguish executed results from navigation hints.

The trace correctly identified TelegramVerify and its replay dependency. The
pre-edit ownership reader returned no owners despite explicit planned paths;
test-plan found the mixed PostgreSQL case but also suggested unrelated Ai/Billing
tests. Privacy returned Dashboard health-data candidates rather than this bounded
fingerprint lifecycle. Current source and actual provider tests are authoritative.
No generic Wiki generator, ranking rule or verification threshold was changed.

Three old/new production Git blobs are equal. Reversing exactly the two expected
path replacements reproduces the baseline corpus after Git line normalization.
The baseline native search measurement at e304b1148 is top1=96, top10=99 and
MRR=.9719. Its snapshot has one porcelain-modified Wiki reader copied by the native
wrapper with different line endings: both Git blobs equal HEAD and the normalized
diff is empty. This is a source-equivalent baseline, not a porcelain-clean claim;
the detailed normalization/status proof is retained separately. The current graph
and actual final Wiki outcome must still be checked independently.

The initial solution build rejected five string-comparison analyzer findings in
the new provider test. They were corrected with explicit ordinal comparisons;
the repeated complete build passed with zero warnings/errors. Original failed
logs are retained, and no rule was relaxed.
