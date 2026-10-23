---
name: fooddiary-frontend-coverage
description: Diagnose FoodDiary Angular/Vitest coverage gaps and add meaningful client, admin and library tests. Use for frontend coverage reports or missing behavior coverage; backend dotCover belongs to fooddiary-backend-coverage.
---

# FoodDiary frontend coverage

Read the frontend and owning project guides and `docs/TESTING_STRATEGY.md`.
Establish the requested projects and behaviors. Discover current targets and
Angular guidance through Angular CLI MCP when available.

Start with the supplied report. Record revision/working delta, project, test
include filters, coverage include/exclude filters, configuration, actual exit code
and failed/skipped tests. Missing reports or collection failure are not zero
coverage. Compare file sets and statement/branch totals before percentages.

Current npm entry points are `test:coverage:app`, `test:coverage:admin`,
`test:coverage:ui-kit`, `test:coverage:tour` and `test:coverage:webmcp`; verify their
definitions in `FoodDiary.Web.Client/package.json`. `test:coverage` runs all five.
Use focused execution while iterating, then the complete requested scope on final
sources. Retain raw coverage and test output under owned artifacts.

Trace gaps to owner behavior. Prefer real template inputs/outputs and visible
states for components, and public state/contracts for services. Cover failure,
cancellation and cleanup where relevant. For auth/realtime races, use controlled
completion signals and existing typed SDK/SignalR fixtures; count unique
connections and prove closure and absence of reconnect. Fix duplicated fixture
callbacks instead of weakening production expectations.

Do not replace templates, broaden coverage exclusions, invoke private methods
solely for hits, or write tests that just repeat an implementation. Library tests
do not prove application integration or native browser behavior. Use
`fooddiary-browser-qa` when that verification is needed, distinguishing mocked
network responses from real API interactions.

Report behavior gaps closed, remaining gaps, actual outcomes and comparable
before/after scope. Follow the existing Wiki route and verification plan.
