# Publish a production log audit

Use only when the task authorizes saving sanitized audit results in BugTriage. Grafana, logs and host correlation remain read-only; publication does not authorize deployment, configuration changes, mail, or additional Git actions.

## Start and recovery

Run `./Services/BugTriage/worker/Connect-BugTriage.ps1` to check the credential-free authenticated SSH stdio route used for publication. Each bridge request owns its forwarding-only SSH connection; do not start or trust a local port 15099 listener. Use it solely for this API; production host correlation still follows the production-audit skill. The bridge loads the private BugTriage configuration without printing its key.

1. Run `python Services/BugTriage/worker/audit.py retry` to retry saved completions using their existing IDs and payloads. Do not repeat investigation or PR creation for a publication retry. Failure must not prevent today's read-only audit.
2. Run `python Services/BugTriage/worker/audit.py start` once. It prints a private `run.json` path and saves the UUID and UTC interval before requesting registration. Use that file's `[fromUtc,toUtc)` interval for every collection and report. If registration fails, the file still exists; continue with its interval.
3. Preserve that run file when interrupted. Never create another ID merely because a response was lost. The ID belongs to an audit attempt, not to a recurring incident.

Publication requires the trusted worker credential (`X-BugTriage-Key`), not the Admin read key. It does not claim mail or acquire a mail lease.

## Finish

Save a sanitized `result.json` beside the run file:

- `outcome`: `clean` for complete coverage without significant findings; `findings` for complete coverage with actionable findings; `incomplete` for unavailable/truncated/unverified sources; `failed` if the audit could not be performed.
- `summary`: nonempty concise coverage, outcome and material gaps, maximum 8,000 characters.
- `findings`: at most 20 significant problems requiring review or action. Each has `title`, `summary`, `outcome` and optional `mergeRequestUrl`. Outcomes use the existing `needs_information`, `not_confirmed`, `duplicate`, `draft_ready`, `failed` meanings. `draft_ready` requires a real HTTPS draft PR and completed checks. The bridge assigns stable finding IDs within the run.

Do not create a finding for every warning, expected response or healthy run. Distinguish observations from confirmed defects. Keep raw logs, private mail, personal data, tokens, authorization headers and credential-bearing URLs out of the result. A full sanitized report can remain in the automation's usual report location.

```json
{
  "outcome": "incomplete",
  "summary": "Application logs were checked, but one telemetry source was unavailable.",
  "findings": []
}
```

Run `python Services/BugTriage/worker/audit.py complete --run <private run.json> --result <private result.json>`. The bridge saves the exact completion before sending it and records successful publication locally. Identical retries are safe; a prepared result cannot be replaced with another payload.

On 404, endpoints may not have been deployed: preserve the result and report this rollout limitation once while unchanged. On 409, stop publication: the result conflicts or retention has expired; do not invent another run or PR to bypass it. Other availability failures retain the prepared result for a later retry. Never claim that Admin was updated without a successful publication.

If collection fails after registration, publish `failed` or `incomplete` with the actual limitation. A workstation/app that never starts cannot send a failure; Admin marks coverage older than 36 hours and running audits older than two hours as stale.

Exports stay under `%USERPROFILE%\.codex\production-audit\<runId>`. The run file records a local expiry; the private bridge configuration's optional `auditRetentionDays` must match effective server retention (default 30 days). Remove only that owned run directory after publication when no longer needed and no later than its expiry. Before removal verify its absolute path is beneath the private audit root and its final directory name equals the run UUID. Do not clean the whole root. Remove expired pending results rather than republishing them; the bridge rejects expired files.

Deploy BugTriage's additive schema and runtime grants, the primary API and Admin UI before relying on publication. Historical Markdown import, cross-day incident grouping and GitHub PR-state synchronization are outside this version.
