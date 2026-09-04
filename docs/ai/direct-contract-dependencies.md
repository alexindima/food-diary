# Direct owner-contract dependencies

## Boundary

This batch removes unused umbrella references from central Application.Abstractions.
It does not relocate C# types, change implementation bodies, introduce a new assembly,
or change namespace/assembly identity. Shared messaging, unit-of-work, current-user
resolution and the remaining delegating Errors facades stay central.

The exact baseline is `3eb1dcb4229a74e6c11376cc3a4bb60379415035` on master.
A clean isolated full solution build compiled all 324 projects with zero warnings/errors.
PE metadata was read without executing the compiled assemblies. Central's 32 direct
ProjectReferences included nine absent from its emitted assembly-reference set:

- Dashboard.Contracts, Products.Contracts and Recipes.Contracts;
- Users, Identity, Billing, Marketing and Notifications Application.Abstractions;
- Products.Domain.Contracts.

Direct references fall from 32 to 23. The other references support current source,
including error facades; their existence is not a reason to move contracts into Shared.
Metadata alone is insufficient for constant-only/erased compile dependencies, so an
independent trimmed central build and a full solution rebuild are required as well.

## Consumers

The baseline contains 129 emitted uses of these nine assemblies without a direct
owner ProjectReference, across 79 projects (35 production and 44 tests). All 129 are
made explicit, with the production/test matrix updated exactly. Some Domain.Contracts
dependencies were inherited through domain projects, not solely through central.
Explicit references reveal existing dependency rather than granting a new capability.
The first rebuilt solution exposed one additional compile-only dependency: Admin's
unchanged AdminBillingQueryFilters uses BillingProviderNames constants. Const inlining
explains their absence from PE references. A direct Billing.Domain reference preserves
that existing dependency. Export's unchanged sensitive-cycle validator likewise uses
inlined AuthenticationInputLimits constants and now references Identity ports directly.
The final total is 131 additions across 80 consumers (36 production and 44 tests).
For every one of the 131 additions, traversal of the exact baseline project graph
finds a preexisting transitive path to that target. The audit retains each path witness;
no new assembly becomes reachable by virtue of these direct-reference additions.
The initial compile failure is retained, not represented as a passing build.
Identity still uses semantic Users capabilities; this batch adds no foreign repository
access, implementation reference, aggregate mutation or change to the build policy.

Representative edges: Statistics/Cycles/Tdee/Gamification/WeeklyCheckIn to Dashboard
Contracts, Favorites/Meals to Products/Recipes read contracts, Identity/WeeklyGoals to
Notifications ports, and existing host/adapter consumers to Identity contracts. Products
identifiers/enums are referenced directly by the domain/model projects already using them.
The machine-readable inventory is `direct-contract-reference-inventory.json` alongside
this report. It enumerates every changed project and exact added/removed edge.

## Verification and delivery

The new 13-case architecture guard failed all 13 expected assertions on the original
project graph. Its first compilation attempt needed explicit ordinal string comparers;
that analyzer failure is retained separately and is not counted as the red test run.
The exact matrix covers all 131 additions, while focused guards prevent central
re-export regression and representative consumers losing their direct owner reference.

The first full architecture run found 20 additional exact-reference expectations in
module/host/resource guards (1065 passed, 20 failed). Every old expected set was checked
against the exact baseline ProjectReferences before applying only the audited reference
delta; Assert.Equal and package/ownership restrictions are retained. The 13 new cases
already passed in that run. No failed run is counted as final verification.

The rebuilt solution now passes with zero warnings/errors, and an independent central
build passes with 23 references. PE comparison of all 201 production assemblies shows
identical emitted dependency sets versus baseline; all 324 outputs were inspected and
no emitted reference to the nine selected targets lacks a direct owner reference. This
does not claim exhaustive discovery of other fully inlined compile-only uses. The two inlined-constant uses
are separately source/compile proven. Production, HTTP, EF and DI C# diffs are empty.
The EF pending-model check reports no change (existing tools/runtime version warning).
After updating the 20 exact expectations, full Architecture passes 1085/1085.

Final runtime ledger: 56 unfiltered suites, 6532 passed, zero failed/skipped. It uses
the latest actual TRX per project and includes all 33 module Application suites,
Architecture 1085, six full PostgreSQL suites totalling 141 cases (central 93,
Identity 7, Marketing 6, Products 3, RecentItems 2, Users 30), and full HTTP 182.
The main sequential wrapper retains exit 1 because it includes the first failed
architecture run; the separate rebuilt full architecture rerun is the final result.
Initial failures are preserved rather than removed or included in passing totals.

Full solution build: zero warnings/errors; independent central build: passed.
NuGet audit: exit 0, all 324 projects inspected, zero vulnerable entries. The 167
changed existing lockfiles preserve all retained resolved package versions. XML
comparison after removing ProjectReference nodes proves all other metadata in the
81 changed csproj files unchanged. Production emitted-reference parity is exact.

Wiki uses native update, explicit source-impact reviews, verify, architecture-health,
scope replan, acceptance/evidence, context assessment and strict delivery validation/
critique before commit. Actual process results and governed receipts are retained under
`.artifacts/direct-contracts-evidence`; generated navigation is not execution evidence.
No coverage collector, push, deployment or production access is part of this task.

## Wiki observations

Start/research/decision/test-plan/brief/design were used before metadata changes.
Design accepted the source/compiled evidence and bounded architecture decisions.
Cold ranked research reported zero relevant paths; exact project-path SQL impact also
returned zero candidate records despite scanning candidates. Direct source/project/PE
inspection supplied the consumer inventory; inferred navigation was not treated as
proof of absence. No generic Wiki repair or ranking/threshold change is included.
After graph refresh, ownership completed successfully and returned 20 direct and
20 transitively impacted modules, but zero downstream modules; emitted uses and the
baseline project-path witnesses remain the independent consumer evidence. The exact
inventory JSON was added to the native task scope after its first generation, with the
original scope retained; this is a documented scope amendment, not a pre-edit claim.

## Remaining work

This does not remove all transitive dependencies or every central facade. The remaining
23 central references and unrelated implicit owner edges require their own source-based
review. Shared DbContext, migrations, transactions and cross-module read orchestration
remain intentional. Whole-host rebuild is the verification/deployment unit; rollback
is a source rebuild, with no data migration or external consumer coordination required.
