# ADR 0003: HTTP Models Live In Presentation Projects

## Status
Accepted

## Context
Earlier architecture had contract-style DTO concepts, but current backend boundaries separate application requests/models from HTTP transport models.

Application use cases should not know about ASP.NET transport details. HTTP route shape, query/body binding, current-user binding, error response format, and OpenAPI concerns belong to presentation.

## Decision
Keep HTTP request, query, response, and mapping types in presentation projects:
- `FoodDiary.Presentation.Api`,
- `FoodDiary.MailRelay.Presentation`,
- `FoodDiary.MailInbox.Presentation`.

Do not introduce or revive a separate HTTP contracts project for request DTOs.

## Consequences
Benefits:
- Application remains transport-independent.
- Presentation changes can adapt HTTP shape without changing use case contracts.
- OpenAPI/snapshot governance remains close to controllers and mappings.

Tradeoffs:
- Service-to-service clients still need explicit DTOs in their client packages.
- Some mapping code is required between HTTP models and application requests/models.
