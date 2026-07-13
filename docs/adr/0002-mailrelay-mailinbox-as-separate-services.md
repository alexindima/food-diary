# ADR 0002: MailRelay And MailInbox As Separate Services

- Status: Accepted
- Date: 2026-05-21
- Owners: Backend platform
- Related: ADR-0001
- Supersedes: None

## Context
Email delivery and inbound email have different operational characteristics from the primary FoodDiary API:
- separate databases,
- queue/listener behavior,
- retry and deduplication state,
- external transport/provider concerns,
- distinct health and observability needs.

## Decision Drivers

- Independent data and retry lifecycles for outbound and inbound email.
- Isolation of SMTP, queue, DNS, and MIME-processing concerns from the primary API.
- Explicit service-to-service contracts.

## Considered Options

1. Keep all email behavior inside the primary API and database. Simpler locally, but couples long-running transport workloads to product request handling.
2. Use one combined mail service. Reduces deployables but merges outbound queueing and inbound listener concerns with different failure modes.
3. Operate MailRelay and MailInbox as separate bounded contexts and services.

## Decision
Keep MailRelay and MailInbox as dedicated bounded contexts with their own layered projects:
- `*.Domain`,
- `*.Application`,
- `*.Infrastructure`,
- `*.Presentation`,
- `*.WebApi`,
- `*.Client`,
- `*.Initializer`.

Primary FoodDiary core may call these services only through typed client packages, currently via `FoodDiary.Integrations`.

## Consequences

### Positive

- The primary API does not own SMTP delivery/listener runtime complexity.
- Email state can use separate databases and operational lifecycle.
- Client packages provide explicit service-to-service contract surfaces.

### Negative

- Cross-service calls require stable DTOs and failure handling.
- Changes can span multiple projects and tests.
- Local development needs additional services when exercising full email flows.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/MailRelayArchitectureTests.cs`
- `tests/FoodDiary.ArchitectureTests/MailInboxArchitectureTests.cs`
- `tests/FoodDiary.ArchitectureTests/ClientPackageBoundaryTests.cs`

## Follow-up

- None.
