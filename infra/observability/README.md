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
