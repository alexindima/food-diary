# Identity tests

Keep focused Identity application behavior in the nested application test project. The Domain test project owns EmailTemplate, UserRefreshTokenSession and UserLoginEvent invariant tests.
Mixed Admin, Users, host, HTTP, provider, shared persistence, and cross-module tests
remain with their established owners. Do not duplicate moved tests.

Infrastructure.Tests owns email-template cache/fallback and module registration
contracts. Infrastructure.IntegrationTests owns the login-event repository's
PostgreSQL reporting and retention cases, linking only the shared database fixture.
Central suites retain mixed persistence coverage, Admin consumers and host tests.
Run both module infrastructure projects plus central Infrastructure integration
tests for changes to these adapters; do not substitute InMemory tests for SQL proof.

Telegram replay provider tests live here, not in the mixed Dietologist suite.
Keep the extracted sequential assertions and real PostgreSQL tests for concurrent
single consumption, fingerprint storage, expiry boundaries and cancellation.
Registration tests verify scoped lifetime and model/adapter assembly ownership.

Infrastructure.Tests/Authentication owns JWT behavior and password-hash tests plus
singleton/assembly/composition contracts. Shared JwtOptions validation tests stay
in central Infrastructure.Tests; HTTP authentication tests remain host-owned.
