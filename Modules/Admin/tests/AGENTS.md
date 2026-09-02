# Admin tests guidelines

Rules for `Modules/Admin/tests/`.

Own focused Admin application and domain suites in separate projects. Mixed Users/Identity/Lessons/Ai/ContentReports, shared PostgreSQL, HTTP, host and SSO protocol separation tests remain central. Preserve assertions; tests do not use coverage collectors.

Infrastructure.Tests owns scoped role-audit registration/alias checks.
Infrastructure.IntegrationTests owns real PostgreSQL role-audit projection tests,
using links to the common provider fixtures. Keep user filtering, limit 1-50,
ordering, nullable actor, cancellation and all projected fields covered. The
former isolated audit helper must not regrow in the central mixed repository test.
