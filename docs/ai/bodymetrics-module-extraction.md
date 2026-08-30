# BodyMetrics extraction evidence

## Baseline and compatibility decision

Baseline: `037bbc254b` (`refactor: extract billing module`), the local master tip used by this worktree. No push is part of this task.

The extraction preserves `FoodDiary.Application.BodyMetrics` AssemblyName/RootNamespace, every application CLR namespace, all HTTP requests/responses/routes, calculations, date normalization, authorization, repository predicates, and EF entity identity. The two proven feature groups remain `WeightEntries` and `WaistEntries`.

`User` publicly exposes `IReadOnlyCollection<WeightEntry>` and `IReadOnlyCollection<WaistEntry>` and entry entities navigate back to `User`. Moving these types into a module Domain assembly would require a cyclic project reference or a CLR/EF navigation change. As in the accepted Hydration precedent, entry entities/IDs/invariants remain central. `WeightGoal` and `WaistGoal` are User-owned lifecycle children mutated by `User.Start*Goal`/`Cancel*Goal`, not BodyMetrics measurement aggregates; their IDs, statuses, value objects, EF mappings, and tests stay central. No empty Domain or Contracts layer is created.

## Ownership inventory

| Area | Owned implementation / retained seam |
| --- | --- |
| Application slices | Each feature owns Create, Update, Delete commands and GetEntries, GetLatestEntry, GetSummaries, GetHistoryPageSummary queries, validators, mappings, read service, and history-page model. Shared local helpers are RequiredIdParser and UtcDateNormalizer. |
| Application abstractions | 18 files across WeightEntries/WaistEntries: four repository ports, one read-service capability, one error factory, and three projection models per feature. Public namespaces are preserved. |
| Domain | Central `WeightEntry`, `WaistEntry`, `WeightEntryId`, `WaistEntryId` retain factories, finite/range checks, empty-user rejection, date normalization, comparison epsilon, audit behavior, and User navigations unchanged. |
| User-owned goals | Central WeightGoal/WaistGoal, IDs/status enums, DesiredWeightKg/DesiredWaistCm/ProfileWeightKg, User goal methods and Users commands remain unchanged. Shared numeric limits used by entries remain central. |
| Persistence model | Entry configurations move to module Infrastructure/Model and are registered explicitly from central FoodDiaryDbContext. Tables, columns, conversions, unique indexes, relationships, cascade behavior and central snapshot/migrations remain unchanged. |
| Repositories | WeightEntryRepository and WaistEntryRepository move to module Infrastructure/Persistence without query or mutation changes. Same scoped composite instance supplies narrow aliases. |
| Providers | No external provider adapter is owned by BodyMetrics. Users current-weight/current-waist providers stay central consumer-owned projections. |
| Presentation/hosts | WeightEntries/WaistEntries controllers and HTTP mappings remain Presentation-owned. API and Initializer invoke complete AddBodyMetricsModule. JobManager retains its existing application reference but has no new BodyMetrics scheduled behavior. |
| Focused tests | Four WeightEntries/WaistEntries application test files move to the module test project. Central entry/goal invariant tests remain with central Domain; mixed Users history-page, Dashboard, HTTP and shared persistence tests remain central. |

## Cross-module read seams

- Dashboard: `RepositoryDashboardBodyReadService` and snapshot composition consume the two read capabilities; optimized central `DashboardBodyReadService` reads entry DbSets as a consumer-owned batch projection.
- Statistics: `GetStatisticsSummaryQueryHandler` consumes both read services and summary models.
- TDEE: insight/calculator consumes weight-entry projections through `IWeightEntryReadService`.
- WeeklyCheckIn: read service/calculator consumes both projection capabilities.
- Users: `UserCurrentWeightProvider` and `UserCurrentWaistProvider` read scoped entry DbSets; goal lifecycle remains User-owned. BodyMetrics history-page queries compose Users projection contracts, not User aggregate mutations.
- Dietologist: `AttentionSignalMetricsReadService` retains its approved batch weight projection and existing access boundary.
- Export: source search found no direct BodyMetrics repository/read-service dependency in Export; diary/cycle export continues through Meals/Cycles contracts. No new health-data transfer is introduced.
- Cleanup: central `UserCleanupService` retains transactional user-scoped deletion and existing cascade safety. Gamification has no new direct BodyMetrics dependency introduced by this extraction.

## Privacy and semantic review

All repository reads retain their `UserId` predicates, including ID lookup; aggregate mutations still enter through current-user access checks. No logging, cache, queue, external transfer, retention, authorization, or payload changes were made. Numeric validation, inclusive date ranges, UTC/date-only treatment, tie ordering, limits, empty-bucket zero and banker rounding are preserved. Historical migrations and model snapshot are untouched; pending-model-change verification is required before delivery.

## Verification

Successful checks:

- Force-evaluate solution restore with repository-level `.artifacts/bodymetrics-solution` outputs.
- Full solution build: zero warnings and errors.
- EF `has-pending-model-changes`: no changes since the last migration (tool 10.0.10 warns that runtime is 10.0.11; model comparison succeeds).
- NuGet vulnerability audit: no vulnerable packages or audit problems; lockfile diff changes no resolved package versions.
- Scoped `dotnet format --verify-no-changes` and `git diff --check`.

| Test project / scope | Passed |
| --- | ---: |
| BodyMetrics Application | 97 |
| Central Domain | 1,078 |
| Central Application | 1,500 |
| Statistics / TDEE / WeeklyCheckIn / Export Application | 24 / 35 / 30 / 61 |
| Dietologist Application / Infrastructure | 249 / 11 |
| Focused central Infrastructure: DI, Dashboard, Users | 32 |
| Architecture | 745 |
| Presentation | 825 |
| Web API / JobManager unit tests | 247 / 168 |
| Web API integration excluding unrelated performance-baseline class | 175 |
| Focused PostgreSQL repositories, Dashboard body and Users cleanup | 7 |
| Total across successful selected suites | 5,284 |

All listed successful runs have zero failures and skips. HTTP integration covers full/focused/auth-admin Swagger snapshots, weight duplicate-date conflict, waist idempotent retry, and user flows. The seven persistence scenarios passed on retry after a Testcontainers startup timeout.

Broader diagnostic runs were not fully green: central Infrastructure exposed six pre-existing shared DI-fixture gaps for Billing/OpenFoodFacts/Wearables (the baseline fixture omits their module registrations) and a PDF deadline timeout, then was interrupted after prolonged inactivity. The unrestricted HTTP integration run passed 178 cases before the three-minute inactivity guard aborted the unrelated Meals performance-baseline test; the complete non-performance integration suite subsequently passed. These observations do not constitute a successful full-backend regression run.

Wiki update, verify, delivery validation/critique and commit-hook outcomes are reported in the final handoff. No push is authorized.
