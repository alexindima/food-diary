# Controlled browser scenarios

Use these modes when the requested behavior needs them; they do not turn a
focused layout check into an unrelated whole-application audit.

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
