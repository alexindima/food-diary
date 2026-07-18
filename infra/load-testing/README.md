# API Load-Test Baseline

This k6 scenario exercises the authenticated dashboard, product, recipe, and
consumption read paths. It is a release-regression baseline, not a capacity
claim for production.

Run it only against an isolated environment with disposable test data:

```powershell
docker run --rm -i `
  -e BASE_URL=http://host.docker.internal:8080 `
  -e TEST_EMAIL=load-test@example.test `
  -e TEST_PASSWORD='<secret>' `
  -e VUS=10 `
  grafana/k6 run - < api-baseline.js
```

Alternatively pass a short-lived access token through `AUTH_TOKEN`. Never
commit credentials or production tokens.

The default gate requires fewer than 1% failed requests, more than 99% passing
checks, global p95 below 750 ms, and endpoint-specific p95 budgets. Record the
environment, dataset size, commit SHA, k6 summary, database saturation, and
outbox age with each baseline run. Do not raise a threshold without recording
the query or infrastructure reason in `docs/backend/BACKEND_PERFORMANCE_REVIEW.md`.
