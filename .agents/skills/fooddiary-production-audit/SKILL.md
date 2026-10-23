---
name: fooddiary-production-audit
description: Audit FoodDiary production logs in Grafana/Loki, correlate incidents with service and host health, and produce a sanitized evidence-based report. Use for scheduled production-log audits and operational incident triage; not local browser QA or repository security scans.
---

# FoodDiary production audit

Produce a repeatable account of what happened in production, what changed since the previous audit, and which findings justify action. This skill supplies the audit method; the current user or automation prompt supplies authorization for code changes and Git publication.

## Context and access

- Start from the task's actual existing repository, currently `D:\FD`. Read its current root `AGENTS.md`, especially Production Grafana and SSH Access. A path in a prompt does not repair a missing chat working directory; verify the checkout before repository commands.
- Use `.llm-wiki/index.md` for navigation and verify relevant claims in current source. Consult `docs/backend/BACKEND_OBSERVABILITY_BASELINE.md` and `docs/frontend/FRONTEND_OBSERVABILITY_BASELINE.md` for the telemetry contract. Discover current service names, datasource UIDs, labels and metric names rather than freezing a historical inventory.
- Read the automation's own previous sanitized report and `memory.md`, when present. Distinguish recurring signals, resolved incidents and changes in the deployed revision. A historical classification is a hypothesis to recheck, not an allowlist of harmless messages.
- Production Grafana is `https://grafana.fooddiary.club`. Use read-only requests. Discover datasource UIDs/types before Loki or Prometheus queries; prefer the Grafana datasource proxy where supported. Use browser access if the authentication flow requires it.
- Load authentication inside the requesting process from the secret location in current `AGENTS.md`. Never print credentials, authorization headers, cookies or credential-bearing URLs. Do not capture authentication forms containing secrets. Reject unexpected authentication redirects rather than forwarding credentials to another origin.
- For host correlation use `ssh fooddiary-prod`, read-only, within the task's scope. `fooddiary-bugtriage` belongs to email processing and is not a general production identity.
- When the task authorizes saving audit results in the admin journal, follow [the audit publication recipe](../../../Services/BugTriage/worker/CODEX_AUDIT_TASK.md). Use its forwarding-only BugTriage connection solely for journal publication. Begin the saved run before collection and use its fixed UTC interval; preserve prepared results for identical retries. This exception does not permit other production writes.

## Complete collection in a fixed window

Use [the reusable collector](references/collector.md) for bounded Grafana/Loki
aggregation and a sanitized coverage manifest instead of rewriting collection
loops each run. Keep the existing authenticated transport and supplement its
result with host, interval-metric and incident evidence required below.

Fix the analysis end once in UTC and use the preceding 24 hours unless the task specifies otherwise. Keep that same window across Loki, Prometheus and host logs. State `[start, end)` precisely and show Asia/Tbilisi time in the report. Separate current health snapshots from observations inside the historical window.

Discover sources and labels first. Cover current FoodDiary application services, supporting mail/BugTriage services and frontend telemetry where available. Record unavailable sources separately from services that emitted no events; an empty query does not establish service health.

For Loki:

- Use bounded time slices, often one hour initially. Record the effective response limit and returned count for each slice.
- A slice reaching its limit may be truncated. Split it further or paginate with boundary-aware handling. If many events share a timestamp, partition by stream or use a supported continuation mechanism; advancing the timestamp alone can lose events. Report unresolved gaps instead of claiming complete coverage.
- Query boundaries may be inclusive. Deduplicate overlap using timestamp, canonical stream labels and line, while preserving repeated entries that occurred within one response. Do not deduplicate solely by message text.
- Parse structured fields and common .NET/PostgreSQL/Nginx formats without counting every `timeout`, `error` or `500` substring as a failure. Retain unknown-level counts and do not count stack-trace continuation lines as separate incidents.

Aggregate service/level counts, final HTTP statuses and stable signatures. Keep raw logs in process memory when practical. Any necessary temporary files belong outside the repository and should be removed after use. Persist only sanitized counts, timestamps and minimum supporting excerpts.

Correlate important groups with the relevant evidence:

- Grafana/Prometheus target availability, alerts, API/presentation/DB failures, job failure streaks and last-success age, outbox backlog and provider failures.
- Container running/health status, restart/OOM/exit state, deployment timestamps and deployed SHA. Include stopped containers when investigating an exit.
- Nginx access/error logs, host/kernel/systemd events, backup results and disk space when the symptom calls for them.
- Frontend client/global-error telemetry against backend requests and the deployed frontend version.

Use metric names from current source/configuration. Counter comparisons need interval deltas with reset handling; current zero values do not establish that the entire window was healthy. Include old instance series when a deployment occurred in-window. Avoid commands that dump container environments, secret-bearing inspect output, provider payloads or database contents.

Use a script file or stdin for nontrivial requests/parsing instead of nested PowerShell/SSH quoting. Check HTTP status and response shape before parsing. On access failure, summarize safe error metadata instead of dumping the authenticated request.

## Evidence-based classification

For each important group establish frequency, first/last occurrence, final outcome, impact and supporting evidence. Compare deployed source/revision before attributing an incident to newer local code. Search current GitHub issues and open/closed PRs before proposing another fix.

| Signal | Evidence needed before classification |
| --- | --- |
| HTTP 499 / cancellation | Correlate the finalized request, action/filter logs and downstream failures. A warning labelled 500 followed by 499 may be an observability defect; do not suppress all exceptions followed by cancellation. |
| 400/401/403/404/405 | Check the operation and current contract. Invalid input, authentication failures and scanner traffic may be expected; a broken valid flow still needs investigation. |
| Provider 502/503/timeouts | Distinguish the external response from FoodDiary's mapping, retry/fallback and final outcome. An external failure alone does not justify an application patch. |
| Shutdown / startup warnings | Match deployment and healthy recovery. Repeated startup without a successful running period may be a crash loop. |
| SSR forwarded-header warnings | Check request/render outcomes and the ingress trust contract. Do not enable broad proxy-header trust just to silence warnings. |
| Missing GSSAPI library | Confirm the installed client/version, documented fallback, successful connectivity and absence of DB failures. Similar wording in another version may represent a different failure. |
| Native crash / exit 139 / OOM | Confirm kernel/container exit and recovery. Distinguish memory exhaustion from a native fault and state whether a dump/backtrace exists. A familiar library offset is insufficient for a speculative dependency patch. |

Possible outcomes include expected behavior, external/provider incident, infrastructure incident, observability defect, confirmed application defect and insufficient evidence. Neither alarming wording nor resemblance to previous noise is enough to decide.

## Report and authorized follow-up

Write a concise sanitized report with the exact window, covered/missing sources, collection completeness, service/level totals, important classifications, correlation evidence, deployed revision, changes since the prior audit, actions and concrete next steps. Keep personal data, raw payloads, credentials and private mail out of reports, memory, Git and PRs. Update the automation's own memory with durable incident state and the report location.

When no significant failure exists, say so briefly and state material coverage gaps. Follow the task's notification preferences. Inaccessible sources, saturated queries and failed authentication mean an incomplete audit, not confirmed health.

Implement a fix only when the user/automation authorizes it and logs plus current code support a bounded high-confidence correction. Follow the repository's adaptive change route and scoped guides. When Git delivery is authorized, use the available `fooddiary-delivery` skill for its checks, hooks and publication workflow. Preserve the user's checkout and use an isolated worktree/branch for an authorized fix.

The daily Grafana automation permits at most one draft PR per run; preserve that limit when present in its prompt. This skill grants no merge, deploy or production configuration permission. If evidence, access or required verification is inadequate, report the limitation and next diagnostic step instead of publishing a speculative fix.
