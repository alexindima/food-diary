# FoodDiary AI change workflow

This is the detailed workflow referenced by root `AGENTS.md`. Its requirements
retain the same authority as the root guide; scoped guides, current code, tests
and accepted ADRs determine concrete behavior.

## Daily route

1. State the observable outcome and any behavior that must remain compatible.
2. For a bounded change use `develop -Intent <task> -PlannedPath <paths>`;
   for a large or cross-layer change use `start` with an explicit task workspace.
3. Follow `next` and the grounded research route. Use design when that route
   requires it or a product/compatibility/ownership decision remains unresolved.
4. Implement the smallest complete behavior and verify at its owning boundary.
5. Follow the existing verification planner, refresh affected artifacts, and
   hand off actual results and unresolved risks.

Pass distinct observable outcomes with `start -Criterion` when defects or audit
scenarios are already known. For a visual assessment without implementation,
use the scoped browser guide and `node scripts/ai/task-runtime.mjs list` to find
the owned runtime. Evidence IDs distinguish required checks from supplemental
observations; `evidence-artifact -Id` refers to a review. Use `-Format Json` for
machine-readable results and command-specific `-Help` for parameter semantics.
Preview `task-finish -DryRun` and finish before the final delivery commit, because
completion may record tracked learning observations.

For critical calculations, specify independently derived input/output examples
before implementation and preserve units, source provenance and rounding.
For risky changes, perform a separate review pass from the final diff, relevant
contracts and acceptance criteria. Reproduce suspected failures; a second model's
approval is not verification evidence. Routine local changes use their focused
checks without adding a full governance cycle.

## Conditional obligations and commands

- Use `./.llm-wiki/wiki.ps1 diff` to discover change-set context and `./.llm-wiki/wiki.ps1 verify` before handing off wiki-affecting changes.
- Use `./.llm-wiki/wiki.ps1 brief` to compile risk, scoped instructions, affected modules, focused tests, and review obligations for a non-trivial change.
- Start a large or cross-layer feature with `./.llm-wiki/wiki.ps1 start -Intent <task>`; it captures the baseline, performs initial research, creates a scope-aware acceptance checklist, and initializes governed state when required. For ordinary bugs and bounded features use `develop`; add `-PlannedPath` whenever likely files are known. Follow the adaptive route rather than applying the full governed workflow to every change.
- During implementation, prefer `./.llm-wiki/wiki.ps1 next` for the single recommended action, `phase-next` for governed implementation phases, and `qa` for journey-derived manual scenarios. These facade commands derive from existing Wiki artifacts and do not replace detailed commands when diagnosis is needed.
- Use `research` before editing a non-trivial existing flow. It combines ranked code context, focused tests, known failures, and Git precedents; verify all inferred paths and historical patterns in current sources.
- When research exposes open developer decisions, use `research-next-question` and ask only its highest-priority grounded question. Do not invent a file/line anchor or ask for confirmation when current repository evidence can answer the issue.
- Use `design` only when the adaptive route requires it or when research exposes a blocking product, compatibility, privacy, provider, persistence, or architecture decision.
- For governed work spanning sessions, use `pause` and `resume`; resume must report clean continuity or require a task refresh before edits continue.
- Use `task-handoff -Compact` for a bounded cross-session summary; retain the full handoff for audit and automation.
- Use `journeys` for behavioral changes to identify affected FoodDiary end-to-end scenarios. In governed work, map applicable journey scenario IDs to acceptance criteria.
- For governed work, use `delivery-status` during implementation, `delivery-replan -Reason <evidence>` for intentional scope/plan divergence, and `delivery-validate -FailOnInvalid` before completion. Critical and architectural changes also require `delivery-critique -FailOnInvalid`.
- Use `./.llm-wiki/wiki.ps1 test-plan` to derive focused tests and risk scenarios before implementing or reviewing behavioral changes.
- Use `./.llm-wiki/wiki.ps1 decision` when project references, dependency injection, deployment, ownership, or module graph changes.
- Use `dependencies` for manifest changes and `rollout` for migrations, configuration, jobs, providers, or deployment-sensitive changes.
- Use `hotspots` and `test-gaps` to calibrate review depth; treat test references as navigation evidence, never as execution coverage.
- Use `topology` before changing external clients, webhooks, background workers, recurring jobs, or message delivery behavior.
- Use `privacy` before changing credentials, identity/health/financial data, private content, exports, logs, caches, queues, or provider sharing.
- For explicitly bounded autonomous work, use `task-init` and `task-validate` to detect accidental changes outside the declared path scope.
- Use `./.llm-wiki/wiki.ps1 trace -Query <command-or-query>` before changing an existing backend flow.
- Use `./.llm-wiki/wiki.ps1 ownership` for cross-module changes and `api-compat` after API snapshot changes.
- Search `./.llm-wiki/wiki.ps1 failures -Query <error>` before repeating diagnosis; record only verified, reusable resolutions.
- Use `docs/ai/CODE_REVIEW.md` for consistent AI-assisted review and resolve triggered change-policy obligations through an evidence bundle when the task warrants formal handoff.
