# Images tests

Images-only domain invariants and repository PostgreSQL tests live here without
duplicates. Mixed UserCleanup, shared outbox, HTTP and migration tests remain with
their established owners. Do not run coverage collectors.

The existing Infrastructure.IntegrationTests project also owns plain, non-Docker
outbox record contract Facts and provider round-trip tests. Shared-engine
enqueue/dispatch and mixed replay coverage remain in the central infrastructure
suites. Do not duplicate the six relocated record lifecycle cases there.

OutboxReplayStreamTests exercises Images-owned list/metadata/tracking on PostgreSQL.
The common four-stream replay transaction/concurrency tests remain central.
