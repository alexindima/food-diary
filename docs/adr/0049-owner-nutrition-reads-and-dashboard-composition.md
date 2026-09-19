# 0049: Owner nutrition reads and dashboard composition

Status: Proposed

## Context

Statistics and WeeklyCheckIn requested general nutrition buckets through Dashboard,
although Meals already owned the aggregation and bucket model. Dashboard.Infrastructure
contained only a DTO adapter and registration. Ai quota persistence records accepted
an application request, introducing an unnecessary upward project dependency.

## Decision

- Meals.Contracts exposes ReadMealNutritionStatisticsQuery; Meals Application delegates
  to its existing nutrition read capability. Statistics and WeeklyCheckIn dispatch the
  owner query and use the existing Meals bucket. Callers retain authorization, UTC/date
  normalization, quantization and period limits. No HTTP or database contract changes.
- Dashboard retains its snapshot DTO shape and reused internal adapter, registered by
  AddDashboardModule with scoped concrete/interface identity. Remove the standalone
  Dashboard.Infrastructure project and AddDashboardReadServices composition step.
  SQL body/meal readers remain host-composed; modules never reference that implementation.
- Move existing cross-module Dashboard projection tests to Platform Infrastructure.Tests
  and remove their obsolete module Infrastructure test assembly. Preserve all scenarios.
- Ai quota records accept scalar values; Infrastructure translates the reservation
  request. Preserve independent transactions, locks, reconciliation and retry behavior.
- Source boundary checks resolve actual production .csproj directories and fail on
  missing required roots. Module/Shared relocation cannot silently erase scan coverage.

## Consequences and verification

Deploy/revert backend assemblies together because internal request assembly ownership
changes. Keep external response DTOs, schema, migration history and FK behavior unchanged.
Shared runtime and the remaining small contract assemblies retain their existing roles.

Validate the full project graph and architecture suite, Statistics/WeeklyCheckIn/Dashboard
application behavior, host DI, cross-module projections, API contracts and PostgreSQL
quota/projection behavior. Regenerate lockfiles, module inventory and Wiki context.
Missing Docker execution must be disclosed; it is not replaced by mock-based coverage.

This supersedes Dashboard statistics routing/registration in ADR 0041 and its extraction
guides; the broader owner-request and transaction rules remain in force.
