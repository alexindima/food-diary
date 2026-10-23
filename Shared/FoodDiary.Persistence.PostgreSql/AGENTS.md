# PostgreSQL transaction completion

Own only provider-specific verification of transaction commit outcomes. This
library is shared by coordinated persistence, independent AI transactions and
outbox claims. Do not add application/domain dependencies, context creation,
transaction orchestration, saving, callback execution or retry policies.

Preserve the result computed by the caller when PostgreSQL confirms commit.
Rethrow the original failure only after confirmed abort. Unknown outcomes must
raise a nontransient exception and must never authorize callback replay.
Verification uses an independent connection and a bounded cancellation budget.

Provider recovery tests live in the existing platform infrastructure test suites;
consumer tests must exercise the actual owner operation with retries enabled.
