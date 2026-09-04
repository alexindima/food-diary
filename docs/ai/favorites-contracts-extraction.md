# Favorites contract ownership

## Boundary

All 26 source files formerly in central FavoriteMeals/FavoriteProducts/FavoriteRecipes
move without signature, namespace or executable-body changes:

| Owner | Files | Responsibility |
| --- | ---: | --- |
| Favorites Application/Abstractions | 20 | Twelve repository ports, three persistence projections, three Errors classes, consumed source-meal port and model |
| Favorites Contracts | 6 | Three semantic read services and three consumer projection models |

Favorites application and infrastructure depend explicitly on their owner ports.
Products, Recipes and Presentation explicitly consume Favorites Contracts. Meals
references both: it implements IFavoriteMealSourceReadService and uses favorite
projections; Favorites implements Meals' IMealFavoriteReadService. The application
implementations do not reference each other. Central Application.Abstractions
retains only Errors.Favorite* delegating facades and an owner-port reference, not
its former direct Favorites Domain reference.

Existing FavoriteMealId and MealId require the current Favorites/Meals Domain
assemblies. This is a documented residual ID seam, not permission to return or
mutate aggregates through public read contracts. Repository interfaces stay private
to their owning application boundary. A later ID extraction is independent.

## Compatibility and privacy

The old namespaces, signatures, nullability, cancellation defaults, two executable
GetOwnedByIdAsync forwarding methods, error codes and model fields are preserved.
Application assembly identity remains FoodDiary.Application.Favorites. Moved types
have new declaring assemblies: rebuild/deploy hosts and consumers together; old
precompiled binary compatibility is not promised. No external consumers exist per
the repository owner's confirmation. No package version upgrade is intended.

UserId predicates, private favorite relationships, source-meal access, ordering,
nutrition/unit projection, persistence/transaction ownership and DI lifetimes do
not change. Repository bodies, mappings, context, migrations/snapshot and HTTP
implementation are unchanged. No new collection/logging/export/provider behavior.
Rollback is a coordinated code rebuild, not a database rollback.

## Verification

Final runtime verification: 12 complete unfiltered suites, **3,693 passed, zero
failed/skipped**, without historical rerun double counting:

| Suite | Passed |
| --- | ---: |
| Favorites Application / Domain | 84 / 25 |
| Meals / Products / Recipes Application | 161 / 128 / 63 |
| Central Application / Infrastructure unit | 361 / 550 |
| Architecture | 1,053 |
| Presentation / JobManager | 825 / 168 |
| Full PostgreSQL Infrastructure integration | 93 |
| Full HTTP/Swagger integration | 182 |

Force-evaluate and locked restore passed. Full solution build passed with zero
warnings/errors; the subsequent exact architecture lists and all consumer suites
were compiled and tested. EF reports no pending model change (existing tools
10.0.10/runtime 10.0.11 warning only). Whole-solution transitive NuGet audit passed
with no vulnerable packages; 148 changed existing locks retain package versions.

The first full build stopped on two IDE0008 errors in the new test counters;
explicit int fixed both. The first architecture run had 1,048 passed / 5 failed:
four local reference lists and the stale generated repository catalog. Exact lists
and native Wiki update fixed these; the complete rerun passed 1,053. Neither failure
was labelled a baseline failure or hidden by a filter/suppression.

Durable process logs, final and initial TRX, and source-equivalence audit are
retained under `.artifacts/favorites-contracts-evidence`. The audit requires
all 26 relocated C# files to match the exact starting commit after line-ending
normalization and rejects protected runtime/model/HTTP changes. New module tests
exercise default forwarding with explicit and omitted tracking/cancellation
arguments; architecture guards enforce physical ownership and no aggregate/repository
types in public read contracts. No coverage collector is used. Heavy outputs are
kept in one repository-level scope and cleaned with native dotnet clean; evidence
and shared Wiki caches are retained. Final Wiki/governance receipts are recorded
separately from runtime evidence; no broader retrieval-quality pass is implied.

## Wiki observations

Start/research/design completed; design ready=True. Before edits, native ownership
returned empty direct/downstream owners despite planned module paths; this clean-diff
result was not used as proof of isolation. Research's
Git precedents were real but dominated by broad lockfile/Users matches; current
Products/Recipes contract projects supplied the more relevant verified precedent.
Initial test-plan omitted the module-owned Favorites suites and suggested unrelated
AI/Product tests; source consumer inspection supplies the actual test plan.
Initial privacy discovery did not establish Favorites user scoping, which required
direct contract/repository review. The old Favorites manifest still advertised
central paths; this change updates supported owner/project mappings, without
patching generic generators, retrieval ranking or policy thresholds.

After the real diff, ownership identified Favorites/Meals/Products/Recipes plus
broader AI/lockfile and downstream context. The generated page now finds the six
physical source areas; it still merges owner-only ports with consumer-facing
contracts under one public-surface label and misses the literal Favorites HTTP
surface/business consumers. These are navigation limits, not isolation evidence.
Dependencies reported no package changes, consistent with the retained resolved
versions; the project-reference delta is verified separately by the exact matrix.

The manifest inventory now ignores source-less directories (which Git does not
track), with a temporary-directory regression proving nested C# areas remain
included. Old empty central Favorites directories may remain locally because the
cleanup command was rejected by tool policy; no original source file remains there.

No new ADR: this applies the established owner-port versus consumer-contract split;
it does not introduce a new boundary policy. Current guides and architecture tests
record the exact residual ID and compatibility seams.
