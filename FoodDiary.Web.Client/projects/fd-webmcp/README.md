# fd-webmcp

Shared Angular workspace library for local browser WebMCP infrastructure.
The client and admin import only `fd-webmcp`; no application code is imported
by the library. Like `fd-tour`, the package supplies the mechanism while each
application owns its scenarios.

The public entry point exports:

- `localWebmcpContext`: detect native browser support on exact loopback hosts in development mode.
- `registerWebmcpTools`: register tools with abortable, disposable session ownership; clean up partial failures.
- `WebmcpExecutor`: serialize mutations, permit concurrent context reads, check owner sessions before and after execution, and apply the owner's safe error policy.
- `WebmcpScreen`, `WebmcpScreenHost`, `connectWebmcpScreen`: optional page/dialog capabilities tied to their Angular destroy scope.
- `WebmcpScreenRegistry`: maintain a page/dialog stack with dirty-state, revision and explicit discard protections.
- `WebmcpTool`, `WebmcpOperation`, `WebmcpModelContext`, `WebmcpError`: protocol and safe-error contracts.

Create an executor and screen registry per application adapter. Call browser
registration only after browser rendering, using injected `DOCUMENT` and
`isDevMode()`. Bind the returned disposer to the authenticated session's cleanup.
Registration has no import-time side effects and installs no polyfill or server.

Protocol contracts use the pinned official `webmcp-types` definitions. Browser
registration is removed by aborting its registration signal; the current API
has no `unregisterTool` method. Execution forwards the browser's cancellation
signal to the operation and rejects cancelled work before returning a result.
The mutation lock remains held until underlying work settles. Cancellation is
cooperative and cannot roll back a UI action already accepted by the router or
another owner; asynchronous operation implementations should observe the supplied
signal before subsequent effects. Ordinary failures still use the consuming
application's bounded error policy. Local tools declare the debugging annotation.

Applications retain tool names/schemas, routes, authentication and role policies,
API clients, metadata allowlists/redaction, and all domain calculations. Published
capabilities prepare unsaved forms; the library has no persistence or permission
API. Local CLI fixture/mail diagnostics and the .NET development MCP server remain
separate tools.

From `FoodDiary.Web.Client`:

```shell
npm run build:webmcp
npm run test:ci:webmcp
npm run test:coverage:webmcp
```

The build/tests are included in `npm run verify`, pre-push and CI. ESLint and the
dependency graph enforce independence from application and UI code; consumers
must use the public package entry point.
