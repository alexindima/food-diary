# Retire feature error facades

## Scope and compatibility

Baseline: `026f2f32c6a55f52ee3f146e8021158530b6f566` on local master.

Retire the remaining 20 feature-specific central `Errors` nested classes across
16 owners: Admin, Ai, Billing, Cycles, Dietologist, Favorites, Images, Lessons,
MealPlanning, Meals, Products, RecipeCommunity, Recipes, Usda, Users and Wearables.
Eighteen existing owner factories remain unchanged. Billing and Lesson factory
members move unchanged into their existing owner Application/Abstractions projects.
No new project, error bucket, aggregate capability, provider operation or migration.

Central Application.Abstractions retains Authentication and Validation because its
parsers use them, plus CurrentUserAccessResolver and shared SSO contracts. Its
ProjectReferences shrink from 18 to exactly five: Domain.Primitives, Mediator,
Results, Users.Contracts and Users.Domain.Contracts. Consumers declare existing
used owners directly instead of receiving the old transitive exports.

Compatibility means a coordinated source rebuild of all in-repository consumers;
the retired nested C# types are not binary-forwarded. There are no external
repository consumers according to the user. Public HTTP codes/messages/kinds,
status mapping, nullable Details, parameter formatting, defaults, authorization,
logging, cancellation, provider transport, persistence and transaction behavior
must remain unchanged.

Notable existing cross-module contracts are deliberately preserved:

- Notifications MarkNotificationRead returns `Dietologist.InvitationNotFound` for
  missing and foreign notifications. Direct DietologistErrors use preserves this
  historical result; changing it is a separate HTTP behavior decision.
- Favorites AddFavoriteMeal returns `Meal.NotFound` for a missing source meal.
- Export without a cycle profile returns `Cycle.NotFound` with the empty GUID.
- Users' errors remain in Users.Contracts, which central resolution still needs.

## Verification design

Prepare the complete batch before one consolidated runtime campaign, as requested.
No initial red build or separate build per module. Ordinary hooks remain enabled.

Static evidence is retained under `.artifacts/feature-errors-evidence`:
source inventory and syntax spans; 110 production caller token comparisons allowing
only factory receiver/import changes; 18 unchanged factory sources and two exact
member-body moves; baseline reachability/emitted-use evidence for direct refs;
exact ProjectReference-only XML deltas and 28 standalone expected-reference guard
updates verified against baseline; syntax diagnostics for changed C# files.

Owner application suites contain literal assertions for all 75 factory members,
declaring assembly/namespace checks and invariant date/number culture cases.
The former central Dietologist/Meals factory assertions now belong to those owners
without comparing a factory to its facade. Four existing cross-module cases now
assert literal code/message/kind/Details. Common and presentation catalog tests
explicitly enumerate the 20 owner factories alongside shared taxonomy, retaining
their previous coverage instead of silently shrinking after nested-type deletion.

The source audit is not a claim of compiled or executed coverage. Historical PE
metadata belongs to the exact baseline; new direct factory calls legitimately add
owner references to consumer IL. Final builds/tests and metadata must confirm the
new combined graph. No coverage collector, push, deployment or production access.

## Runtime and source evidence

- Force-evaluate and locked restore passed. The independent central project builds.
- Full solution build passed with zero warnings/errors. The first full attempt
  reported two newly introduced style errors: Billing's extra directory segment
  conflicted with RootNamespace, and one guard used `var` instead of its required
  explicit type. Both were fixed before any tests ran; failed logs are retained.
- The final ledger contains 50 unfiltered suites, **6,286 passed, zero failed or
  skipped**, selecting the latest result per suite rather than summing reruns.
  It includes Architecture 1,107, full central PostgreSQL 93, Admin PostgreSQL 4,
  Cycles PostgreSQL 3, Users PostgreSQL 30, and full HTTP/Swagger 182.
- After the main campaign, DLL inspection exposed one missing direct owner
  reference for the new Admin factory tests. The reference/matrix were corrected;
  only Admin tests and the complete architecture suite were rebuilt/rerun. No
  production source or behavior changed in that correction. All other suites
  retain their completed evidence against unchanged production/test source.
- All 324 project DLLs were inspected. All 201 production assembly-reference sets
  match the exact factory/receiver changes; no direct error-owner edge is missing.
  The final change makes 186 references explicit in 96 consumers (97 project
  files including central); the central project loses 13 references and keeps five.
- EF reports no pending model changes. The existing tools/runtime warning
  (10.0.10 versus 10.0.11) was not suppressed or addressed by an upgrade.
- API compatibility audit reports zero breaking/additive changes and zero
  behavioral restrictions. HTTP snapshots are unchanged.
- Final NuGet audit covers all 324 projects, with zero vulnerable entries or
  reported problems. All 147 changed existing lockfiles retain resolved package
  versions. No package version change was requested.

Explicit SHA256 manifests retain the complete .NET source/project/build-input
set. After the final correction, later staging/commit checks compare all these
hashes to the verified manifest instead of rerunning unchanged test campaigns.
TRX files, exact commands, initial failures, source/compiled audits and manifests
remain in `.artifacts/feature-errors-evidence`; they are not coverage measurements.

## Wiki usefulness and limits

Native start/research/brief/test-plan/design and ownership/dependency/privacy/
topology/rollout queries were used. Research returned real Git precedents,
including `cca556d95d` (direct owner contracts), `4ca71ff9e1` (factory ownership)
and `f0c9f306e1` (owner-only facade retirement). Design was ready; deterministic
decision returned no ADR trigger. Source and compiled audits independently verify
the proposed ownership and compatibility, rather than treating navigation as proof.

The dependency query returned changeCount zero and did not describe the direct
ProjectReference changes; the exact XML/graph audit supplies that evidence.
Privacy/topology navigation includes adjacent module data beyond the receiver-only
diff. Neither inferred coverage nor a successful command establishes execution or
runtime correctness. No Wiki generator, ranking, threshold, corpus or policy was
changed to make this batch pass.

Final closure requires actual affected update/source reviews/verify and current
architecture-health, followed by strict delivery validation and critique. The
governed workspace is `.artifacts/llm-wiki/tasks/feature-errors`; sealed native
receipts are retained with the evidence before the ordinary hook-enabled commit.
Only the two migration/Designer criteria are N/A: no migration or snapshot changed.
No known-baseline exception, coverage collector, push or deployment is authorized.
