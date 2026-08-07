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
- the client network audit covers authenticated routes separately from smoke CI, prints and attaches a route-to-endpoint table, attaches `network-audit.json`, and fails on duplicate identical GET requests, more than 8 API requests per initial route load, or API responses with status 400 and above
