# Admin tests guidelines

Rules for `Modules/Admin/tests/`.

Own Admin application, domain, infrastructure and presentation suites in separate projects, including AdminFeatureTests and handoff tests. Central suites retain cross-module persistence/HTTP/host composition and shared fixtures. Preserve assertions; tests do not use coverage collectors.

Infrastructure.Tests owns scoped role-audit registration/alias checks and proves the impersonation writer resolves without read composition.
Infrastructure.IntegrationTests owns real PostgreSQL role-audit projection tests,
using links to the common provider fixtures. Keep user filtering, limit 1-50,
ordering, nullable actor, cancellation and all projected fields covered. The
former isolated audit helper must not regrow in the central mixed repository test.
