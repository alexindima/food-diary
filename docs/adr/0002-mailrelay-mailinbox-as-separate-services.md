# ADR 0002: MailRelay And MailInbox As Separate Services

## Status
Accepted

## Context
Email delivery and inbound email have different operational characteristics from the primary FoodDiary API:
- separate databases,
- queue/listener behavior,
- retry and deduplication state,
- external transport/provider concerns,
- distinct health and observability needs.

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
Benefits:
- The primary API does not own SMTP delivery/listener runtime complexity.
- Email state can use separate databases and operational lifecycle.
- Client packages provide explicit service-to-service contract surfaces.

Tradeoffs:
- Cross-service calls require stable DTOs and failure handling.
- Changes can span multiple projects and tests.
- Local development needs additional services when exercising full email flows.
