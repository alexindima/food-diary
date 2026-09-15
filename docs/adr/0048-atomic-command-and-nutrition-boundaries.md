# 0048: Explicit atomic commands and owner nutrition policy

Status: Accepted

## Context

Meals Create/Repeat staged an aggregate but immediately executed achievement outbox
SQL. The normal command pipeline saved afterwards, allowing outbox autocommit ahead
of the aggregate. Recipe overview also implemented the nutrition formula separately
from Recipes Application. Read composition relied on source tests to prohibit writes.

## Decision

- Introduce IAtomicCommand and IAtomicCommandExecutor in existing generic application
  contracts. Only top-level commands with retry-safe handlers opt in. The existing
  persistence coordinator executes the handler and save on the same transaction;
  the application port exposes no connection, tracker or transaction handle.
- Meals Create/Repeat opt in. Recognition creation keeps its existing explicit
  transaction and direct local handler invocation. Other commands retain their
  existing transaction policy. External side effects must remain outside retries.
- Flush post-commit actions after the atomic executor has committed. Failed Results,
  exceptions, cancellation and save failures roll back and discard queued actions.
- Keep a single scalar RecipeNutritionPolicy in the existing Recipes Domain project.
  Both application and SQL projection adapters use it for source precedence,
  scaling, rounding and fallback. No new assembly or project reference is needed.
- FD0018 rejects write/tracking/SQL capabilities in ReadModel.Composition, including
  method groups. FD0016 also reviews direct ADO capabilities in module adapters.
  This is engineering enforcement, not a security sandbox or database permission.
- Retain the 22 Shared production assemblies. Their separation protects consumers
  from persistence/provider implementation dependencies; the review established no
  safe unused-project removal. Small size alone is not a reason to merge contracts.

## Verification

PostgreSQL scenarios observe meal/outbox visibility from a separate context before
commit and verify success, failed Result, exception, cancellation and save-failure
outcomes. Pipeline tests verify commit-before-callback ordering and fail-closed
registration. Nutrition tests preserve source preference, fallback and rounding;
existing application and projection tests protect adapter compatibility. Analyzer
cases cover ordinary reads, write invocations and delegate acquisition.

No schema, migration or HTTP payload change. Hosts must rebuild together for the new
application execution port and its persistence registration. Existing broad technical
transaction contracts remain limited to reviewed adapters; the new use case does
not widen access to those contracts.
