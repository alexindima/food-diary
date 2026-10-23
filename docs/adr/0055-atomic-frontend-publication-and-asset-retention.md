# ADR 0055: Atomic frontend publication and asset retention

- Status: Accepted
- Date: 2026-10-22
- Owners: Frontend and deployment
- Related: ADR 0050
- Supersedes: None

## Context

Deleting a live static directory before copying the next Angular build creates a
partial-publication window. It also removes lazy chunks that an open tab has not
downloaded. The PWA deliberately loads most JavaScript on demand, and the admin
application also depends on lazy chunks. Updating HTML cannot repair missing old
assets. The SSR process can issue new asset URLs as soon as it restarts.

Nginx binds the host site directories into its container. Replacing those host
directories would leave an existing container attached to the old directory inode.
Service-worker manifests must retain the build's original hash table and matching
HTML; combining manifests would assert a version the build never produced.

## Decision

Keep `/var/www/fooddiary.club` and `/var/www/admin.fooddiary.club` as stable bind
roots. Publish each complete build to an immutable `releases/<id>` directory
inside its root. Validate the staged bytes and every service-worker hash entry
before atomically replacing the internal relative `current` symlink. Keep an
internal `previous` symlink for rollback. IDs are validated commit SHAs or SHA-256
digest identifiers; reusing an ID with different bytes fails.

Nginx serves static files from `current`. Retain Angular content-hashed JavaScript,
CSS and media under `retained-assets`, preserving their URL paths. Only the hashed
asset allowlist can fall back there; missing assets return 404. HTML, JSON,
service-worker manifests and unversioned workers never use this fallback. Build
files, including HTML and `ngsw.json`, are copied unchanged; no manifest is merged.

During migration, snapshot existing flat files into a digest-identified release
and preserve their hashed assets before switching. Mirror new hashed assets into
the stable flat root before SSR restarts. The existing Nginx container can then
serve both old flat HTML and new SSR asset URLs until it is recreated with the
`current` root configuration. Keep ACME challenges in the stable parent directory.

Publish static assets before starting the matching SSR image. Retain the existing
deployment lock and add a per-site publication lock for direct publisher use.
Reject path traversal, symlink/junction escapes, special source files, asset URL
collisions and incomplete copies. Failures before the pointer switch keep the old
`current`. Each site's pointer switch is atomic; deployment of both sites, SSR and
Nginx is a coordinated sequence rather than a cross-process transaction.

## Alternatives

- In-place copying still exposes partial releases and does not preserve old URLs.
- Replacing the bind root itself does not update an existing container mount.
- Retaining or merging old service-worker manifests can mismatch shell hashes and
  provides no supported content-version contract.
- A retention time limit needs an agreed browser/tab lifetime and an operational
  disk policy. It is a separate decision, not an implicit part of this repair.

## Consequences and enforcement

This change intentionally does not delete releases or retained/mirrored assets.
Disk usage grows until an explicit garbage-collection horizon and rollout policy
are agreed. Operators must monitor free space; failed staging directories also
need inspection. This is a known operational cost, not indefinite capacity.

Python temporary-directory tests cover consecutive releases, original manifest
bytes, old lazy URLs, copy/validation failure, rollback, migration and unsafe paths.
The Nginx fixture exercises the actual production public/admin server blocks,
retained chunks/fonts, non-retained document/worker behavior and stable ACME roots.
Current operation and compatible rollback are documented in
[`PUBLIC_RECIPES.md`](../frontend/PUBLIC_RECIPES.md#atomic-static-publication).
