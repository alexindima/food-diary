# Authentication and image-storage provider ownership

## Boundary and preserved behavior

This coordinated source-rebuild batch moves twelve production files from
Integrations into the existing Identity and Images Infrastructure assemblies.
Google/Telegram validators and their two options classes belong to Identity.
S3 image/object adapters, the safe unconfigured fallback, object transport types
and S3 options belong to Images. All original CLR names and provider/logger names
remain unchanged; this is not binary forwarding for previously built consumers.

API and JobManager explicitly call AddIdentityProvider and AddImagesProvider beside
AddIntegrations. Initializer does not gain these configuration registrations.
Existing Identity persistence, JWT/password/SSO and Images persistence/outbox
composition remains separate. Registrations retain singletons, supply the default
TimeProvider only if absent, and preserve a host-provided clock.

Identity requires no Integrations reference. Images references Integrations one-way
for the single existing URI validator and telemetry meter, with internal friend
access. Integrations removes its Identity/Images ports and unused Users-ID reference,
and moves direct AWS, Skia, WebUtilities and OpenID/JWT package ownership to their
real adapters. No new shared production assembly or copied helper/meter is added.
Billing, mail-client bridges, common HTTP safety, shared JWT options/SSO storage,
Users credential state, DbContext, migrations and snapshots remain unchanged.

The source audit protects exact provider bodies and option chains:

- Google keeps HTTPS discovery, signing keys, accepted issuers, audience, lifetime,
  one-minute clock skew, verified email, issuer/subject checks and cancellation.
- Telegram keeps the distinct WebApp/widget signing constructions, constant-time
  comparison, parsing, five-minute future clock skew and configured TTL. Replay
  persistence and application replay handling are not moved or redesigned.
- S3 keeps private staging and explicit public destination buckets, 15-minute
  presigning, content-type/size limits, bounded reads, decoded image validation,
  exact validated-byte publication, delete routing, cancellation and telemetry.
  Custom endpoint/path style, region fallback, public-access opt-in and all option
  validation messages are retained. No credential or production call is needed.

### Separately approved S3 correction

The new offline registration test exposed an existing region-only defect before
the correction: assigning `ServiceURL = null` after `RegionEndpoint` cleared the
region in the current AWS SDK, and construction threw `AmazonClientException`
(`No RegionEndpoint or ServiceURL configured`). The original central and moved
registration token streams were identical at that point. Options explicitly
permit region-only configuration; this was not a relocation-induced failure.
The user explicitly approved fixing it in this batch. The sole production
behavior change assigns ServiceURL only when it is nonblank. It does not trim
or otherwise alter custom URLs, signing region, path style or public-access
policy. Offline tests cover null/whitespace URL, trimmed region, custom URL with
explicit region and the existing us-east-1 signing fallback. They also inspect
RegionEndpoint itself. All other registration and validation tokens are audited
against the original; the conditional assignment is an explicit audit exception.

## Tests, metadata and evaluation paths

Four complete provider test files move to the owner suites. Ten owner-specific
option test methods leave the mixed options catalog without duplication. Existing
Identity Infrastructure.Tests is reused; one Images Infrastructure.Tests project
imports the shared test settings. Offline owner/DI tests additionally check exact
assembly ownership, singleton lifetimes, clock preservation, option binding/errors,
safe storage fallback and AWS endpoint/region configuration.

Central host/cross-provider, authentication HTTP, shared outbox and PostgreSQL
tests retain their owners. Architecture guards protect physical sources, absence
of reverse owner exports, all six explicit provider registrations, and unchanged
Initializer configuration scope. The HTTP safety scanner follows the new roots.

Images' stale module manifest is corrected to actual module-root Application,
Contracts, Domain, Infrastructure and Model paths. Twelve exact expected-path
literals across eight Wiki corpora follow source-proven moves. Queries, thresholds,
accepted alternatives, ranking policy and historical measurements are not changed.
The parallel Wiki task owns only its generic IntegrationTests-suffix repair and
reports engine-only attribution separately in wiki-retrieval-followup.md.

## Wiki-first findings and verification protocol

Native start, research, brief, test-plan, decision, ownership, topology, privacy,
dependencies, rollout and design were used before implementation. Design returned
ready=true, but its wrapper exited on a locked temporary read-only SQLite snapshot
during cleanup. An earlier start graph refresh correctly rejected source drift
from parallel Wiki edits. Both actual errors are retained, not treated as clean
executions. Explicit graph phases require freezing all source/documentation writes.

Ownership returned the concurrent Wiki report instead of the planned provider
boundary; privacy largely returned Billing candidates despite explicit source
paths. Current code and test inventories therefore remain authoritative. Broad
frontend/job/deployment suggestions are not evidence of an actual behavior change.

Evidence is retained in .artifacts/auth-storage-evidence: native command logs,
whole-file moves, option-method inventory, exact fixture maps, source/registration
audits, build/test/EF/NuGet logs and final runtime summaries. No coverage collectors,
push, deployment or live provider calls are part of this task.

## Executed backend verification

Force-evaluate and locked restores succeeded. The final full solution build
succeeded with zero warnings/errors in 46.54 seconds. Sixteen latest unfiltered
suite results total **3776 passed, zero failed/skipped**, without counting reruns
twice; exact commands, TRX hashes and counts are in runtime-summary.json.
Highlights: Images Infrastructure 76; Architecture 1158; JobManager 168; complete
HTTP 182; complete central PostgreSQL 93 plus Identity 7 and Images 11.
The approved S3 correction was followed by a rebuild and nine affected suites,
including Images PostgreSQL and full HTTP. The central persistence 93 and Identity
runs predate that isolated registration correction; their persistence/provider
sources were unchanged. They are not represented as a second execution.

EF reported no pending model changes (existing tools 10.0.10/runtime 10.0.11 warning).
The full NuGet audit covered 326 projects, zero vulnerable entries/problems.
Twenty-nine changed existing locks retain all surviving resolved package versions.
Whitespace formatting passed with the workspace-loading warning retained.

Initial failures are retained: the general formatter repeatedly failed its
namespace code-fix provider and its verified owned process tree was stopped;
the first build reported 11 analyzer errors in new registration/tests, corrected
without weakening analyzers; the first runtime exposed the S3 bug, three exact
architecture metadata/catalog mismatches, and the missing explicit providers in
the JobManager test fixture. All affected suites subsequently passed. Provider
sources, four moved test files and ten option methods remain source-equivalent.

The parallel full MCP suite subsequently passed 266/266 without skips, bringing
the coordinated total to 4042. Its first run had 265 passes and one graph-drift
failure caused by a concurrent edit to this report. The existing configured-server
test internally refreshes the graph and checks a build even when its outer test
command uses --no-build. A complete frozen rerun passed; neither the earlier
failure nor the internal graph/build work is hidden by the outer command flags.

## Final Wiki evidence boundary

The parallel retrieval report records its independent same-database engine-only
comparison and full MCP tests. Final combined-source graph, official verify,
tail-stage and governed check outputs live in auth-storage-evidence; all 18 corpus
Node/MCP comparisons live in wiki-quality-followup-evidence/final. These later
outputs are deliberately separate from the pre-relocation engine comparison.
Runtime green does not waive a failing strict search cohort or quality threshold.
No benchmark query, threshold, weight, accepted alternative or history is changed
to obtain acceptance. Final indexed reports freeze before the shared graph pass;
subsequent results are recorded in evidence, not by concurrent source edits.
