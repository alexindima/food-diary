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
