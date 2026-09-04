# Identity tests

Infrastructure.Tests/Integrations owns Google/Telegram provider and option tests,
including offline signing/claim/timestamp cases and singleton/clock registration.
No live Google or Telegram credentials/API calls are required. Mixed HTTP/host
authentication and replay persistence tests retain their existing owners.

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

Authentication also owns all six relocated AdminSsoService cases and protocol
regressions. Resolve the real internal in-memory store through public central DI
and dispose the fixture provider; do not add public/internal-access test seams.
Verify encoding/payload/token forwarding, exact expiry boundary, cancellation,
singleton composition and host-selected store replacement. Mixed Admin/Identity
protocol-isolation tests stay central; never duplicate them in module tests.
