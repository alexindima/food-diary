# MailInbox review — 2026-09-05

MailInbox has a coherent service boundary and substantial executable coverage, but the existing green suites missed four correctness defects. All four were reproduced and fixed locally in this review. This is a code and behavior review, not a completed Codex Security scan or a production acceptance certificate.

## Scope and method

Reviewed the production paths across Domain, Application, Infrastructure, Presentation, WebApi, Client and Initializer: SMTP admission and storage, MIME/DMARC decoding, PostgreSQL migrations and quotas, deduplication, retention, HTTP queries and mutations, configuration and startup, capability-key routing, telemetry, and service-to-service contracts. Read supporting Compose configuration, deployment and certificate-renewal scripts, architecture rules and the data-lifecycle guide. Executed every MailInbox test project, with real PostgreSQL containers and local SMTP sockets where applicable. Test source review concentrated on behavior and missing regression cases; this is not a claim of independent line-by-line security coverage of every test file.

The workspace contained a large unrelated shared-contract refactor before review. MailInbox had no pre-existing changes. Product edits remain inside MailInbox; documentation records the corrected behavior. No production access or deployment was performed.

## Confirmed defects and fixes

| ID | Severity / kind | Trigger and observed consequence | Fix and regression evidence |
| --- | --- | --- | --- |
| MI-01 | major / defect | `MailInboxMailboxFilter.CanAcceptFromAsync` returned `false` when a session, source or sender temporarily exhausted its message quota. The actual SMTP listener replied `550 mailbox unavailable`, which tells a sender that delivery has permanently failed. Recipient-count exhaustion used the same permanent rejection mechanism. | Temporary message limits now throw the library's response exception with `450`; excess recipients return `452`. Local socket tests cover all four limits. Unknown recipients, disallowed sources and oversized messages retain their permanent rejection behavior. |
| MI-02 | warning / defect | `NpgsqlInboundMailStore.GetMessageDetailsCoreAsync` decoded stored MIME bytes as UTF-8 before sending them to the DMARC parser. A stored binary gzip attachment was corrupted and its preview was absent. | The parser interface consumes `byte[]`; the store passes the original `bytea` value. A PostgreSQL round-trip regression reproduces the original failure and now returns the report domain. The existing HTTP string field remains a text preview. |
| MI-03 | warning / defect | A base64-encoded XML attachment containing a UTF-16 BOM was unconditionally decoded as UTF-8 and produced no DMARC preview. | Plain XML now uses the same bounded BOM-aware reader as compressed XML. A UTF-16 attachment regression now passes. MIME loading and attachment decoding also receive cancellation, and the parsed MIME message is disposed. |
| MI-04 | warning / defect | A body-size cutoff between an emoji's UTF-16 surrogate pair produced an incomplete character in the extracted text. The regression observed `a` followed by an unpaired high surrogate instead of valid truncated text. | Truncation steps back one UTF-16 code unit when needed. Text and HTML use the same helper; original MIME bytes remain unchanged. The regression now returns the valid prefix `a`. |

The new tests were run against the original behavior first. SMTP and XML regressions failed, the database binary-attachment regression failed, and the Unicode regression failed. Their fixed counterparts pass. A separate expanded socket test verifies that the listener accepts new sessions after rejecting an excess per-source connection; its retry accounts for asynchronous server-side connection cleanup. That test did not establish another product defect.

## Existing behavior retained and checked

| Area | Evidence and assessment |
| --- | --- |
| Layer ownership | Application depends on narrow domain/shared contracts; SMTP, MIME and PostgreSQL remain in Infrastructure; HTTP transport remains in Presentation; Client stays separate. No project-reference or DI graph change was needed. |
| Ingestion and retries | Actual envelope recipients determine delivery. Size, MIME structure, metadata, connection and processing limits exist. PostgreSQL uses an ingestion advisory lock and fingerprint lookup before insert; duplicate deliveries do not consume another daily quota. |
| Persistence and lifecycle | Migrations are versioned and transactionally recorded under a migration lock. Integration tests cover repeated/concurrent initialization, quota rollback, deduplication boundaries, read-state updates and content/metadata retention. |
| HTTP and client | Existing tests cover capability-specific keys, invalid inputs, missing messages, bounded concurrent operations, timeouts, cancellation and response mappings. HTTP routes, payloads and status contracts were not edited. |
| Deployment and operations | Dedicated database and runtime-role setup remain in place. The Compose host, TLS entrypoint, initializer and certificate-renewal restart hook were reviewed as source. Live certificate renewal, production grants, backup restoration and monitoring were not exercised. |
| Compatibility | No schema migration, HTTP snapshot update, package change, quota adjustment, retention-policy change or frontend change is part of this patch. Rebuilding and deploying the normal MailInbox service artifact is sufficient to apply the product fixes. |

## Verification

The original seven MailInbox suites passed **435 tests, zero skipped**. With the added cases, the reviewed suite set contains **442 passing tests, zero skipped**:

| Suite | Passing tests |
| --- | ---: |
| Application | 47 |
| Client | 29 |
| Domain | 16 |
| Infrastructure | 214 |
| Initializer | 13 |
| Presentation | 63 |
| Integration / PostgreSQL | 60 |

Infrastructure and integration suites were rerun after their corresponding fixes. The other five suites passed in the baseline and their production layers were not changed. The final full solution build passed with zero warnings/errors, and all **1,160 architecture tests passed**. A NuGet vulnerability check of the WebApi, Initializer and Client projects, including transitive packages, reported no known vulnerable packages from the configured NuGet source on this date; this does not substitute for a source security audit.

`git diff --check` passed for the patch. The scoped whitespace formatter returned success with a workspace-loading warning; the actual build and architecture tests passed separately. Wiki research, trace, design, test-plan, topology, privacy and journey navigation were run. After refreshing stale generated indexes, scoped Wiki verification passed all seven stages. Governed delivery remains blocked on evidence lineage: it reports two unresolved checks and eight definition/lineage issues despite the recorded successful commands. Requirements, all four acceptance criteria, proof of change and the 12-path scope check pass. An unscoped replan attempted to absorb the unrelated working-tree refactor; the task contract and packet were subsequently restored to the explicit Services/MailInbox/documentation scope. These workflow results are retained in the local evidence directory and are not represented as a passing delivery gate.

The final governed critique also returned `valid=False`; its automated `approve` verdict is not a passing delivery receipt. Local command logs and TRX evidence are under `.artifacts/mailinbox-audit-20260905/`. Generated evidence is local, not committed. To repeat the service tests in PowerShell:

```powershell
foreach ($project in (rg --files Services/MailInbox/tests -g '*.csproj')) {
    dotnet test $project --artifacts-path C:/FD/.artifacts/mailinbox-verification
    if ($LASTEXITCODE -ne 0) { throw "MailInbox verification failed: $project" }
}
```

## What remains before calling the service fully closed

1. **The formal security scan did not pass capability preflight.** Scan `4cbf65de-cda7-4587-8b87-922a84bc00e8` remains resumable. The helper returned `incomplete` for `active_multi_agent_mode`: the active tools expose delegation and four total slots, but do not establish the runtime owner/version required by the plugin. It provided no concrete configuration patch. No configuration was invented or changed, and no completed security report or absence of vulnerabilities is claimed. The scan skill explicitly requires `ready` before its substantive audit.
2. **Production readiness still needs operational evidence.** Deployment of the fixes, live SMTP delivery, current TLS validity/renewal, database backup restoration, and capacity/retention monitoring have not been verified on the server in this task. Repository tests cannot establish those facts.
3. **Stable feature scope does not remove maintenance.** MailInbox still depends on runtime and package updates, certificate rotation and database operations. Raw MIME is retained for a bounded period; the existing `RawMime` HTTP string is not a binary attachment export. DMARC remains an unverified preview, as documented in the existing lifecycle guide.

No broader rewrite is justified by the confirmed functional findings. The four fixes and their regression coverage can be reviewed and deployed independently of completing the remaining formal and operational checks.
