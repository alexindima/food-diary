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
- current CI is intentionally unchanged; add a dedicated job only after the smoke suite is stable in local/dev use
