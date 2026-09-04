# Users, Identity and residual application contract ownership

## Scope and compatibility

This tranche assigns 117 existing C# files to their physical module owners:

| Owner | Files | Boundary |
| --- | ---: | --- |
| Users Contracts | 59 | Semantic capabilities, projection models, status filter, errors |
| Users Application/Abstractions | 7 | Aggregate/repository ports and role catalog |
| Identity Application/Abstractions | 45 | Authentication and email-template ports/models/helpers |
| Admin Application/Abstractions | 3 | Administrative role-audit read ports/model |
| Dietologist Application | 2 | ID and enum validation helpers |
| Billing Application/Abstractions | 1 | Billing consumer-owned marketing conversion recorder |

Moved bodies, CLR namespaces, public signatures, default interface forwarding,
nullability and error/security behavior are unchanged. Source equivalence is
checked against the exact Git baseline and recorded in
`.artifacts/contracts-batch-evidence`. All consumers/hosts rebuild together;
the owner confirmed no external consumers. Old precompiled binary compatibility
is not promised. No HTTP, EF, migration, provider or DI behavior is redesigned.

Three new projects establish one-way ownership. Users Contracts never references
Identity or central Application.Abstractions; Users owner ports depend on its
Contracts and Domain; Identity ports depend on Users Contracts and Identity
Domain. Current UserCalorieSchedule/UserPreferenceUpdate signatures require a
Users Domain reference in public Contracts. Guards prohibit aggregate/repository
exposure; separating those value types is not hidden inside this change.

Central Application.Abstractions retains facade references for existing consumers.
Owner applications/adapters and Marketing's Billing consumer-port implementation
receive explicit references. This tranche establishes declaring-assembly ownership,
not a claim that all transitive central-contract dependencies are eliminated.

## Deliberately shared

CurrentUserAccessResolver stays central because it delegates UserIdParser and the
shared authentication-error policy. IAdminSsoCodeStore stays central for both SSO
protocols, in its unchanged Authentication/Abstractions folder and declaring
assembly. Central feature-folder guards accept either Common or Abstractions
as a purposeful contract folder and ignore source-less local directories;
all source-file placement restrictions remain enforced.
Shared rendered-email transport/outbox, audit, DbContext, migrations and storage
implementations retain their owners. Credential state remains Users Domain.

Billing owns the conversion-recorder consumer port; Marketing implements it.
Admin owns its role-audit projection, not the Users role-audit aggregate.
Identity owns email-template contracts despite their legacy Admin namespace.

## Verification and Wiki evidence

Use isolated output `.artifacts/contracts-batch`, ordinary tests without coverage
collectors, independent new-project builds and a complete solution build.
Exercise Users/Identity/Admin/Billing/Marketing/Dietologist and cross-module host
consumers, including real PostgreSQL compatibility tests. Keep failed initial
attempts separate from final results; Wiki navigation hits are not executed tests.
Wiki start/research/design/decision/brief/test-plan were run before production
changes. Design explicitly records the dependency graph and compatibility scope.
Final measured results and any unresolved gates are recorded below after execution.

Initial compilation rejected an unnecessary folder-only move of the shared SSO
store interface; it was restored to its exact original path/body/assembly.
The 117 module moves remain source-equivalent. Dietologist's two existing public
helper namespaces need an exact two-file IDE0130 exception, following the existing
physical-relocation policy; no other namespace or diagnostic severity is changed.
New test-only style findings are fixed without changing production assertions.

Two Wiki fixtures referenced moved AccountCreatedMessage/ICurrentUserAccessService
files. Their three literal path occurrences are remapped from proven Git moves.
The receipt fixture now records the actual Ai consumer test command returned by
the bounded current plan, not the old central application command. Its receipt
reuse/composition/lineage assertions remain unchanged; no ranking or thresholds
are changed. The current default plan lists alphabetically selected module
consumers but omits the Users owner suite; source-derived verification therefore
explicitly includes Users and the other module application consumers.

## Final runtime verification

- All three new projects build independently; force-evaluate and locked solution
  restores pass. Complete solution builds pass with zero warnings/errors.
- Final per-suite TRX ledger: **44 unfiltered suites, 6,055 passed, zero failed or
  skipped**, including all 33 module Application suites. Repeated attempts are not
  counted twice. Architecture: 1,072; central Application: 386; Presentation: 825;
  Web API unit: 247; JobManager: 168; central Infrastructure unit: 550;
  Identity Infrastructure unit: 59. Real PostgreSQL: Users 30, Identity 7,
  central Infrastructure 93. Full HTTP integration: 182.
- EF pending-model check exits zero with no model changes. The existing tools
  10.0.10/runtime 10.0.11 warning is retained; no tooling/package upgrade.
- Whole-solution transitive NuGet audit exits zero with no vulnerable entries.
  147 changed existing lockfiles retain all still-present resolved package versions.
- All 117 moved source bodies compare equal against the exact starting commit
  after line-ending normalization. No runtime implementation, HTTP snapshot,
  persistence mapping or migration source changes.
- Initial build/style failures and the first architecture run (1,065 passed,
  seven stale path/catalog failures) remain in evidence. Expectations/catalog were
  corrected to actual owners; the complete 1,072-case rerun passed.
- Wiki receipt regression passes; SQL/JSON brief parity passes all 14 cases.
  An intermediate compiled projection became stale after update; a native graph
  rebuild restored it. Neither cached pass fabrication nor threshold weakening
  was used. Final affected Wiki/governance results are retained separately.

No coverage collector, deployment, production call or external security scan was
performed. These results prove source relocation and rebuilt-consumer behavior,
not elimination of every remaining central compatibility reference.
