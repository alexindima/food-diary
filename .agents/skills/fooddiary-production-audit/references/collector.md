# Reusable Grafana collection

Import `scripts/collect_grafana.py` into the existing authenticated request
process. Pass its trusted-origin GET-JSON function as `get_json(path, params)`;
retain its status checks, timeout and redirect rejection. Credentials stay in
that process. The module neither reads secrets nor changes Grafana configuration.

Call `collect_audit` using the original audit `run.json` interval, a selector and
metric expressions verified against current baselines, and known service names
whose labels may appear in sanitized output. Unknown service names aggregate
under `other`; arbitrary labels and raw lines are discarded. Do not persist the
datasource response or authentication wrapper.

The result records datasource discovery, requested half-open time slices, limits,
saturation, query failures and request-budget gaps. Saturated parent results are
replaced by child slices, preserving repeated events inside a response. A saturated
single timestamp stays incomplete. Prometheus results are labelled instant
snapshots; they do not establish interval health. `collectionComplete` covers the
configured collector plan, not host correlation or incident validation.

Save its sanitized output with the audit's existing run artifacts. Retain the same
run ID/window after an interruption and distinguish collection retries from
publication retries. Compare deployed revision and current resource hashes before
classifying historical PWA failures; an untested device installation remains
untested even when current hashes match.

For the full repeated collection entrypoint, import `run_audit` from
`scripts/run_audit.py` in this skill, in the same authenticated process:

```python
result = run_audit(get_json, original_run_path, {
    "selector": verified_selector,
    "service_names": verified_service_names,
    "metrics": verified_metric_expressions,
})
```

It reads the original run UUID/window, delegates bounded collection and returns
`collection` plus a report whose service/level totals come directly from the
aggregates. Save those sanitized results in the existing run's artifact directory;
the helper neither publishes nor reads credentials. Supply the current verified
transport; do not copy a credential-bearing wrapper from an old temporary script.
The generated report is a numerical starting point. Add deployed-version,
host/interval correlation and reasoned incident conclusions separately.

Read only the current active block of automation memory first: last run/window,
open signals, pending publication and report path. Keep completed empty runs in
a separate history instead of appending their full text to the active block.
Retain an unpublished result and its original run for identical publication retry.

Run `python -m unittest discover -s .agents/skills/fooddiary-production-audit/scripts -p test_*.py`.
Fixtures cover saturation, repeated rows, privacy, budgets, missing sources and
snapshot boundaries. They do not establish access to production.

API references: [Grafana datasource proxy](https://grafana.com/docs/grafana/latest/developer-resources/api-reference/http-api/api-legacy/data_source/#data-source-proxy-calls),
[Loki query range](https://grafana.com/docs/loki/latest/reference/loki-http-api/#query-logs-within-a-range-of-time),
[Prometheus instant query](https://prometheus.io/docs/prometheus/latest/querying/api/#instant-queries).
