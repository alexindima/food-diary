---
name: fooddiary-local-runtime
description: Prepare or recover the complete local FoodDiary application for development or browser QA, including dependency readiness, frontend-to-API connectivity, and ownership of running processes. Use for local launch requests or environment blockers, not production operations.
---

# FoodDiary local runtime

Return a working local application whose checkout, API, dependencies, and browser connections are known.

For an isolated task runtime, start with `scripts/Start-FoodDiaryTask.ps1` and
`docs/ai/AI_DEVELOPMENT.md`. It prepares owned data, ports, configuration and
frontend/API connectivity. Its disabled-provider statuses are explicit limitations,
not evidence that those journeys have passed. Use the normal root launch commands
when the user requests their usual local environment.

## Discover the current environment

Resolve the active checkout using Git, including whether it is a worktree. Read its root and relevant host/client/service AGENTS.md. Use `.llm-wiki/index.md` to navigate to runtime topology, then verify the relevant claims in current composition, configuration, and service documentation. Resolve all repository references against this checkout.

Inspect listening ports and existing application processes before starting replacements. Maintain a compact runtime record: checkout, frontend/admin/API URLs, process or session identifiers, which processes are reused versus created, and relevant isolated data-store identifiers. Avoid putting secrets or full sensitive process arguments in this record.

Determine which dependencies the requested journey requires from current configuration. A frontend and API alone may leave image uploads, email, background work, or cache-dependent behavior unavailable. Check actual dependencies rather than starting every possible service by default.

Use the root guide's standard frontend build, frontend start, and backend run commands. If concurrent work requires a different port or isolated database, document the reason and verify the frontend actually uses that API. Follow repository-level `.artifacts/` conventions for isolated .NET output. Bound concurrent builds according to available memory and other active work.

## Prove readiness

Check the requested frontend and API endpoints, then verify a real request from the rendered application reaches the intended backend. Do not substitute a successful health check for frontend-to-API connectivity. Confirm the checkout or revision serving the browser when multiple copies exist.

When authentication is required, obtain local credentials through the location and rules in the current root guide. Use synthetic accounts appropriate to the authorized task. Never place secrets in generated launch helpers, command output, screenshots, or handoff records.

For the relevant flow, probe dependencies through useful application outcomes: an owned image round trip, a local inbox operation, or the applicable job behavior. Record each relevant integration as verified working locally, mocked, blocked, or untested, with its evidence or blocker. An internal billing overview does not prove that the payment provider works; opening a mail page does not prove delivery. Mark intercepted requests separately from real provider readiness.

Before starting a helper or service, record how to stop that owned process safely. Preserve reused processes and shared data. Leave the requested local application running when that is the user's intended result; stop only owned temporary helpers no longer needed.

## Return a usable handoff

Give clickable local application URLs, the serving checkout/revision, readiness evidence, and remaining environment blockers. Persist the safe runtime record for a long QA pass so another continuation can verify the existing environment before recreating it. Use the current repository commands and documentation as the source of launch details instead of copying a historical port map into this skill.
