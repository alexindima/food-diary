# E2E Smoke

This folder contains lightweight Playwright smoke tests for the client and admin apps.

Goals:

- verify the public client shell starts
- verify client auth and dashboard entry flows
- verify the admin shell starts
- verify auth redirect behavior
- verify core admin routes render with mocked API responses

Local run:

```bash
npm run test:e2e:admin:smoke
npm run test:e2e:client:smoke
npm run test:e2e:client:network-audit
```

Interactive UI mode:

```bash
npm run test:e2e:admin:smoke:ui
npm run test:e2e:client:smoke:ui
```

Notes:

- the suites use mocked HTTP responses and do not depend on a running backend
- admin smoke starts the Angular admin dev server on `http://127.0.0.1:4300`
- client smoke starts the Angular client dev server on `http://127.0.0.1:4201`
- CI runs both smoke suites after the corresponding unit tests and builds
- deterministic API fixtures cover public, authenticated user, admin, meal-plan detail, and lesson-detail states
- the client network audit covers authenticated routes separately from smoke CI, prints and attaches a route-to-endpoint table and `network-audit.json`, and waits for the intended routed view plus an idle API window before measuring. It rejects redirects, runtime errors, duplicate identical GET requests, transport failures (status 0), and HTTP status 400 and above. The default initial-load budget is 8 requests. Reviewed exceptions: profile uses 9 for overview, latest measurements, billing and sessions plus shell requests; the dietologist client dashboard uses 10 for client selection, dashboard, goals, recommendations, tasks and templates plus shell requests. These limits include the four shared shell requests and must be reviewed when adding endpoints.
