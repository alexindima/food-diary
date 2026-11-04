# ADR 0061: Retain full frontend releases for seven days

- Status: Accepted
- Date: 2026-11-04
- Owners: Frontend and deployment
- Related: [ADR 0055](0055-atomic-frontend-publication-and-asset-retention.md), [ADR 0060](0060-single-host-blue-green-deployment.md)
- Supersedes: None

## Context

Each frontend publication adds a full Angular build for the public site and
another for the admin site. Frequent deployments can consume several GiB in days.
Nginx serves the selected release through `current` and serves old hashed files
through `retained-assets`. Full release directories also support coordinated rollback.

## Decision

After public probes and blue-green handover succeed, prune full release directories
on both sites under the deployment and publication locks. Keep the targets of
`current` and `previous` regardless of age. Keep other releases for seven days
according to the release directory's modification time. The cleanup covers only
the full release directories. The `retained-assets` fallback and flat compatibility
files continue to serve assets for existing tabs and service workers.

Rename each expired release to a hidden cleanup directory before removing its
contents. A subsequent cleanup can finish an interrupted deletion. Report cleanup
errors as warnings after handover and try the other site's cleanup independently.

## Consequences

The amount of archived build data depends on deployment frequency within the
retention window. Rollbacks older than that window may require restoring the
matching build. Asset expiry needs a separate browser and service-worker policy.

## Enforcement

`scripts/deploy/test_publish_frontend.py` covers the age cutoff, protected release
pointers, retained assets, interrupted removal, and workflow ordering. The
[public recipes runbook](../frontend/PUBLIC_RECIPES.md#atomic-static-publication)
describes operation and rollback.
