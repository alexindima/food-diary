# ADR 0001: Modular Monolith With Supporting Services

- Status: Accepted
- Date: 2026-05-21
- Owners: Architecture
- Related: ADR-0002, ADR-0006
- Supersedes: None

## Context
FoodDiary needs fast product iteration, strong in-process consistency for the primary food diary domain, and enough separation for operationally distinct workloads such as email delivery, inbound email, background jobs, Telegram integration, and the Angular frontend.

Full microservices would add deployment, versioning, distributed tracing, retries, contract management, and data consistency overhead before the product needs that complexity.

## Decision Drivers

- Fast product iteration and strong in-process consistency in the primary domain.
- Independent operation for workloads with distinct runtime characteristics.
- Avoid distributed-system overhead without concrete scaling or ownership pressure.

## Considered Options

1. A single undifferentiated monolith. Operationally simple, but it couples unrelated runtime workloads and weakens business boundaries.
2. Full microservices. Physically isolated, but introduces premature distributed consistency and deployment costs.
3. A modular monolith for the primary domain with selected supporting services.

## Decision
Keep the primary FoodDiary backend as a modular monolith with explicit project/layer boundaries.

Run operationally distinct workloads as separate deployable units:
- MailRelay,
- MailInbox,
- JobManager,
- Telegram bot,
- Angular client.

Enforce boundaries with architecture tests and local `AGENTS.md` project guides.

## Consequences

### Positive

- Simpler development and deployment for primary domain changes.
- Stronger consistency inside primary use cases.
- Clear layer boundaries without unnecessary network hops.
- Supporting services can evolve independently where the operational boundary is real.

### Negative

- Requires discipline to keep module boundaries from eroding.
- Some deployable units still share repository/release context.
- Future service extraction should be driven by runtime, ownership, scaling, or reliability pressure, not by folder count.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs`
- `tests/FoodDiary.ArchitectureTests/LayeringTests.cs`
- `docs/ARCHITECTURE.md`

## Follow-up

- None. New extraction decisions require their own ADR.
