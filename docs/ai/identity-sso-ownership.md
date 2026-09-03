# Identity SSO protocol ownership

## Boundary and compatibility

Ordinary Admin SSO is an Identity authentication protocol. Its callers are
AdminSsoStartCommandHandler and AdminSsoExchangeCommandHandler, not Admin's
impersonation flow. `AdminSsoService` moves to
`Modules/Identity/Infrastructure/Authentication` with identical production source
and the legacy CLR namespace. `AddIdentityAuthenticationInfrastructure` owns the
same singleton `IAdminSsoService`; all three hosts already compose that entrypoint.
No project, reference, package, host source, schema or HTTP contract change is needed.
Deploy/rebuild hosts and assemblies together; this is not old-binary forwarding.

The service still generates 32 random bytes as a 43-character URL-safe code, stores
the user GUID with a two-minute TTL and validates the code before consumption.
Missing/invalid payloads still fail closed. Cancellation and expiry are unchanged.
The application retains user/admin checks and token issuance; none move into storage.

Shared `IAdminSsoCodeStore` and its central in-memory implementation remain shared.
Admin's handoff uses its own `imp_` code and `impersonation:` value prefixes over
that store. API's Redis adapter still uses its existing key prefix, TTL and atomic
StringGetDelete, selected by the existing host replacement. In-memory storage is
process-local; the move does not introduce distributed guarantees. Do not force the
shared mechanism into Identity or make Identity overwrite a host-selected store.

No Redis/provider access or security scan is implied by the local verification.
The Redis implementation is unchanged; host descriptor checks protect its selection.
Shared DbContext, model/migrations, combined UserRepository and credentials stay put.

## Test ownership

All six focused SSO test methods move to Identity Infrastructure.Tests unchanged.
Only their namespace and fixture composition change: the disposed provider resolves
the real internal store through public DI, without new InternalsVisibleTo/public API.
Additional protocol tests cover encoding, exact TTL/payload/cancellation forwarding,
malformed codes before store access, invalid/foreign payloads, 119/120/121-second
expiry boundaries, canceled consumption and host replacement in either order.
Existing singleton tests also verify SSO assembly ownership and composition order.
Mixed Admin/Identity code-isolation tests remain central and are executed there.
Architecture guards prevent reintroducing the central service/tests/registration.

## Executed runtime verification

Force-evaluate and locked restores passed. Final full solution build completed
with zero warnings/errors in 32.05 seconds. Thirteen unfiltered suites passed
4,128 cases with zero failures/skips: Identity Application171/Infrastructure59,
Admin52, Dietologist249, Users156, central Infrastructure523/Application361,
Presentation825, API247, JobManager168, Architecture1036, PostgreSQL99 and HTTP182.
The PostgreSQL suite took 4m32s; HTTP included existing authorization/Swagger cases.
No coverage collector or external Redis test was run. EF reports no model changes
(the existing tools10.0.10/runtime10.0.11 notice remains). NuGet audit covered all
318 solution projects with zero reported problems/vulnerable entries; all321
existing lockfiles retained resolved versions. No project file changed.

The initial build failed six analyzer diagnostics at three new test fixture sites
requiring asynchronous disposal. Those were fixed with await using, not suppressed;
the original log and successful unique retry log are retained. One early summary
attempt preceded the final audit receipt and was not treated as verification;
the final ledger requires successful process receipts and complete TRX counts.

## Wiki observations

Clean baseline is `2f717d9f8fde6523443e978482c7b518394e7d3a`.
Research found real Git precedents, including the immediately preceding Identity
adapter extraction, but ranked broad aggregate/AI/MealPlanning history above it.
Direct source inspection established the protocol/store distinction. Ownership
returned no modules despite explicit paths; privacy surfaced broad credential
fields rather than proving the SSO payload/TTL boundary. Decision reported no
deterministic ADR trigger; explicit design was ready. Existing boundaries need no
new ADR. No generator, evaluation target, ranking or threshold is changed here.

The clean baseline frozen100 result is top1=96/top10=99/MRR=.9719, with the sole
DomainGuard miss at rank45. Final verification and comparison receipts live under
`.artifacts/identity-sso-evidence`; an executed failed Wiki gate must not be called
a clean pass merely because runtime tests or governed delivery succeed.
