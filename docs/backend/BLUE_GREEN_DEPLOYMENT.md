# API and SSR release handover

The implementation is governed by [ADR 0060](../adr/0060-single-host-blue-green-deployment.md).
The entrypoint remains `.github/workflows/deploy.yml`; run helpers under its
existing exclusive `/var/lock/fooddiary-deploy.lock`. Production lives at
`/opt/fooddiary`. Do not run a second deployment controller concurrently.

## Automatic decision

Preflight queries the pinned candidate Initializer with `list` and the live
PostgreSQL `__EFMigrationsHistory` in read-only mode. Exact matching ID sets allow
overlap only after the first managed deployment and with an unchanged API
contract fingerprint. Missing/pending IDs select
maintenance. Unknown database IDs, changing history, unavailable dependencies,
unexpected active slots or inconsistent state block deployment before mutation.

The first installation uses maintenance to replace the unverified legacy API
and establish the SignalR backplane and Nginx affinity/drain configuration. It
has the normal coordinated restart window once. Subsequent migration-free
releases use overlap. Set the manual Deploy workflow's `mode` to `maintenance`
for a reviewed breaking internal contract or Nginx image upgrade. Version public
breaking APIs for old clients; selecting maintenance does not update old tabs.

All images retain same-source signed-digest verification. CI computes the Git
tree identity of the complete API integration snapshot directory, including
`openapi-full-contract.json`, payload/error and focused behavior contracts. A
changed or unknown fingerprint selects maintenance even for additive changes.
This conservative first version avoids publishing a new frontend against an old
API with missing endpoints, and preserves rollback when both wire contracts agree.
Snapshot equality still requires semantic backwards-compatibility review; it is
not proof about authentication, cache or job behavior. Component builds are not skipped.

## Slots and publication

| Slot | API service / host port | SSR service / host port | SSR API origin |
| --- | --- | --- | --- |
| Blue | `api` / `5000` | `client-ssr` / `4000` | `http://api:5000` |
| Green | `api-green` / `5001` | `client-ssr-green` / `4001` | `http://api-green:5000` |

Slot ports bind IPv4 loopback. `API_PORT`, `API_GREEN_PORT` and `SSR_GREEN_PORT`
may customize the corresponding ports; actual Compose bindings are validated.
The bot must use the stable HTTPS ingress, never a slot's Docker hostname.
Both APIs share the existing Redis and DataProtection volume. Job workers keep
their single service instances and are replaced after candidate API readiness.
Preflight rejects drift between the explicit slot configurations, except for
validated bindings, profiles and matching SSR origins/dependencies.
Private host overrides must apply shared API environment and host aliases to
both `api` and `api-green`; API-only overrides deliberately fail this check.

`scripts/deploy/blue_green.py` warms and checks the candidate API's exact SHA/tag,
then releases the serialized auxiliary initializer barrier. Existing atomic
static publication runs before candidate SSR starts. SSR health and `/explore`
warmup must pass before Nginx can route requests to it.

The generated `nginx/sites-enabled/00-fooddiary-upstreams.conf` is atomically
replaced in the existing directory mount. Its checked-in blue default supports
local Compose bootstrap. Deployment deliberately never uploads that default over
the host's active routing. Before uploading vhosts, a locked bootstrap step
creates the blue route only if no route or managed state exists; failed initial
preflight therefore leaves restartable ingress configuration. A loopback-only Nginx listener on port `5090` verifies
the selected API version/readiness and SSR through the actual proxy. Public
`/internal/version` stays denied; do not expose the probe listener externally.

Nginx configuration is checked before reload. Handover preserves old SignalR
slot cookies for 30 seconds, then maps stale cookies to the current slot. Old
workers drain for 210 seconds; the controller's 215-second wait and subsequent
60-second application stop bound retirement. Existing upgraded sockets can
reconnect after their drain deadline. This is not durable message replay.

## Capacity and failure recovery

Overlap requires at least 256 MiB available RAM and 768 MiB available RAM plus
free swap before each candidate startup. These are admission thresholds, not a
guarantee about peak memory. API startup is serialized against initializers; SSR
startup follows API readiness and auxiliary completion. Measure swap, request
latency and peak RSS on the first authorized deployment before tuning thresholds.

Private `.deploy-plan.json` records the in-flight phase and owned candidate
services. `.deploy-state.json` records the last completed slot/SHA/tag/contract fingerprint. A failed
pre-switch candidate stops only its owned slot. A no-migration switch failure
restores the old route, probes it and drains candidate connections before cleanup.
Failed rollback keeps both slots for inspection instead of destroying recovery
options. An interrupted retirement is idempotently completed only after the
candidate still passes exact-version proxy checks.

Before a migration attempt, failed backup recovery restarts only previously
running primary writers. After `migration-started`, automatic recovery never
restarts old images. Inspect the database and pinned images, complete a forward
recovery or restore the verified backup with matching images. See
[migration safety](BACKEND_MIGRATION_SAFETY.md).

There is one narrow forward recovery for the first installation: if its saved
plan reached `api-ready`, no completed state exists, and that exact API SHA/tag
still serves from the blue service, a later release may restart maintenance.
The candidate must have identical applied migration IDs, the same API contract
fingerprint, identical ports and unchanged blue routing. The new private plan
retains the complete failed plan in `recoveredFrom`. Normal verified backup and
target-readiness checks still run; pre-migration images are never restarted.
Other partial maintenance releases retain explicit recovery requirements.

The final client runtime installs the shared libraries needed by its copied
Node binary and executes `node --version` during image construction. A missing
linker dependency therefore fails image publication before SSR is replaced.

The next planner recovers an uncommitted migration-free handover first. For a
stopped controller, inspect the private plan, current container identities,
readiness and loopback probe under the same deploy lock. `abort` restores a safe
pre-migration/no-migration release; `finish` can resume a probed retirement.
Never delete the plan or edit the active slot merely to bypass an inconsistency.
Command failures retain a private `.deploy-command-failure.log`; do not publish
its expanded environment or connection details.

## Verification

Run `python -B -m unittest discover -s scripts/deploy -p 'test_blue_green.py'` and
the existing deployment source/parallel/publication tests. Set
`FOODDIARY_NGINX_BINARY` to a local current Nginx binary for
`test_blue_green_nginx.py`. Linux CI runs the real fixture inside the pinned
production Nginx image and does not skip it. A local native Nginx pass is separate
from that Linux result; production rollout must wait for the complete CI gate.
