# ADR 0001: Modular Monolith With Supporting Services

## Status
Accepted

## Context
FoodDiary needs fast product iteration, strong in-process consistency for the primary food diary domain, and enough separation for operationally distinct workloads such as email delivery, inbound email, background jobs, Telegram integration, and the Angular frontend.

Full microservices would add deployment, versioning, distributed tracing, retries, contract management, and data consistency overhead before the product needs that complexity.

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
Benefits:
- Simpler development and deployment for primary domain changes.
- Stronger consistency inside primary use cases.
- Clear layer boundaries without unnecessary network hops.
- Supporting services can evolve independently where the operational boundary is real.

Tradeoffs:
- Requires discipline to keep module boundaries from eroding.
- Some deployable units still share repository/release context.
- Future service extraction should be driven by runtime, ownership, scaling, or reliability pressure, not by folder count.
