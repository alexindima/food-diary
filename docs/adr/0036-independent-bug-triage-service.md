# 0036: Independent bug triage service

Status: Accepted

## Context

MailInbox is intended to remain independently deployable and reusable, with its typed Client a potential NuGet package. Email-driven investigation of FoodDiary defects introduces a different lifecycle: claims, retries, evidence and draft merge requests. Coupling that lifecycle to inbound mail would make MailInbox depend on a particular development workflow.

## Decision

Add BugTriage as a separate service in the same repository, with Application, Infrastructure, Presentation and a composition-only WebApi host. It owns its PostgreSQL database and report processing state. Its only MailInbox dependency is the client package in Infrastructure. Primary FoodDiary modules do not acquire new mail dependencies.

MailInbox exposes generic recipient-filtered keyset pagination and lossless MIME export using its existing metadata/content permissions. These are additive routes, with no changes to existing message routes or DTOs. BugTriage keeps the two service read credentials server-side; local processors receive only the independent BugTriage key. The initial read keys are capability-wide within MailInbox; recipient filtering is not advertised as per-mailbox authorization. Keep MailInbox on the private service network or behind an SSH tunnel. A future reusable consumer-key policy can narrow the service account independently.

The service never executes Codex or Git. A local task claims a report, investigates it in a separate worktree, renews the lease, and returns an outcome or actual draft MR URL. Database leases fence stale writes, and idempotent completion tolerates a lost HTTP response. Stable report branches and existing-PR checks address the external publication boundary; a database lease alone does not make Git publication exactly-once.

Mail is untrusted evidence. SMTP provenance does not establish reporter identity. The initial service imports all messages delivered to the configured recipient; authenticated sender admission must be added before exposing an unattended coding workflow to unrestricted public traffic. No new production integration, schedule, auto-merge or deployment is enabled by adding the service.

## Consequences

- The MailInbox Client remains independent of server assemblies and can be packaged later without BugTriage.
- BugTriage has independent credentials, runtime, initialization and retention. It does not use SSO or FoodDiary's application database.
- A polling scan revisits retained pages, with a receipt keyed by the MailInbox stored ID; mail read state and spoofable Message-Id are not processing cursors.
- Content lifetime defaults to 30 days from receipt, with explicit bounds of 1–90 days. Closed or open report content is purged on the next import cycle after expiry; API access stops at expiry. Minimal ID/time/status receipts remain to prevent reimport. This is an operational queue, not a permanent bug archive.
- A successful draft remains subject to repository checks, enabled Git hooks and human review before merge.
