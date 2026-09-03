# Retire unused central ID converters

## Decision and evidence

Base: `b5cc09818082c84d8e5e03ecc9c9439d0fba2dc0`, clean master.
The user explicitly confirmed there are no consumers outside this repository.
Removing this unused public API is intentional; this is not a binary compatibility
promise for previously compiled consumers.

`StronglyTypedIdConverters` contains thirteen nested converters: UserId,
FastingPlanId, FastingOccurrenceId, FastingCheckInId, WebPushSubscriptionId,
ProductId, MealId, RecipeId, MealItemId, MealAiSessionId, MealAiItemId,
RecipeIngredientId and RecipeStepId. Searches of source, project/build files,
migrations, reflection discovery and EF conventions found no production consumer.
The sole caller is `StronglyTypedIdConvertersTests`, an isolated helper test.
Effective Infrastructure `IsPackable` and `GeneratePackageOnBuild` are both false.

Current Users, Fasting, Notifications, Products, Meals and Recipes EF mappings
already specify their own inline `HasConversion` expressions. Source-verified Git
precedents include Fasting extraction `ea16933690500f00a8a920ea398ef90a03d27835`
and Notifications integration `3851a98e0340fd12191c56cf41f261a678a632c8`.
The latter originally retained the helper only for public compatibility; this
follow-up resolves that caveat rather than changing notification persistence.

Delete the container and its sole helper test; do not relocate dead code into
modules or introduce another shared EF assembly. Module mappings, shared context,
migrations/snapshot, project references, DI and runtime conversion behavior stay
unchanged.

## Replacement checks

`StronglyTypedIdModelTests` constructs the real composed Npgsql model without
opening a connection. Thirteen cases inspect every mapped property of each ID
type, including foreign keys and nullable properties. They require a primary-key
mapping, UUID store/provider types, exact empty/non-empty Guid round trips and null
preservation. Typed factories construct expected values without reflection.
This cross-module model-composition coverage belongs in the central infrastructure
test project, not in a module that owns only part of the model.

The focused tests are run before deleting the helper to establish that the real
model already supplies these conversions. Full central unit, architecture and
PostgreSQL suites plus EF model comparison follow the deletion. No coverage
collector, production access, provider API call, push or deployment is requested.

Actual verification after removal:

- Infrastructure unit tests: 550 passed.
- Full ArchitectureTests: 1047 passed.
- Full unfiltered PostgreSQL Infrastructure.IntegrationTests: 93 passed (5m05s).
- Combined final cases: 1690 passed, zero failed/skipped. The thirteen successful
  pre-deletion cases are a separate baseline run, not added to that total.
- Locked restores and all corresponding project builds passed; Web.Api startup
  project build passed with zero warnings/errors.
- EF `has-pending-model-changes`: no changes since the last migration. Existing
  tools 10.0.10/runtime 10.0.11 warning retained; no version upgrade performed.
- Source audit: the only production delta is the deleted container; module
  mappings, context, migrations, project/build files and DI are unchanged.
- Own isolated build outputs cleaned successfully; evidence and Wiki caches kept.

Durable TRX hashes, actual commands/exits, source audit, Wiki verification and
governed delivery receipts are retained under `.artifacts/id-converters-evidence`.
The first new-test build reported two style diagnostics (trailing comma and
collection expression); both were corrected before the successful baseline run.
That failed build is retained separately, not counted as test execution.

## Wiki observations

Research located the helper/test and relevant Git precedents, which were checked
against current source. The adaptive route selected architectural/governed work
because this removes a public infrastructure API; the design records the user's
explicit no-external-consumer decision. Generic migration criteria do not imply
a migration is necessary: unchanged mappings and an actual EF comparison decide
that. Compiled navigation is not proof that the helper is used.

An initial planning invocation incorrectly expanded a PowerShell array through a
native `pwsh -File` boundary. Calling the facade in the same PowerShell process
fixed it; this was an invocation error, not a Wiki generator defect. No Wiki
engine, policy, ranking, threshold or evaluation corpus is changed here.
