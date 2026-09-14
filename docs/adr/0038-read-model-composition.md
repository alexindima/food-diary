# ADR 0038: Explicit read-model composition

- Status: Accepted
- Date: 2026-09-12
- Extends: ADR 0029, ADR 0031, ADR 0035

## Decision

The user approved removal of the remaining ten module Infrastructure references to
foreign Domain projects, including a separate composition layer for complex reads.

FoodDiary.ReadModel.Composition owns cross-module SQL projections that require
joins before filtering, aggregation or pagination. It implements narrow module
read ports and is registered explicitly by executable hosts. Modules never
reference this assembly. It may depend on the shared context, scalar contracts,
read-port declarations and explicit Domain owners needed to translate SQL.

This assembly is read-only: no tracked reads, aggregate-returning capabilities,
writes, SaveChanges, transactions, raw SQL or provider calls. Architecture tests
enforce its references and reviewed persistence capabilities. Moved query bodies
retain their SQL predicates, joins, ordering, limits and cancellation behavior.
Legacy implementation namespaces are retained for source-compatible test moves;
their physical ownership and assembly registration change explicitly.

Mixed repositories retain owner writes and delegate composed read methods through
separate read ports. Products supplies immutable batch snapshots for existing
Meals/Recipes hydration, preserving one batch query and transaction participation.
Unused Meals.Domain references in Export and MealPlanning are removed.

This is an explicit shared-database read boundary, not independent databases or
complete runtime isolation. Other reviewed foreign reads through the shared
context remain visible in the capability inventory. Do not move writes into the
composition assembly to hide ownership dependencies.

## Verification and rollout

Verify unchanged query behavior with PostgreSQL, owner/consumer tests, host DI,
HTTP/Swagger and the full architecture suite. Enforce zero direct foreign Domain
project references in module Infrastructure. No schema migration is intended.
Rebuild API, JobManager and Initializer with the new assembly and registration;
update Docker restore inputs. Revert moves, ports and registrations together.

## Dashboard body reads and Admin query ports

DashboardBodyReadService now lives in composition with its query body unchanged. Host registration replaces the fallback with one scoped concrete/interface instance; Dashboard Infrastructure neither installs nor removes that port. This removes Dashboard Infrastructure references to central Infrastructure and EF Core. Composition explicitly references Hydration Domain for SQL translation alongside BodyMetrics. Tests cover both registration orders, PostgreSQL tenant filtering, hydration boundaries, trends and no tracking.

Admin billing and role-audit report ports use Query names and immutable DTOs. Impersonation list handlers consume the existing composed query directly, while the owner repository retains writes and compatibility delegation. No HTTP or schema contract changes.
