# Email bug triage

## Scope and architecture

`bugs@fooddiary.club → MailInbox → BugTriage → local Codex task → draft MR`

BugTriage is a separate service under `Services/BugTriage/`. It consumes only `FoodDiary.MailInbox.Client`; MailInbox knows nothing about defects, AI, Git or merge requests. See [ADR 0036](../adr/0036-independent-bug-triage-service.md).

The first version includes a durable PostgreSQL queue, recipient-filtered import, MIME and image access, expiring leases, attempt limits, idempotent completion, a bounded recent-results endpoint and a local Python bridge. The local task recipe describes investigation, tests and draft creation. It is not a standalone Codex scheduler or an automatically enabled deployment.

## Server configuration

Create a **separate** PostgreSQL database named `fooddiary_bugtriage`. Supply settings through the service environment or .NET user secrets, never a committed file:

| Setting | Meaning |
| --- | --- |
| `ConnectionStrings__BugTriage` | BugTriage database connection; never the main or MailInbox database |
| `MailInboxClient__BaseUrl` | Trusted MailInbox HTTPS origin |
| `MailInboxClient__MetadataApiKey` | Existing MailInbox metadata capability |
| `MailInboxClient__ContentApiKey` | Existing MailInbox content capability; a state key is not required |
| `BugTriageHttp__ApiKey` | Independent random 32–256-character token for local processors |
| `BugTriage__Recipient` | Default `bugs@fooddiary.club` |
| `BugTriage__PollInterval` | Default five minutes; between ten seconds and one day |
| `BugTriage__ImportTimeout` | Default ten minutes; cancels an unsuccessful scan so it can retry |
| `BugTriage__ContentRetention` | Default thirty days from receipt; configurable 1–90 days |
| `BugTriage__MaxConcurrentReports` | Default one live report lease across all service instances; configurable 1–10 |
| `BugTriage__MaxImportsPerPoll` | Default 100 new reports per scan; configurable 1–1000 |
| `BugTriageHttp__LeaseDuration` | Default thirty minutes; renew at least every ten minutes |
| `BugTriageHttp__MaxAttempts` | Default three abandoned lease attempts |

MailInbox redirects are disabled and MIME downloads are bounded to 10 MiB. HTTP is accepted only for an explicitly enabled loopback development connection (`MailInboxClient__AllowInsecureLoopback=true`); use HTTPS for remote connections.

Initialize using a database owner credential, then run with a role granted only SELECT/INSERT/UPDATE/DELETE on `bugtriage_reports`:

```powershell
dotnet run --project Services/BugTriage/FoodDiary.BugTriage.WebApi -- --initialize
dotnet run --project Services/BugTriage/FoodDiary.BugTriage.WebApi
```

Initialization is idempotent and serialized by a PostgreSQL advisory lock. Runtime does not run migrations. The default listener is `http://127.0.0.1:5099`. Keep it on loopback behind an authenticated SSH tunnel, or configure HTTPS through a reverse proxy before remote use. `GET /api/bug-reports` with the key checks API and database availability; import failures emit a content-free warning and retry on the next cycle.

Add the bugs recipient to the effective MailInbox SMTP configuration and any upstream mail routing. The source default includes it; deployed environment overrides must be checked separately. Publish MailInbox's additive export endpoints before starting BugTriage. Existing Admin consumers keep their original routes and contracts.

## Local use

Save a private JSON file at `%USERPROFILE%\.codex\secrets\food-diary.bugtriage.json`:

```json
{
  "baseUrl": "https://bugs.fooddiary.club",
  "apiKey": "<generated-token-stored-only-outside-git>"
}
```

An SSH tunnel may instead use `http://127.0.0.1:5099`; no inbound connection to the workstation is needed. Do not expose the JSON file or its contents in prompts, terminal output or screenshots.

```powershell
python Services/BugTriage/worker/bugtriage.py list
python Services/BugTriage/worker/bugtriage.py claim
python Services/BugTriage/worker/bugtriage.py renew --lease <private-lease.json>
python Services/BugTriage/worker/bugtriage.py complete --lease <private-lease.json> --result <result.json>
```

`claim` saves the report, raw MIME and supported images under `%USERPROFILE%\.codex\bugtriage\<report-id>`. Filenames are generated locally; archives and executables are never extracted or run. The lease is saved before MIME download so a failed download does not lose ownership information. Keep those files private and remove them no later than `contentExpiresAtUtc` in the lease response. The server cannot delete exports already downloaded to a workstation.

Example completion file:

```json
{
  "outcome": "needs_information",
  "summary": "Could not reproduce. Need the browser version and the exact steps after sign-in.",
  "mergeRequestUrl": null
}
```

Supported outcomes: `draft_ready`, `needs_information`, `not_confirmed`, `duplicate`, `failed`. `draft_ready` requires a real HTTPS draft link. A failed or uncertain investigation is not evidence that no bug exists.

Use [the local Codex task recipe](../../Services/BugTriage/worker/CODEX_TASK.md) when enabling a scheduled task. The recipe requires worktree isolation, stable branch names, checking existing PRs, lease renewal, appropriate tests, enabled commit/push hooks, and draft-only publication. Initially run it manually against a synthetic report before enabling unattended processing. Sender addresses alone are not authenticated admission to an autonomous coding workflow.

## HTTP behavior and failure recovery

- `GET /api/bug-reports`: latest 50 unexpired outcomes, no MIME or lease tokens.
- `POST /api/bug-reports/claim`: 200 with report and lease, or 204 when empty.
- `POST /api/bug-reports/{id}/renew`: 204 on success, 409 for expired/wrong ownership.
- `POST /api/bug-reports/{id}/complete`: 204 for first or identical completion, 400 for invalid outcome, 409 for stale ownership or conflicting retry.
- `GET /api/bug-reports/{id}/mime`: requires `X-BugTriage-Lease` and an active lease; otherwise 404.
- All routes require `X-BugTriage-Key`, disable caching and share a 120-request/minute limiter. Failures return 503 without provider exception details.

An abandoned lease becomes claimable again until the attempt limit; exhausted reports become `failed` when the queue is next claimed. Restarting the service preserves receipts and outcomes. Replaying an identical completion is safe. Do not publish from a worker after a failed renewal; the service cannot roll back an already created external PR. The draft branch uses the stable report ID to support reconciliation.

Imports rescan retained mail in descending `(received_at_utc,id)` pages rather than fetching just the newest N messages. Unique stored IDs prevent SMTP retries or repeat scans from creating duplicate work. An import failure retries next cycle; tune the scan timeout if retained mail volume prevents a full traversal. Content already purged by MailInbox becomes `content_unavailable` and is not assigned to a worker.

## Verification and rollout

Claims serialize capacity checks with a PostgreSQL transaction advisory lock, so the configured live-lease limit applies across instances using the same database. Configure the same limit on each instance. Completion or lease expiry releases capacity; an idle claim returns 204 while capacity is occupied. Imports stop after `MaxImportsPerPoll` new receipts and revisit retained mail on the next scan. Existing receipts do not consume that budget. These limits bound processing load; they do not classify spam or authenticate senders.

The queue limits require a service restart with the updated build, but no schema migration. Existing configuration uses the defaults above. Under sustained arrivals exceeding the import budget, older mail may wait until traffic subsides or retention expires; monitor the backlog and adjust the budget deliberately.

```powershell
dotnet test Services/BugTriage/tests/FoodDiary.BugTriage.Tests
dotnet test Services/MailInbox/tests/FoodDiary.MailInbox.IntegrationTests
python -m unittest discover -s Services/BugTriage/worker -p test_bugtriage.py
dotnet test tests/FoodDiary.ArchitectureTests
```

PostgreSQL tests require Docker. They exercise duplicate imports, competing claims, lease expiry/fencing, retry limits, completion retries and content purging. MailInbox tests cover recipient isolation, tied timestamps, key capabilities, binary fidelity and retention.

The production Deploy workflow builds and signs the BugTriage image, then invokes `Services/BugTriage/deployment/deploy.py` after MailInbox is updated. This idempotently provisions a separate PostgreSQL container and volume, initializes the schema with the owner role, grants only DML to the runtime role, and checks the token-protected API. Private configuration lives in `/opt/fooddiary/bugtriage/.env` (mode 0600); existing tokens are preserved across deployments. MailInbox read capabilities are copied server-side without being printed. PostgreSQL has no published port and uses a dedicated internal Docker network.

The API binds only to server loopback port 5099. The provisioned workstation uses SSH alias `fooddiary-bugtriage`, a dedicated key restricted to local forwarding to `127.0.0.1:5099`, and local port 15099. Run `./Services/BugTriage/worker/Connect-BugTriage.ps1` before bridge commands. The private JSON uses `http://127.0.0.1:15099`; no new DNS, SSO or public HTTPS endpoint is needed. The administrative `fooddiary-prod` alias is used only for provisioning and maintenance.

After activation, send a synthetic bug with an image and verify import, claim, MIME retrieval, renewal and completion before scheduling unattended work. Enabling autonomous processing of arbitrary public mail requires an explicit admission policy; this deployment does not itself enable a Codex schedule.

Rollback: stop BugTriage and the local task, revoke its token and retain the BugTriage database for diagnosis under the retention policy. MailInbox and the main application continue operating; no primary database schema or SSO change is involved.

## Receipt acknowledgements and outgoing mail

The primary application can send editable receipt acknowledgements for persisted bug mail, independently of the triage worker. See [Outgoing mail](OUTGOING_MAIL.md) for ownership, duplicate/auto-response handling, journal access and opt-in rollout.
