# FoodDiary observability

The files in this directory are the source-controlled production baseline:

- `grafana/fooddiary-backend-reliability.json` is the backend reliability dashboard.
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
