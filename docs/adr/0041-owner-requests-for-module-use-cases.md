# ADR 0041: Owner requests for cross-module use cases

- Status: Accepted
- Date: 2026-09-15
- Related: ADR 0030, ADR 0035

## Context

Public administration and composition services let consumers invoke owner business operations through interfaces. Several services were only dispatch boundaries around a single use case. The user chose a common request/handler approach for Admin, Ai, Billing and BodyMetrics, including the owner operations they consume, and asked to apply it incrementally to subsequent modules.

## Decision

Place cross-module business requests and their response contracts in the supplying module's Contracts project. Consumers depend on that project and dispatch through ISender. Put operation-specific behavior in an Application handler in the owner module. Do not introduce a service plus a forwarding handler for a single use case, or inject a foreign handler directly.

Keep technical outbound ports, provider adapters, repositories, token issuance, mail transport and narrow reusable profile/access capabilities where their ownership requires them. The rule governs business use-case entrypoints, not every interface. New module requests must not introduce a dependency cycle; Billing's premium conversion request belongs to Marketing.Contracts.

Preserve transaction ownership explicitly. Migrated nested operations that stage writes use IRequest<T> without ITransactionalCommand. Their existing outer command owns commit, rollback and post-commit actions. Independently committed workflows keep their explicit boundaries. Do not disable the transaction pipeline globally, and do not assume that naming a request Command makes it an independently committed transaction.

Authorization, validation, user scoping, cancellation, errors and idempotency remain explicit responsibilities. Mediator dispatch alone enforces none of them. Owner normalization belongs with the operation; endpoint authorization and response shaping remain with their entrypoints. Telemetry recognizes requests in owner Contracts assemblies without logging payloads.

## Consequences

The owner exposes a discoverable use-case API and can run it through the common pipeline. The cost is one request and handler slice per operation, plus explicit handler registration in each executable composition root. Reusable algorithms may remain internal helpers.

Changing an interface call to a request is a source/binary boundary change. Rebuild and deploy hosts and their module assemblies together. HTTP payloads and database schema are unchanged by this migration. Rollback restores the complete prior application artifact; it does not need a data migration.

## Enforcement and follow-up

CrossModuleRequestBoundaryTests rejects exported service interfaces in Admin, Ai, Billing and BodyMetrics Contracts and verifies the migrated owner request/handler pairs. Dependency-matrix and aggregate-boundary tests protect the graph. OwnerRequestTransactionTests and the PostgreSQL shared-context suites protect caller-owned commits and rollback. ModuleTelemetryBehaviorTests covers both legacy Application and canonical Application/Contracts names.

The first batch migrates 43 operations; see [the inventory](../ai/cross-module-mediator-refactor.md). Review remaining modules individually before applying the same rule, retaining justified technical ports.
