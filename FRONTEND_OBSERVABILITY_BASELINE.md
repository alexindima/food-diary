# Frontend Observability Baseline

Date: 2026-04-03
Scope: SPA client observability baseline

## Current Collection Path

Frontend telemetry is sent to:

- `POST /api/v1/logs`

The initial payload categories are:

- `client_error`
- `route_timing`
- `http_request`
- `web_vital`

The baseline is enabled through frontend environment config:

- local: `enableClientObservability: false`
- staging/prod: `enableClientObservability: true`

## What Is Collected

### Client exceptions

- unhandled global Angular errors through `GlobalErrorHandler`
- message
- stack when available
- current location
- build version

### Route timings

- router navigation duration
- outcome: `success`, `cancelled`, or `error`
- resolved route

### API request telemetry

- HTTP method
- normalized API path
- status code
- duration
- outcome: `success`, `client_error`, `server_error`, `network_error`

### Web vitals baseline

Initial baseline:

- `ttfb`
- `fcp`
- `lcp`

This is intentionally small. It is meant to provide an initial production signal without adding a noisy client analytics layer.

## Operational Use

Use this baseline to answer:

- are client crashes happening after a deploy?
- did route transitions become materially slower?
- did API failures spike from the browser perspective even when backend availability looks normal?
- did core page-load vitals regress after a frontend rollout?

## Next Likely Follow-Ups

- add admin-app coverage to the same telemetry path
- aggregate client events into dedicated backend metrics if volume justifies it
- add CLS or INP only if the simpler baseline proves useful and stable
