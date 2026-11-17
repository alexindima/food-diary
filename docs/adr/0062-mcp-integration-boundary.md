# ADR 0062. MCP integration boundary

- Status: Accepted
- Date: 2026-11-17
- Related: ADR 0039, ADR 0041, ADR 0058

## Context

FoodDiary exposes recipe and shopping-list operations through MCP. As additional
features gain tools, both the application host and the domain owners need a
consistent boundary for registering tools and enforcing user permissions.

## Decision

The `McpIntegration.Presentation` module owns the MCP resource, protocol wiring,
tool-level scope checks, and idempotent write handling. Web.Api composes that
module with tools from Recipes and MealPlanning. The integration is deployed
with the main API.

Identity owns OAuth endpoints and validation of the delegated account session.
Its validator checks the principal, account security version, and active browser
session. Both token exchange and MCP authorization call this validator. The
existing tools continue to dispatch their owning application commands.

Preserve the live issuer and resource URLs, tool names and schemas, OAuth client
IDs and scopes, and retry semantics. Add new tools through their owning commands
and queries. Select permissions and replay behavior for each new operation.

## Verification and rollout

The Web.Api integration suite verifies the contract, independent account access
and durable writes. Architecture tests enforce the new module dependencies. The
change is shipped by rebuilding the existing API image with its module assemblies.
It carries no schema migration.
