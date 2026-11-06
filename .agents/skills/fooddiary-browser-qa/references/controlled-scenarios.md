# Controlled browser scenarios

Use these modes when the requested behavior needs them; they do not turn a
focused layout check into an unrelated whole-application audit.

For repeated local checks, use `scripts/ai/controlled-browser-scenarios.mjs`
from the repository root. Pass the existing Playwright `browser`, local `baseURL`,
a fresh task-owned output directory below `.artifacts`, and scenarios with `id`,
optional `prepare(page, inputs)` and required `exercise(page, inputs)` callbacks.
The callbacks own current SDK/network fixtures, login and actual UI assertions;
the runner does not invent a component harness or authenticate an account.
By default it executes RU/EN at 390x844 and 1440x1000, captures successful
screenshots and bounded WebMCP metadata, writes `report.json` after each case,
and throws if a case fails. Choose only the locales/viewports the task needs.

Import it from a task-owned script using an absolute file URL or a resolved
repository path. Supply Playwright from the frontend's installed package or an
existing repository test runner; the helper itself has no Playwright dependency,
so root/worktree ESM package discovery does not need a temporary dependency copy.
It closes each context it creates and leaves the supplied browser untouched.
Screenshots require synthetic, non-sensitive page content. A unit test of this
orchestrator does not prove any FoodDiary journey; inspect the real run artifacts.

For Angular evidence, run the owning `test:ci:app|admin|ui-kit|tour|webmcp`
script with `-- --include=<workspace-relative.spec.ts>` directly through
`evidence-run`; `npm --prefix FoodDiary.Web.Client run ...` and the existing
`cd FoodDiary.Web.Client && npm run ...` form are supported. Playwright smoke
scripts accept their explicit `e2e/...spec.ts` path and bounded project/worker
flags. Lint changed test code before recording red/green evidence; changing the
test afterward invalidates the pair. Required policy checks keep their exact
canonical command, and focused runs remain supplemental.

For a component-only visual change, reuse its real Storybook story, styles and
translations. Check the native file chooser, keyboard/focus and narrow viewport
when relevant. A Storybook without application composition may have no WebMCP;
record that boundary. It does not establish backend integration.

For auth and realtime races, use typed SDK/SignalR fixtures matching current
contracts. Count unique connection IDs, not duplicate close callbacks. Verify
teardown and absence of reconnect. For account switching, distinguish the two
tab contexts and exercise the relevant remember-me, delayed logout/refresh and
stale response cases. A unit fixture and a real multi-tab pass prove different
boundaries; keep their evidence separate.

For a first-frame or reload issue, use the actual production build through the
current Nginx configuration and control the auth-restoration delay. Observe the
first visible content and final route at the affected paths. Record whether the
service worker is absent, current or from an earlier release; a clean tab alone
does not establish an old installed PWA updates correctly.

Keep helpers for authentication and result parsing task-owned. Parse structured
outputs with their actual line endings, redact tokens from console/WebSocket
logs, and verify API identity independently from frontend captures. Never save
credentials in a reusable helper or export them in screenshots.
