# Identity authentication adapters and the Email outbox boundary

## Scope and ownership

Baseline: `a2c8291c27a4cf45f5bc4e05ea34e587a6e246a1`, clean master.
Move `JwtTokenGenerator` and `PasswordHasher` to the existing Identity Infrastructure
Authentication and Services folders, retaining exact C# source, public types and legacy namespaces.
Assembly ownership changes: coordinated host rebuilds are required, not binary
forwarding for old compiled clients. No new project, schema or HTTP contract.

Identity owns issuance/refresh validation and the technical password algorithm.
Users still owns credential operations, one-time-token checks and stored hashes;
it calls the unchanged IPasswordHasher port. Admin token consumers stay unchanged.
`AddIdentityAuthenticationInfrastructure` registers the same two singletons in
all three hosts, separately from unchanged `AddIdentityPersistence`. Central
JwtOptions/binding, API bearer checks, SSO/Redis/provider verification, combined
UserRepository, shared context/model/migrations and mail transport are unchanged.
Direct BCrypt.Net-Next/System.IdentityModel.Tokens.Jwt references move to the owner
at the same centrally managed versions, without a package upgrade.

PasswordHasher test bodies move unchanged, with the test namespace aligned to the
module. The mixed JWT file is split: generator cases/helper move unchanged apart
from the test namespace; seven JwtOptions theory methods remain centrally in
Authentication/JwtOptionsTests. No duplicated or removed cases. Typed tests cover
singleton lifetime, either DI order, assembly ownership and refresh round-trip,
including standalone authentication registration without persistence. Architecture
guards protect physical sources/packages and all three executable host calls.

## Email outbox decision

Identity EmailSender and DietologistEmailSender independently compose messages
and enqueue via IEmailOutbox. Central EmailOutbox stores the rendered EmailMessage;
EmailOutboxProcessor delegates claiming/retry to the generic engine and delivery
to IEmailTransport. Neither selects Identity templates nor depends on authentication
state. Existing payload scrubbing, idempotency key, retention/retry and mixed
four-stream replay remain unchanged. Multiple consumers alone are not ownership
proof; inspecting rules and dependencies identifies shared technical delivery,
not Identity business policy. A communications boundary requires its own design;
no extra shared/module assembly is introduced to conceal dependencies.

## Verification and Wiki

Final runtime verification: 4,107 passed, zero failed/skipped in 13 complete
unfiltered suites. Identity Application171/Infrastructure34, Admin52,
Dietologist249, Users156, central Infrastructure529/Application361,
Presentation825/API247/JobManager168, Architecture1034, PostgreSQL99 and HTTP182.
Forced/locked restore and final full solution build passed (32.68s, zero warnings
or errors). EF has no pending model changes; the pre-existing tools10.0.10 versus
runtime10.0.11 notice remains. NuGet process and parsed318-project audit passed,
zero problems/vulnerable entries. All321 existing lockfiles preserve retained
resolved versions. No collectors, deployment, real credentials or provider calls.

Initial compile attempts exposed namespace/collection-convention issues; test
namespaces and physical hasher folder were aligned without changing algorithms.
The first architecture run was1033pass/1fail because its exact central package
inventory still included the two moved references. After updating that inventory,
the full1034 rerun passed; failure evidence is retained. Final results use the
latest complete run per suite, not a sum of retries.

Evidence under `.artifacts/identity-auth-adapters-evidence` includes Git blob and
mixed-test split audit, TRX/process exits, package audit, final graph/Wiki results,
native acceptance/context/delivery receipts and normal commit-hook logs. Builds
use one isolated scope, cleaned separately from evidence and shared Wiki caches.

Clean-baseline frozen100 measured top1=96/top10=99/MRR=.9719 with the sole DomainGuard
miss at rank45. This is an actual quality failure, not a green Wiki result. Final
comparison and actual facade/gate exits are recorded separately in the evidence;
runtime success must not be used as proof of a full Wiki PASS. Only two source-proven
expectedPaths in context-search-validation move; queries, IDs, ranking, thresholds
and frozen100 stay unchanged.

Wiki start/research/design ran before source edits; design ready=True. Ownership
returned empty direct/downstream modules despite exact paths. Research classified
the combined extraction/outbox-review intent as Assessment (readyToImplement=false,
no blockers); explicit design and current-source verification establish the chosen
implementation boundary. Privacy found credentials but included broad health and
financial candidates. Decision reported no deterministic ADR trigger. No generator
or retrieval-policy tuning is part of this task.

Start also inferred background-job criteria from the JobManager host and Email
discussion. No job/configuration/retry/cancellation implementation changes; those
criteria are explicitly N/A, while direct consumer build/test remains applicable.
No migration/Designer/snapshot changes are needed. These scope decisions are
distinct from the Wiki quality result and never replace its verification.
