---
name: fooddiary-api-change
description: Change FoodDiary API consumers or published feature capabilities using the generated SDK, owner semantic types and executable contract checks. Apply to endpoint integration and API boundary refactors.
---

# FoodDiary API changes

Find the owning adapter and the concrete generated operation. Keep native HTTP
values at the wire boundary, decode identities and dates once, and expose only
the owning capability to consumers. An already branded foreign identifier or
quantity must not become another meaning through a constructor.

For a task with declared paths, keep the change inside that boundary. A legacy
shared mapper can be outside it: decode in the in-scope owning adapter or report
the needed broader migration rather than changing extra consumers. Strengthening
one published capability does not require branding every internal field.

Use current scoped AGENTS.md and the actual SDK operation as authority. Generated
SDK files are regenerated, never manually repaired. Required fields belong in
the owning decoder; preserve existing cancellation, errors and idempotency.
Generated DTO fields may be optional even when the feature needs them. Check the
actual response type before constructing a semantic value; do not make a missing
field disappear through a cast or a simplified dependency stub.

For examples of a real decoder and an authorized backend handler, read
[references/examples.md](references/examples.md) when that layer is involved.
They demonstrate boundaries, not a mandatory implementation recipe.

The repository generator previews a new slice or adapter with
`node scripts/ai/generate-feature.mjs`; it leaves business authorization and
behavior for implementation. Existing slices usually need a direct bounded edit.

Verify the affected consumer behavior and compile negative type witnesses where
identity, quantity or date meaning changes. Use the Wiki test plan to select
checks. `npm run test:sdk` checks generated contracts and usage; selected browser
scenarios prove the stateful journey. For extracted evaluation snapshots, use the
provided acceptance contract and available checks rather than trying to recreate
the full application or reading a historical answer outside the snapshot.
When a snapshot provides `verify-contract.cjs`, run it against the real candidate
and dependency types. It is a focused check, not the full application build.

API routes and wire shapes also require the owning snapshot checks. Use
`fooddiary-time-change` or `fooddiary-nutrition-change` for actual time/nutrition
behavior changes. This skill does not authorize release, external messages, or
provider calls.
