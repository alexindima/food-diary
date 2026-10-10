# Application patterns

These patterns complement the existing vertical slices, module ownership, narrow
contracts, command/query separation and reliable delivery protocols. Apply them
when a feature has meaningful business rules, several result variants, a durable
lifecycle or an external semantic boundary. Keep ordinary CRUD in its handler.

## Pure rules and side effects

Represent calculations and decisions as deterministic functions over supplied
values. A rule must not read a clock, database, configuration, user session or
provider itself. Supply the relevant snapshot or instant explicitly. Keep reads,
authorization, transactions, provider calls and persistence in the owning handler
or existing adapter. Do not add a service, interface and DI registration merely
to separate a small rule.

Examples:

- Recipes owns `RecipeNutritionPolicy`: scaling, source selection and rounding.
- Ai owns `FoodRecognitionPolicy`: valid provider quantities/probabilities,
  nutrition correspondence and whether a second paid stage is needed.
- Billing owns `BillingPremiumAccessPolicy`: access for a status, period end and
  supplied current instant.

## Types describe valid result variants

Prefer a closed set of variants when fields depend on a state. A ready result
requires its payload; a pending result cannot promise one. Use private validated
construction where a public constructor would allow inconsistent combinations.
Do not use a cast or null-forgiving operator as validation of external data.

Ai's internal `FoodRecognitionResult` exposes `Pending`, `Unusable` and `Ready`.
Only decoding a successful, error-free job with nonempty corresponding vision
and nutrition collections can produce `Ready`; its payload properties are
non-null. The existing cross-module and HTTP job DTOs stay compatible.
Frontend `RequestState<T>` uses a discriminated union for request variants.

Keep semantic identities, quantities and dates at their owning boundaries. A
new wrapper should encode an actual distinction or invariant. For value structs,
account for `default(T)` at consumers; a checked constructor alone cannot prevent
the default instance.

## Explicit lifecycle transitions

For durable jobs or multistep flows, define the allowed source state, action and
target state in one owner policy. Unknown states must not become success or a
fresh job. Terminal states reject worker actions. A state enum alone does not
define the transition protocol.

Ai's `FoodRecognitionLifecycle` owns the recognition transition table. The
PostgreSQL adapter enforces the expected source state atomically with the update
or row claim. A pure policy cannot replace a database fence. Keep provider calls
outside retryable transactions and retain the existing rule against automatically
replaying a paid recognition after an uncertain response.

Use a library or a process manager only when hierarchy, parallel stages or a
cross-transaction process justify it. Existing short database transactions and
outbox delivery remain the owning protocols.

## Decode external semantics once

Keep provider JSON, transport errors and SDK DTOs in the owning adapter. Decode
required fields, shapes and applicable numeric rules before returning application
values. Preserve absent values, zero, units, precision and source provenance.
Do not infer grams from millilitres or pieces, fill missing nutrition with zero,
or recalculate provider calories from macros without an owning rule.

`OpenAiFoodResponseDecoder` adapts provider output to Ai models and invokes the
pure recognition policy. `OpenAiFoodClient` retains HTTP, timeouts, fallback,
request construction and usage extraction. Invalid semantic output returns the
existing `Ai.InvalidResponse`; it does not initiate a new paid request. Generated
frontend SDK files continue to be regenerated rather than edited.

## Specify behavior with examples

Before changing a complex rule, record its rule, independently derived examples
and any unresolved question. Turn the applicable examples into focused tests at
the actual boundary. Use the existing xUnit/frontend/browser infrastructure;
a new BDD runner is not required.

The recognition matrix in [AI recognition jobs](../backend/AI_RECOGNITION_JOBS.md)
links lifecycle, quantity, partial-result and persistence examples to executable
tests. Unit tests verify pure decisions; mocked HTTP tests exercise the real
provider decoder; PostgreSQL tests verify admission, ownership and late-worker
fences. Preserve that separation of evidence.

## Choosing a pilot

For a feature change, identify its owner, mutable states, side effects and inbound
data first. Apply the relevant patterns together to that feature. Use existing
Recipes, Ai, Billing and frontend examples as navigation, then verify their
current contracts. Review compatibility and failure paths before expanding the
same change into another module.
