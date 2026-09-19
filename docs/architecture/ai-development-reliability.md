# AI development reliability

Module boundaries constrain changes, but do not prove runtime correctness. Prefer
compiler-enforced local rules and executable boundary tests to additional layers.

## Failure handling

FD0019 rejects discarded FoodDiary `Result`/`Result<T>` expressions, including
awaited results, unawaited Task/ValueTask results and explicit discard assignments,
in module Application source. Handle or propagate failures. A deliberate best-effort
exception requires a local suppression with an explanation; do not disable the rule
for the entire module. This rule is not a proof that an assigned result is handled
correctly on every control-flow path.

## Transaction ownership

- Public owner requests in module Contracts and Service.Contracts preserve the
  caller's unit of work. They must not implement `ITransactionalCommand`, including
  through the `IAtomicCommand` marker. `ConsumerTransactionBoundaryTests` discovers
  all public request types instead of maintaining a request-name allowlist.
  Two explicitly reviewed outer entrypoints retain transaction ownership:
  `ExchangeAdminImpersonationCommand` (HTTP) and `SendClientTaskRemindersCommand`
  (job). The guard forbids foreign Application modules from acquiring these commands.
- `ITransactionalCommand` saves tracked changes after a successful handler. It
  does not wrap arbitrary SQL executed inside the handler in that save transaction.
- `IAtomicCommand` includes handler execution and saving in one transaction. Meals
  handlers that acquire `IMealAchievementEvaluationRequest` must use this mode:
  its provider writes SQL immediately. `AtomicMealBoundaryTests` discovers those
  constructor dependencies and requires atomic requests. Moving this dependency
  behind another service requires a fresh boundary review and integration test.
- New immediate-write capabilities require an explicit transaction policy and
  failure-path test. The runtime boundary inventory is reviewed policy, not
  a dynamic call graph or an automatic inference of transaction safety.

## Executable evidence

| Guarantee | Evidence source |
| --- | --- |
| Discarded Result expressions fail compilation; unrelated values and explicit suppressions remain valid | [IgnoredResultAnalyzerTests](../../Tooling/tests/FoodDiary.Analyzers.Tests/IgnoredResultAnalyzerTests.cs) |
| Every public owner request preserves caller commit ownership | [ConsumerTransactionBoundaryTests](../../Tooling/tests/FoodDiary.ArchitectureTests/ConsumerTransactionBoundaryTests.cs) |
| Immediate meal evaluation writes require atomic handler execution | [AtomicMealBoundaryTests](../../Tooling/tests/FoodDiary.ArchitectureTests/AtomicMealBoundaryTests.cs) |
| Foreign repositories are detected after sibling-project relocation; empty discovery fails | [ArchitectureSourceDiscoveryTests](../../Tooling/tests/FoodDiary.ArchitectureTests/ArchitectureSourceDiscoveryTests.cs) |
| Failed transaction attempts discard staged entities, outbox state and callbacks; image-reference races preserve integrity | [BoundaryReliabilityIntegrationTests](../../Platform/tests/FoodDiary.Infrastructure.IntegrationTests/Integration/BoundaryReliabilityIntegrationTests.cs) |
| Achievement processing respects lease ownership | [AchievementLeaseOwnershipIntegrationTests](../../Platform/tests/FoodDiary.Infrastructure.IntegrationTests/Integration/AchievementLeaseOwnershipIntegrationTests.cs) |

These references identify executable evidence; they do not claim a test was run
for a particular change. Record actual test execution with each delivery. Changes
to a boundary must cover applicable rollback, cancellation, duplicate delivery and
concurrency scenarios with the real persistence provider. Do not infer coverage
of one boundary from tests of another.

`ArchitectureDocumentationTests` checks local links in this document and the main
architecture guide. Negative scanner fixtures prove the guard detects representative
violations; they are not a proof that all possible C# constructs are recognized.
