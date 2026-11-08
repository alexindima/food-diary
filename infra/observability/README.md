# FoodDiary observability

The files in this directory are the source-controlled production baseline:

- `grafana/fooddiary-backend-reliability.json` is the backend reliability dashboard.
- `grafana/fooddiary-modules-overview.json` compares mediator operations by module
  and shows process-wide CPU, memory and GC context.
- `grafana/fooddiary-module-details.json` drills into one module's operations,
  outcomes, active work and latency, preserving the selected service/time range.
- `grafana/fooddiary-backend-alerting.yml` contains Grafana-managed rules routed
  through the production notification policy.
- `grafana/fooddiary-telegram-notifications.tmpl` formats production Telegram
  notifications in Russian, with severity, the specific condition, its last
  measurement and a public Grafana link.
- `prometheus/fooddiary-backend-alerts.yml` contains the first backend paging rules.
- `promtail-config.yaml` configures log shipping.
- `observability_report.py` produces a point-in-time host report.

Before enabling the rules, validate them with `promtool check rules` and make
sure Prometheus loads the file through `rule_files`. Import the dashboard into
the existing Grafana `Infrastructure` provider folder and select the production
Prometheus datasource.

The dashboard and alerts require the release that exports
`fooddiary_outbox_*` and `fooddiary_job_*` metrics. Absence of these series
before that release is expected; it must not be treated as a healthy zero.

## Telegram notifications

Use `{{ template "fooddiary.telegram.message" . }}` as the message of the existing
`fooddiary-telegram-alerts` receiver, with `parse_mode: HTML` and resolved
notifications enabled. Install the template as the `fooddiary.telegram`
notification template group. The production contact point is file-provisioned
in `/etc/grafana/provisioning/alerting/fooddiary-alerting.yml`; update its message
in place, preserving its token, chat ID, policies and rule groups. Provision the
template in a separate file in that same directory:

```yaml
apiVersion: 1
templates:
  - orgId: 1
    name: fooddiary.telegram
    template: |
      # Indent the complete contents of fooddiary-telegram-notifications.tmpl here.
```

Keep a restricted backup outside the provisioning directory and reload alerting
through `POST /api/admin/provisioning/alerting/reload`. A Grafana or application
restart is unnecessary. Before reload, preview the template using Grafana's
template test API, which renders without sending messages. After reload, verify
the stored template, receiver message/parse mode, enabled recovery notifications,
and unchanged rule evaluation and routing. Never export contact-point secrets
into repository files or verification logs. Roll back by restoring only the
receiver's prior message/parse mode and this template's prior provisioning file,
then reloading alerting; preserve unrelated changes made since the backup.

Active `warning` and `critical` alerts use 🟡 and 🔴. Confirmed resolved
conditions use ✅; a resolved notification with a state reason, missing measured
value, or a synthetic `DatasourceNoData`/`DatasourceError` uses ℹ️ without claiming
measured recovery. Zero is a valid value. Measurements are labelled as the last
measurement rather than a live reading. Clearing the CPU >70% rule does not
assert that the >60% warning is also clear. Clearing a dead-letter notification
does not assert that previously failed messages have been processed.

The template maps the eight current production alert titles to Russian text,
units and threshold descriptions. Keep those descriptions synchronized when
renaming rules or changing their thresholds/pending periods. Unknown alerts
fall back to their title and summary, with a neutral recovery description.
Dynamic text is HTML-escaped. Public links are built from the alert's Grafana
path, avoiding `localhost` links without changing the server's root URL.
Grouped notifications show at most three active and two resolved alerts and
explicitly report omitted entries to keep Telegram messages bounded. Recipients,
evaluation, grouping and repeat intervals belong to the existing configuration.

## Module operations

The outermost `ModuleTelemetryBehavior` covers mediator requests, including
validation, transaction completion and nested mediator calls, in API and any
JobManager paths that use the shared runtime. Direct service calls, notification
handlers, jobs outside mediator and HTTP output-cache hits are not counted.
Nested operations are counted independently; durations are inclusive wall time,
not additive CPU consumption. A module appears after its first observed operation.

The existing `FoodDiary.Application.Runtime` meter exports:

- `fooddiary_module_operations_total{module,operation,outcome}`: completed calls;
- `fooddiary_module_operation_duration_milliseconds_bucket{module,operation,le}`:
  latency histogram with 13 finite boundaries from 1 ms to 30 seconds;
- `fooddiary_module_active{module,operation}`: in-flight calls.

`module` comes from the owning `FoodDiary.Application.<Module>` assembly and
`operation` from the CLR request type name; unrecognized assemblies use `Other`.
Neither request values nor result/exception messages are recorded. Outcomes are
`success`, `failure` (returned failing Result), `exception`, and `cancelled`
(OperationCanceledException with the caller token cancelled). Business failures
are displayed separately from exceptions. Histogram labels intentionally omit
outcomes to limit series growth. Existing exporter/sampling settings are unchanged.

Provision both JSON files into the existing Infrastructure dashboard directory
(`/var/lib/grafana/dashboards` in production). Their stable UIDs are
`fooddiary-modules-overview` and `fooddiary-module-details`. Choose the Prometheus
datasource and service; the production collector exposes service identity as
`exported_job`. No Grafana restart or database migration is needed. Empty data
is not replaced with a healthy zero, and latency/rate panels need multiple export
and scrape intervals after deployment. Use 7/30-day ranges for long-term trends.

Production currently has only a metrics pipeline in the Collector. These
dashboards do not imply stored SQL/HTTP traces or per-module CPU/RAM attribution.
The existing backend reliability dashboard remains the source for job/outbox
metrics, which do not yet have uniform module ownership labels.

Before deployment, run application and architecture tests and validate dashboard
queries against Prometheus. After deployment, verify the three metric families,
bounded labels, histogram buckets and the two provisioned dashboard UIDs. Compare
process CPU, working set, GC and API latency against similar-load periods; a
quiet production smoke check alone is not an overhead benchmark. Roll back the
application release to stop instrumentation; remove only these two dashboard
files to roll back their provisioning, preserving other dashboards.

## Operational coverage

`host_collector.py` runs as the short-lived `fooddiary-metrics.service`, scheduled
by its timer about once per minute after completion. It atomically replaces
`/var/lib/prometheus/node-exporter/fooddiary.prom`. Node Exporter reads that file;
its HTTP listener remains on loopback. The service has a 128 MiB memory limit,
a 55-second deadline and a fixed inventory of service/database labels.

The collector observes Docker state/resources, four PostgreSQL servers, Redis
INFO counters, RabbitMQ queue totals in the application's default vhost, public
HTTP/TLS availability and the three existing scheduled PostgreSQL backups.
PostgreSQL probes use fixed aggregate SQL, read-only transactions and a three
second statement timeout through each container's existing local maintenance
identity. They never return SQL text, parameters, users, messages or row data.
RabbitMQ probes never read message contents. Redis probes never enumerate keys.
No database schema, authentication or application-container configuration changes
are required. The collector needs root/Docker access and is an operator service.

Each probe exports `fooddiary_collector_component_success`; a failed probe omits
its measurements. The separate last-run timestamp detects an abandoned/stuck
collector even while Node Exporter continues serving its previous file. Container
restarts are a snapshot gauge that resets on replacement; they are not a global
cumulative counter. Linux's OOM counter supplements the instantaneous container
OOM flag, which can clear after restart. CPU values are process time, memory is
working set with reclaimable inactive cache excluded, and unlimited containers
report the host memory limit supplied by Docker.

Backup verification checks the newest completed set's SHA256 manifest and size.
A corrupt newest dump never falls back to a healthy older set. This supplements
the scheduled backup script's `pg_restore --list` check; it does not claim that
a complete restore rehearsal succeeded. The age alert uses a 26-hour deadline.

`fooddiary-recording.yml` is the canonical user API recording-rule source. It
excludes health, client telemetry and internal Telegram bot-operation endpoints;
availability excludes 4xx responses. Successful observed traffic produces a zero
error ratio even before the first 5xx series exists; no eligible traffic remains
absent. Optional Prometheus paging files share these definitions. Production
notifications use the Grafana-managed rules, avoiding a second paging route.

Recurring-job deadlines use twice the configured schedule interval plus five
minutes. `JOB_INTERVALS` follows `RecurringJobsHostedService` and must be updated
when a production cron override changes. Missing first-run series get the same
startup grace, measured from the JobManager container start time. Daily jobs do
not become false failures immediately after a deployment. Existing job/outbox
gauges retain No Data as an alert condition rather than a fabricated zero.

New dashboards are **Operational Coverage**, **Frontend Reliability** and **AI
Provider Operations**. The Product Usage dashboard parses categories at query
time too, so old events without indexed category labels remain visible. It counts
observed navigation events, not unique users/sessions. Frontend latency/errors
can be grouped by logged build version; missing telemetry remains missing.

AI Responses requests expose `fooddiary.ai.request.duration` in milliseconds,
including retries and body reading, with bounded operation/model/outcome tags.
Validated provider usage exposes `fooddiary.ai.tokens` by input/output type.
Histogram advice covers 100 ms through 120 seconds. `ai-model-prices.json`
records the source/date and separate confirmed rates for the configured
`gpt-6-luna` and fallback `gpt-4o` models. The cost panel joins usage to each
model's own price; unknown models remain absent and have a separate unpriced
usage panel. Rates were verified on 7 November 2026 against
[official OpenAI pricing](https://developers.openai.com/api/docs/pricing).
The estimate uses Standard short-context, uncached input/output rates. Cache
discounts/writes, long-context, premium/regional adjustments, transcription,
token-count calls and missing usage are outside it. It is not a provider invoice.
Tests use mocked providers; no paid calls are required. The pricing fixture
checks model matching, unknown models and USD-per-million units independently.

### Independent availability

`.github/workflows/external-monitoring.yml` runs the same public probes from a
GitHub-hosted runner on the default branch. It checks the site, admin site,
Grafana health and the expected unauthorized JSON response on the public API.
Public readiness stays private at ingress. This verifies API transport/auth
response handling; PostgreSQL readiness is covered by the internal probes.
No test account or user data is required. State is cached between serialized
runs, with notification after two consecutive failures and measured recovery.
TLS expiry below 14 days is actionable immediately. Existing GitHub Telegram
secrets supply the receiver; successful checks send no routine messages.

The five-minute GitHub schedule is best-effort: scheduler delays or dropped runs
can extend detection time. Cache loss restarts the two-observation grace period.
The workflow remains independent of the VPS and can report a complete VPS outage.
The VPS dashboard shows its own additional probe measurements, not the remote
runner's results. Remote observations are retained as GitHub artifacts for seven
days. This public repository uses standard public-repository hosted runners.

### Validate, install and recover

Generate configuration with Python and `PyYAML==6.0.3`:

```text
python infra/observability/build_config.py
python -m unittest discover -s infra/observability -p 'test_*.py'
node infra/observability/verify.mjs
promtool check rules infra/observability/prometheus/fooddiary-recording.yml
promtool test rules infra/observability/prometheus/fooddiary-alert-tests.yml infra/observability/prometheus/fooddiary-coverage-tests.yml
promtool test rules infra/observability/prometheus/fooddiary-pricing-tests.yml
```

`verify.mjs --frontend-only` runs the focused parser regression; `--prometheus`
runs all PromQL fixtures with the pinned production-compatible Prometheus 3.3.0
image after Docker readiness. `--pricing-only` adds just the cost fixture to the
Python contracts. The wrapper lets the existing Wiki evidence runner capture
real Python/Docker executions without adding those runtimes to its shell parser.
`--slo-only` isolates the low-user-volume regression: heavy internal polling
must not satisfy either SLO alert's volume gate. The CI rule runner checks all
14 scenarios with the same pinned image and a bounded four-minute limit.
The long-window case still evaluates seven hours of traffic; short-window cases
stop after their asserted observation to avoid redundant historical evaluation.

Upload this directory to an owned staging directory on `fooddiary-prod`. Run
`python3 <staging>/install.py` to validate PromQL, Promtail syntax and dashboard
structure. Run the same command with `--apply` for the authorized rollout. The
installer preserves unrelated Prometheus jobs/rule files and the existing
Grafana contact-point/policy configuration. It adds self-monitoring scrape jobs
only for existing reachable monitoring endpoints. It restarts only Node Exporter,
Promtail and Grafana, and reloads Prometheus; application containers and databases
continue running. No trace pipeline or trace destination is enabled.

Every overwritten target has a private copy under
`/var/backups/fooddiary-observability/<UTC timestamp>/`; `manifest.json` records
the exact targets and which ones existed. To recover, stop the owned metrics
timer, restore only those recorded files (remove only newly created owned
targets), reload systemd, then reload/restart the same monitoring services.
Preserve unrelated configuration changes made after that backup. Keep the
backup private because existing operator configuration may contain credentials.
After rollout, verify all probe-success series, last-run age, Prometheus target
health, recording-rule health, Grafana rule evaluation, frontend historical
counts and application readiness separately.

Trace ingestion remains deliberately disabled by the user's decision on
7 November 2026. A future external backend must satisfy the existing trace
privacy, operator-access and maximum 14-day retention contract before ingestion.

### Telegram bot diagnostics

Promtail already collects the bot's stdout/stderr under
`compose_service="telegram-bot"`. Successful named-HttpClient poll noise is
filtered to Warning; application warnings/errors remain enabled. Intake and
ready-work discovery emit numeric summaries after their first completed poll,
then at most once per minute using monotonic time. Summaries contain a fixed
loop name, success/failure counts, mean duration and last-success Unix time.
They contain no message/photo payload, user/chat ID, token or signed URL.
Counters reset after each summary; missing summaries remain missing data.

`FoodDiary Telegram Operations` shows loop progress and selected safe diagnostic
templates. Intake duration includes Telegram long polling and persistence;
discovery duration is the API work-list request, not operation completion time.
Five minutes without a successful intake summary raises an alert after a further
two minutes. More than five operational failures over ten minutes raises a
warning after two minutes. An intentionally disabled discovery loop does not
trigger absence alerts. Install these rules only after the updated bot emits
its first successful intake heartbeat. Existing notification routing is retained.
