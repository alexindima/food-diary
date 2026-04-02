# E2E Smoke

This folder contains lightweight Playwright smoke tests for the admin app.

Goals:
- verify the admin shell starts
- verify auth redirect behavior
- verify core admin routes render with mocked API responses

Local run:

```bash
npm run test:e2e:admin:smoke
```

Interactive UI mode:

```bash
npm run test:e2e:admin:smoke:ui
```

Notes:
- the suite uses mocked HTTP responses and does not depend on a running backend
- the suite starts the Angular admin dev server on `http://127.0.0.1:4300`
- current CI is intentionally unchanged; add a dedicated job only after the smoke suite is stable in local/dev use
