# ADR 0003: HTTP Models Live In Presentation Projects

- Status: Accepted
- Date: 2026-05-21
- Owners: Backend API
- Related: ADR-0001, ADR-0005
- Supersedes: None

## Context
Earlier architecture had contract-style DTO concepts, but current backend boundaries separate application requests/models from HTTP transport models.

Application use cases should not know about ASP.NET transport details. HTTP route shape, query/body binding, current-user binding, error response format, and OpenAPI concerns belong to presentation.

## Decision Drivers

- Application use cases must remain independent of HTTP and ASP.NET.
- Transport contracts and OpenAPI governance should remain close to controllers and mappings.
- Service client contracts have different consumers and versioning needs from public HTTP models.

## Considered Options

1. Put HTTP DTOs in Application. This reduces mapping but couples use cases to transport shape.
2. Create a shared HTTP contracts project. This makes DTOs reusable but risks coupling server internals and unrelated clients to one contract assembly.
3. Keep server HTTP models in Presentation and client DTOs in the relevant client packages.

## Decision
Keep HTTP request, query, response, and mapping types in presentation projects:
- `FoodDiary.Presentation.Api`,
- `FoodDiary.MailRelay.Presentation`,
- `FoodDiary.MailInbox.Presentation`.

Do not introduce or revive a separate HTTP contracts project for request DTOs.

## Consequences

### Positive

- Application remains transport-independent.
- Presentation changes can adapt HTTP shape without changing use case contracts.
- OpenAPI/snapshot governance remains close to controllers and mappings.

### Negative

- Service-to-service clients still need explicit DTOs in their client packages.
- Some mapping code is required between HTTP models and application requests/models.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/PresentationConventionsTests.cs`
- `tests/FoodDiary.ArchitectureTests/HostCompositionBoundaryTests.cs`

## Follow-up

- None.
