# ADR 0060: Single-host blue-green API and SSR deployment

- Status: Accepted
- Date: 2026-11-02
- Owners: Deployment and API hosts
- Related: [0055](0055-atomic-frontend-publication-and-asset-retention.md), [0058](0058-browser-sessions-in-the-existing-api.md)
- Supersedes: None

## Context

Production is one small Docker Compose host. The previous deployment stopped all
primary writers before every backup, then recreated API, SSR and ingress. Static
frontend publication already uses immutable releases and retained hashed assets.
Redis and a shared DataProtection keyring exist; SignalR now uses Redis backplane.
The host can temporarily accommodate a second API/SSR using its swap reserve.

## Decision

Use two API/SSR slots. Existing `api`/`client-ssr` are blue; `api-green` and
`client-ssr-green` are green. Each SSR calls its own API. Ingress routes to one
active slot using a generated configuration in its existing directory mount.
Regular switching tests configuration and reloads the running Nginx master.

The candidate Initializer's read-only migration inventory must agree with the
live database history. Unknown/ahead histories block deployment. For managed
releases with identical migration sets and API contract snapshots, keep the active slot serving while the
candidate warms, verify its exact SHA/tag and readiness, publish static assets,
warm matching SSR, then switch and probe the actual proxy. State is committed
only after successful checks and bounded retirement. Candidate failure before
switch leaves the active slot alone; a failed switch restores its routing.

An uninitialized deployment takes the maintenance path once, establishing the
backplane, Nginx draining/affinity and managed slot state together. Pending
migrations and explicit maintenance releases stop both API slots and primary
workers before verified backup and migration. Never restart old images after a
migration attempt automatically. Online expand/contract migration is outside this
decision. Routine releases still take an online, verified backup.

An interrupted first installation can roll forward after `api-ready` when no
completed state exists. Require the exact failed-plan API SHA/tag on the blue
service, unchanged routing/ports, identical migration inventory and the same API
contract. Preserve the failed plan in `recoveredFrom` and re-enter the ordinary
maintenance backup/start sequence. This never restarts pre-migration images;
other post-migration failures still require explicit recovery.

SignalR negotiation returns an HttpOnly, Secure, SameSite=None routing cookie for
both ordinary browsers and the embedded Telegram client. It grants no authentication. During
handover that cookie routes transport requests to their original process. After
a 30-second negotiation grace, stale cookies route to the active slot. Nginx
workers drain for up to 210 seconds; the controller waits up to 215 seconds and
then gives the retired application a 60-second stop deadline. Persistent sockets
may reconnect; Redis routes user events while both APIs run, without promising
replay. HTTP request budgets, including 190-second video imports, fit the drain.

Keep workers single, serialize initializers, and keep initializer work out of
candidate API startup. Check available RAM plus swap before overlap and before
each candidate service starts. A failed headroom check preserves active traffic.
This guard is not a peak-memory guarantee; measure the first production rollout.

## Alternatives and consequences

Kubernetes or a new permanent proxy adds operational cost on this host. Ordinary
container replacement retains the downtime. Two slots reuse existing services,
security restrictions, Redis and keys, at the cost of temporary memory and a
longer deployment while connections drain.

All images continue to be built, signed and verified by digest for the same
authorized source revision. Component build skipping is a separate change.
The API integration snapshot directory's Git tree identity is retained per
release. A changed/unknown fingerprint uses maintenance, including additive API
changes in this first implementation. Matching snapshots protect the frontend
publication/rollback window; snapshot equality cannot certify database, cache,
queue or behavior compatibility. HTTP compatibility remains a release requirement, including old
tabs/PWAs; maintenance alone cannot repair a breaking client contract.

## Enforcement

`scripts/deploy/test_blue_green.py` covers decisions, failures, state and recovery.
`test_blue_green_nginx.py` exercises the production public locations with real
Nginx, including an in-flight HTTP request, upgraded WebSocket, affinity, stale
cookies and SSR. Linux CI runs that fixture in the pinned production Nginx image.
The existing shell barrier, signed-receipt, metadata-denial and PWA fixtures stay
mandatory. See the [operational runbook](../backend/BLUE_GREEN_DEPLOYMENT.md).
