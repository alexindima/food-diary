# ADR 0050: Public recipe read boundary and server rendering

- Status: Accepted
- Date: 2026-09-27
- Owners: Recipes and frontend
- Related: ADR 0008, ADR 0038, ADR 0039
- Supersedes: None

## Context

Recipe discovery and shared recipe links must work without an account, including
search previews. Existing authenticated recipe responses contain owner-specific data.
The production client previously required only static hosting.

## Decision

Expose separate anonymous public-recipe queries and HTTP response projections. Filter
visibility in the backend and return 404 for private IDs. Do not expose personal notes,
owner IDs, asset IDs or private ingredient source data. Reuse the composed recipe read
service without granting anonymous access to the authenticated API surface.

Serve `/explore` and `/explore/:id` through Angular SSR in a separate `client-ssr`
Compose service, using the same immutable client image. Keep marketing prerendering
and application client rendering. The SSR process accesses only the public recipe API;
requests do not forward user credentials. Recipe responses are not cached so a recipe
made private is unavailable on subsequent requests.

## Alternatives

- Client rendering alone preserves deployment simplicity but does not reliably provide
  populated recipe HTML to link-preview clients.
- Reusing authenticated recipe DTOs would couple anonymous exposure to future private
  fields and is rejected.
- Build-time recipe prerendering cannot reliably reflect publication changes.

## Consequences and enforcement

This introduces a small Node runtime and coordinated nginx/client deployment. No new
database or schema is needed. API visibility tests, the explicit public projection,
OpenAPI snapshots and SSR status tests protect the boundary. Deployment checks SSR
health after API readiness. See `docs/frontend/PUBLIC_RECIPES.md` for rollout/rollback.
