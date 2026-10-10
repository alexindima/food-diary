# Review temporary dependency constraints

Keep reproducible lockfiles. This review concerns temporary overrides, exclusions
and exceptional pins, not routine removal of version constraints from manifests.
Some pins preserve compatibility or reproducibility and have no security expiry.

For inspection-only requests, report review evidence in the response and keep
graph experiments in disposable checkouts. Persist manifest or rationale changes
only within the authorized change scope.

For each candidate, establish its original purpose from maintained records,
Git history, upstream issues and real consumers. When maintaining an authorized
constraint record, include package and ecosystem,
override scope, version/range, affected consumers, advisory or compatibility
reason, source link, removal condition and date/evidence of the last review in
`docs/dependencies/overrides.md`. Do not invent reasons for existing constraints
or create empty records for unrelated packages. Keep the skill free of a copied
list of current pins; manifests, lockfiles and maintained rationale are authority.

Assess removal in an isolated checkout:

- Check current versions and dependency declarations of every parent. A patched
  child release does not help while a parent still requests an older exact child
  version. Updating one parent may leave another vulnerable installation.
- Update compatible parents when that is within the requested scope, remove only
  the candidate constraint, and resolve the graph without it. For npm inspect
  `npm explain`, `npm ls --all` and all matching lockfile entries, including nested
  copies. Check the root and scoped overrides, not just the top-level install.
- Account for a retained lockfile masking the constraint's purpose. Where needed,
  test a fresh resolution in a disposable copy and inspect the permitted upstream
  ranges. Do not deliver a full lockfile regeneration merely to perform this test.
- Check the resolved versions against current advisories, run the local audit,
  and run feasible focused security and consumer compatibility checks. For lint
  tooling, exercise actual matcher/message behavior and import-boundary rules.
  Select further required checks from current scoped guides and hooks.
- Remove the exception only when the resolved graph remains safe for the reported
  issue and the behavior it protected remains compatible. Minimize the delivered
  diff and update its rationale with the concrete replacement or removal evidence.
  Otherwise retain it and report the precise blocker and upstream change needed.

Use the review date as a reminder to reassess, never as an automatic expiry.
Distinguish "can remove after updating these parents" from "can remove now".
Report unrelated audit findings separately and retain permission boundaries for
major migrations, publication, settings changes and recurring automation.
