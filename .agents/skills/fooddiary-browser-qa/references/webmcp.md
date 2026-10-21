# Local WebMCP in browser QA

## Connect to the running application

1. Resolve the frontend URL, API and checkout from this task's runtime record.
   Use `node scripts/ai/task-runtime.mjs list` for discovery and
   [fooddiary-local-runtime](../../fooddiary-local-runtime/SKILL.md) when launch or
   recovery is needed. For admin QA, resolve the separate admin origin and finish
   its real sign-in/SSO flow with an Admin account.
2. Use a **Development build on `localhost`, `127.0.0.1` or `[::1]`**. The adapter
   requires `document.modelContext.registerTool` and an authenticated session.
   Production builds and public hosts intentionally register no FoodDiary tools.
   This is a page capability; no separate MCP server or Codex configuration entry
   is needed.
3. Read the current browser runtime documentation. In `mcp__cua_repl`, the built-in
   Browser entry point is `cua.createBrowserTab("iab", frontendUrl, { visible: true })`,
   where `frontendUrl` is the resolved owned URL. Use an existing owned tab when
   appropriate. Sign in through that tab's actual UI; a Playwright Chrome login
   does not establish this tab's session. Use the intended synthetic test account
   and keep credentials out of tool output and evidence.
4. After binding that tab as `tab`, discover Site tools through its WebMCP
   capability. Adapt this example to the current returned browser documentation:

   ```js
   const webmcp = await tab.capabilities.get("webmcp");
   let siteTools = await webmcp.fetchTools();
   nodeRepl.write(siteTools.description());
   ```

   Call only names and input schemas in this returned catalog. Fetch before the
   first WebMCP call on each tab in every turn, and fetch again after navigation,
   sign-in/session changes, tool-change notifications or stale handles. Replace
   the old `siteTools` object with the new result.

   The browser bridge can append a registration suffix, for example
   `fooddiary_read_screen_context__<registration-id>`. Use the **entire returned
   name**, including that suffix. The base names in the table below identify
   capabilities, not necessarily callable names. For the snippets, set
   `readContextName`, `readCapabilitiesName`, `searchCatalogName` and
   `fillMealDraftName` to their exact names from the current catalog. Refresh
   those name variables along with `siteTools`; registration IDs change after a
   full page reload. Refresh any stored name mappings too.
5. For the client, inspect the published state before preparing the scenario:

   ```js
   nodeRepl.write(await siteTools.call(readContextName, {}));
   nodeRepl.write(await siteTools.call(readCapabilitiesName, {}));
   ```

   Confirm the role, route, locale/timezone, configured API and current screen.
   Loading state requires waiting for the rendered outcome and reading again.
   A null screen or absent action is unavailable evidence, not permission to
   invent a capability. Admin uses its separate `fooddiary_admin_` catalog.

If discovery fails or returns no FoodDiary tools, check the actual origin/build,
completed authentication, selected client/admin tab and current browser support.
Re-fetch after resolving the cause. Record the observed reason (for example,
production mode, signed-out session, missing browser capability or unsupported
screen), then continue through the supported UI route. Do not inject a polyfill,
replace `document.modelContext`, call Angular internals or change production to
make a tool appear. A normal Playwright CLI session does not itself provide the
built-in Browser's Site tools bridge.

## Choose tools by the QA task

| Need | Use when listed | Still verify through the UI |
| --- | --- | --- |
| Diagnose the current page | `fooddiary_read_screen_context`, `fooddiary_read_capabilities` | Visible values, loading/error messages and relevant real network outcomes. |
| Reach a scenario's starting point | `fooddiary_open_diary_date`, `fooddiary_open_section`, `fooddiary_open_entity`, published filter/calendar tools | The date picker, filter or navigation control itself when that control is under test. |
| Prepare an editor | `fooddiary_search_catalog`, `fooddiary_fill_meal_draft`, `fooddiary_open_draft`, `fooddiary_fill_current_draft` | Input interaction, validation, save/cancel, and persistence after reopening. |
| Inspect local admin lists | Listed `fooddiary_admin_` context/navigation/filter tools | Real Admin SSO, permissions and the actual list controls. |
| Inspect push setup | `fooddiary_read_push_diagnostics` | Permission prompts and any real delivery; this tool does not establish full PWA install/update behavior. |

Read the current `draftSchema`, `filterFields`, actions and revision before filling,
filtering or discarding. Tools reuse owner state and may navigate or dirty a form;
they do not save/delete records or send provider requests. Mutations still follow
the task's authorization and fixture ownership. Dirty-editor protections are
expected: inspect the existing draft and use its normal UI, or explicitly discard
an owned unsaved draft with the current revision when that is part of the task.

Inspect every result: recoverable failures can return `{ok:false,error:"..."}`
without throwing. Resolve the reported condition before continuing. Missing build
identity, proxy upstream, server delivery state or unimplemented diagnostics stay
unknown. Keep WebMCP preparation, UI interaction and observed result distinct in
the verification evidence, including the tab used for each.

## Example: check a report that navigation loses an unsaved meal

Use an owned synthetic account and a known, uniquely named product with base unit
`G` in the task's local runtime. Reuse existing fixtures; a broad audit can prepare
and clean its owned fixture run as documented in `docs/ai/AI_DEVELOPMENT.md`.
If the catalog is empty, create one private product in this disposable runtime
through the normal UI; WebMCP can open and fill its clean editor using the
published product schema. Record that fixture and its cleanup. A product with
100 kcal per 100 g gives an independently expected 150 kcal for this example.
Here `ownedProductName` is that product's exact name and `qaDate` is the scenario's
literal local calendar date (`YYYY-MM-DD`). Neither value is a guessed catalog
entry or a UTC conversion.

1. Read the current context/capabilities and ensure a clean starting screen.
   Inspect the catalog result and confirm the exact accessible product and unit:

   ```js
   nodeRepl.write(await siteTools.call(searchCatalogName, {
     kind: "product", search: ownedProductName, page: 1
   }));
   ```

2. Prepare the unsaved editor in this same tab:

   ```js
   nodeRepl.write(await siteTools.call(fillMealDraftName, {
     date: qaDate,
     time: "12:30",
     mealType: "LUNCH",
     comment: "QA unsaved-draft check",
     items: [{ productName: ownedProductName, amount: 150, unit: "G" }]
   }));
   ```

   Inspect success, re-fetch tools after the editor navigation, then read fresh
   screen context and a DOM snapshot. Confirm the visible product, 150 g, date,
   local time and comment, and the published dirty state. Nothing is saved yet.
3. From the fresh DOM snapshot, use the **actual navigation control** to leave
   the editor. Check that the unsaved-changes dialog appears. Cancel leaving
   through its UI and check that the editor, values and dirty state remain.
   Capture the before/after UI and fresh WebMCP context in this same tab.
   Calling a WebMCP navigation tool instead would test its own dirty guard and
   would not verify the UI navigation regression.
4. If a defect reproduces and fixes are authorized, make the bounded fix and
   repeat that exact interaction. If Chrome is the target browser, independently
   prepare and repeat the UI scenario there; the helper draft is tab-local.
5. Discard only this test's unsaved draft through the normal UI. Confirm no meal
   was created and leave the account clean. Record separate results for tool
   preparation, the real navigation/cancel behavior, any Chrome recheck and cleanup.

For current schemas and supported screens, consult
[`docs/ai/AI_DEVELOPMENT.md`](../../../../docs/ai/AI_DEVELOPMENT.md), then verify
the relevant client adapter under `src/app/composition/webmcp` or admin adapter
under `projects/fooddiary-admin/src/app/shared/development`, relative to
`FoodDiary.Web.Client`. The live fetched catalog determines what this tab exposes.
