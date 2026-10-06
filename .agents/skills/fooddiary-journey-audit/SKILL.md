---
name: fooddiary-journey-audit
description: Run a reproducible FoodDiary user or admin UX audit with real browser interactions, scenario coverage, owned fixtures, and verified fixes. Apply to broad user passes and journey regressions, not ordinary frontend implementation or a general security scan.
---

# FoodDiary journey audit

Make the scope of a browser audit and its actual evidence visible. A page that opens is one checked outcome, not evidence that its filters, forms, and failure states work.

## Establish the pass

Resolve the current FoodDiary checkout and read its applicable AGENTS.md. Resolve the paths below against that repository root; they are maintained sources, not copies embedded in this skill.

- Use `.llm-wiki/index.md` for navigation and current source verification.
- Derive relevant journeys and failure cases through the current `wiki.ps1 journeys`, `qa`, and `test-plan` interfaces. Inspect help when their arguments are unclear.
- Read `docs/TESTING_STRATEGY.md` and the client/admin scoped guides for verification commands.

For a broad request such as "try everything", establish a finite initial pass from current routes and user outcomes. State the roles, meaningful viewport/locale coverage, integrations available in the local environment, and completion criteria. Use a reasonable scope from the request; ask only when an unresolved product choice changes the work. Discover additional scenarios as evidence warrants and record their effect on completion.

Use a scenario ledger with: stable ID, role, precondition, action, expected observable result, execution status, evidence, and fixture/cleanup state. Reuse an existing maintained ledger where practical. Keep the scenario result distinct from the result of the automation script. Statuses should distinguish untested, passed, failed, blocked, and fixed then rechecked.

## Exercise outcomes

Prioritize the actual journey and risks present in the feature:

- New user and populated account behavior. For access changes, cover the relevant user/dietologist/admin roles, permitted and denied API outcomes, sharing permissions, and consent revocation. Keep unavailable roles explicitly untested.
- Create, edit, clear optional fields, cancel, retry, reload, and remove owned records.
- Loading, empty results, validation errors, API failure, and preservation of unsaved input.
- For lists: meaningful individual filters and combinations, reset, pagination, invalid page, URL state, reload, and browser back/forward. Record the combinations selected rather than claiming every combination.
- For asynchronous UI: stale responses, changing the selected item while loading, and leaving the page during a request.
- For nutrition: applicable quantities, portions, units, and persistence after reopening.
- Relevant narrow-screen, keyboard/focus, accessible-name, and EN/RU behavior.

Choose selectors from the rendered accessible names and inspect ambiguous matches. Observe application outcomes through the UI and relevant real network responses; a click returning successfully does not prove the operation succeeded. Reopen or reload when persistence is part of the contract.

Wait for the expected rendered value after asynchronous state changes. For the admin app, exercise its real launch/SSO entry and wait for the authenticated screen before navigating further; an origin change alone does not establish a finished handoff. Keep evidence of probe timing errors separate from confirmed application defects.

For controlled network failures or delays, first prove the scenario actually issues the request; cached data may bypass it. Bound waits and release intercepted requests in `finally`. Diagnose a missing selector or failed probe independently before reporting an application defect. Mark mocked evidence explicitly and separately from real integration evidence.

When using API mocks, verify response shape, pagination, status codes, and query parameters against current contracts and the snapshots under `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`. Match pathname and query deliberately; stale mocks can fail a correct application or hide a broken integration.

## Own data and preserve evidence

Use synthetic accounts and fixtures permitted by the user's request and repository rules. Record created IDs and ownership; clean up only those fixtures. Capture enough evidence to recover after compaction without including credentials, tokens, or private user data. Reconcile cleanup with current state before declaring the pass complete.

When fixes are authorized, reproduce, make the bounded change, and recheck the original scenario plus directly affected variants. Run focused checks while iterating and the required final checks on the final change set. Repeat a broad suite when new changes or failures justify it.

## Report completion precisely

Report checked scenarios and roles, confirmed defects and fixes, verification on the final revision, blocked integrations, remaining scenarios, and cleanup state. A route count, an accessibility scan, or a unit-test total is evidence for that check, not proof of complete journey coverage. Keep the ledger usable for the next pass so the audit need not be rebuilt from screenshots or chat history.
