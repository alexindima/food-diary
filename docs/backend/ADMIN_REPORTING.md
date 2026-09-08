# Admin reporting

## BugTriage journal

The primary API exposes `GET /api/v1/admin/bugs` to the Admin role. It reads the independent BugTriage `GET /api/report-journal` endpoint through the Admin Infrastructure HTTP adapter. No BugTriage server assembly or database is referenced by the primary application.

Configure `AdminBugTriage:BaseUrl` and `AdminBugTriage:ReadApiKey` in the primary API's external configuration. Set the matching `BugTriageHttp:ReadApiKey` in the BugTriage service. The read key must be 32–256 characters and different from `BugTriageHttp:ApiKey`. Keep both keys outside source control and browser configuration. The worker key is deliberately rejected by the journal; the journal key does not authorize claim, renewal, completion or MIME retrieval.

HTTPS is required. For a local or forwarded loopback development endpoint, explicitly set `AdminBugTriage:AllowInsecureLoopback=true`. Redirects are disabled. If both configuration values are absent, the API returns `isConfigured=false`, which the UI distinguishes from a configured journal with no matching reports. A configured but unreachable service remains a request failure, with retry available.

The journal filters receipt time using an inclusive `fromUtc` and exclusive `toUtc`. Status, literal subject search and ID filters apply before count and pagination. It returns receipt metadata, attempt count and retained investigation results; it never returns MIME or lease credentials. Subject, summary and PR URL become unavailable at content expiry even before the cleanup worker purges them. This remains a retention-limited processing journal, not a permanent archive.

These configuration options do not deploy or activate a connection. Provision the separate key through the deployment's secret mechanism when enabling the integration.

## Persisted audit journal

`GET /api/v1/admin/audit` reads the existing structured audit table, with period, actor, client, action and target filters applied before pagination. Both the API and UI remain Admin-only. Responses prohibit caching.

Coverage is intentionally explicit: the table currently records dietologist collaboration events. Role-change records and impersonation sessions have their own existing readers. `IAuditLogger` writes technical logs rather than this table, so this endpoint does not claim to include every administrative operation. No missing historical events are synthesized.

The new journal does not alter audit writes, transactional ownership or metadata retention. Future event coverage must be added at the owning workflow and tested with its transaction boundary.

## Diary retention

`GET /api/v1/admin/analytics/retention` groups retained accounts by UTC registration date. Activity means creating a persisted meal entry (`CreatedOnUtc`), independently of the meal's backdated diary date. Counts deduplicate users, not meal rows. The selected period applies to registration cohorts and the separate daily activity table.

Activation counts users with an entry after registration during calendar days D0–D6. D1, D7 and D30 count activity on those exact calendar days, rather than activity at any point before the milestone. All retained accounts in the cohort form the denominator. A measure stays null until its complete calendar observation window has ended; null is not zero retention. The response records its observation time.

These are projections of retained records, not immutable historical analytics. Deleting accounts or entries can change past results. Soft-deleted accounts still present in storage remain in the cohort denominator. No individual diary content or account identifiers are returned by this report.

## Acquisition and mail periods

Acquisition compares the selected UTC interval with the preceding interval of identical duration. Event search and pagination affect the event journal; summary counters retain the selected period's complete population. Historical cleanup can make earlier comparison periods incomplete.

Mail journals apply inclusive start and exclusive end, recipient/sender and record filters before counting and pagination. Bug acknowledgments use the inbox record ID as their correlation ID. The outgoing acknowledgment links back to that record, and the incoming message links to correlated outgoing mail. These links do not send or resend messages.

## Template history and publication rollout

Apply the additive `AddLessonPublication` and `AddTemplateRevisionHistory` migrations before enabling the new editors. Existing lessons default to published. New lessons created in the admin editor default to drafts. Public lesson queries and completion summaries exclude drafts; existing completion records survive unpublishing. Completion and achievement recipient counts describe retained lifetime records, not the selected reporting period.

Email and AI template aggregates snapshot the previous values on a meaningful update. History is bounded to the 50 most recent revisions in the reader, but persisted revisions are not automatically pruned. The tables contain template content, not rendered recipient emails. Only administrators may read them. Restoring copies values into the editor and requires an explicit save; AI prompt version numbers continue advancing. No revision history before enablement or editor identity is invented.

Schema changes are additive, but an older application ignores the publication flag and could expose drafts. Roll forward when possible; before reverting the application to an older version, ensure no private draft content remains in the lessons table. Do not drop revision tables as a routine rollback. Existing mail-service clients tolerate the new optional count fields; deploy mail services before the main application to enable those counters. Their absence is shown as unavailable.

## Billing summary scope

Renewal records count persisted payments explicitly marked as renewal, including failed records, by occurrence time (creation time fallback). Scheduled cancellations count the current cancel-at-period-end flag with the period end inside the selected interval. They are not a historical count of cancellation requests. Revenue cards span all providers and do not inherit the table's status/search filters. Linked record IDs can be searched directly in subscriptions, payments and webhooks.
