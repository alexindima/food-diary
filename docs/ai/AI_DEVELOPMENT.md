# FoodDiary AI development tools

The generated SDK, semantic types, architectural checks and Wiki remain the foundation.
Start discovery at `.llm-wiki/index.md`, then confirm facts in the owning sources.
Use `develop -> next -> phase-next -> validate -> handoff`; the existing verification
planner selects, orders and consolidates final checks. These tools add executable
fixtures, generation, local runtime isolation and coding outcome evaluation.

## Contract fixtures

`FoodDiary.Web.Client/src/testing/api-fixtures.ts` contains native wire DTO builders
shared by unit and browser tests. Application adapters still decode semantic values.
`e2e/support/openapi-fixtures.mjs` validates against the same checked-in OpenAPI used
for the SDK, including references, nullable values, formats and response status.
It never coerces input, assigns defaults or repairs fixtures.

Use `jsonFixture(contract, method, url, body, status)` when fulfilling an API route.
An unknown method/path/status fails. A no-content response must have no JSON body.
Malformed response tests use `invalidJsonFixture(body, reason, status)` explicitly.
The default authenticated smoke registry refuses unknown operations and mutations;
its complete response set is checked offline before Playwright starts.

```powershell
cd FoodDiary.Web.Client
npm run test:fixtures
npm run test:e2e:client:smoke
```

OpenAPI only guarantees the declared wire shape. Business state, sequences,
partial failures and intentionally supported legacy payloads need explicit scenarios.
Optional DTO fields remain optional; the validator does not invent required fields.

## Application patterns

For recognition-quality references, database recovery drills, repeated load
measurements, live user journeys and alert checks, follow
[Reliability verification](RELIABILITY_VERIFICATION.md). The report distinguishes
local execution, replayed model output and production/provider evidence.

Follow [Application patterns](../architecture/APPLICATION_PATTERNS.md) when a
feature needs pure rules, validated result variants, lifecycle transitions or
external semantic decoding. Durable food recognition applies all five approaches
with a [rule and example matrix](../backend/AI_RECOGNITION_JOBS.md#owner-rules-and-executable-examples).
Its implementation preserves existing HTTP/storage contracts and paid-call
recovery rules. These are repeatable design examples, not evidence of a measured
model productivity improvement.

## Behavioral verification

The Recipes Domain suite includes FsCheck properties (300 generated examples per
property, with shrinking and replay information on failure) for ingredient
splitting, equal product/nested-recipe bases, unavailable sources and manual
zero overrides. Fixed witnesses protect midpoint-to-even rounding, partial
nutrition and nonpositive/absent bases. Inputs preserve the owning legacy policy;
generated tests do not impose new validation or historical recalculation rules.

```powershell
dotnet test Modules/Recipes/tests/FoodDiary.Modules.Recipes.Domain.Tests/FoodDiary.Modules.Recipes.Domain.Tests.csproj --artifacts-path .artifacts/nutrition-tests --filter FullyQualifiedName~RecipeNutrition
dotnet tool restore
node scripts/ai/verify-nutrition-mutation.mjs
node scripts/ai/verify-user-journeys.mjs
# Reuse an already ready, current task runtime without stopping it:
node scripts/ai/verify-user-journeys.mjs --runtime my-change
```

The pinned Stryker tool mutates only RecipeNutritionPolicy and
RecipeNutritionIngredient, fails immediately when baseline tests fail, and uses
two test sessions. The explicit net10.0 target prevents Windows Buildalyzer from
selecting an older Visual Studio MSBuild for inherited project settings. Build
outputs and reports remain under repository `.artifacts/ai-verification`.
Review surviving mutants rather than suppressing them to increase a score.

Live journeys type-check fixtures against generated SDK requests, then use the
owned PostgreSQL/API/frontend proxy with no intercepted HTTP. They verify a
dashboard hydration write across reload and a meal quantity edit with independently
derived nutrition across save/read/reopen. A recipe case verifies gram-to-serving
decoding, stored meal totals and the existing rejection of edits to used recipes.
Credentials stay in the ignored runtime
directory and browser memory; traces and login screenshots are disabled. A fresh
runtime is stopped on completion; a supplied runtime remains available. These
scenarios do not verify external AI, email, payments or background providers.

## Feature generators

Generation defaults to a dry-run JSON plan. The owning project/feature and SDK
operation must already exist. It rejects path escapes, symlinks and overwrites.
No project references, routes, provider bindings or meaningless tests are generated.

```powershell
node scripts/ai/generate-feature.mjs backend --module Hydration --name InspectWater --kind query --response int
node scripts/ai/generate-feature.mjs frontend --feature hydration --name WaterProbe --sdk hydration --operation getHydrationDaily
```

Add `--apply` to write the reviewed plan. Backend slices include the request,
validator and handler in the owning namespace. The handler explicitly rejects use
until its authorization and owner capabilities are implemented. Frontend generation
uses actual generated request/response types and the existing request-state controller.
Its facade is local and requires a route/page provider. Decode wire DTOs in the
owning adapter before exposing domain values to UI state.

```powershell
node --test scripts/ai/generate-feature.test.mjs scripts/ai/task-runtime.test.mjs
node scripts/ai/verify-backend-generators.mjs
```

These checks compile the real frontend SDK adapter/facade and both backend slice
kinds, and exercise refusal of collisions and path escapes. They do not establish
the behavior of a newly generated business feature.

## Local WebMCP pilot

Add the admin frontend to an existing owned runtime without changing the shared
Angular workspace or its usual development URLs:

```powershell
node scripts/ai/task-runtime.mjs admin --name my-change
node scripts/ai/runtime-diagnostics.mjs mail my-change
```

The admin command builds both apps in the runtime's generated workspace, starts
admin on a separate loopback origin, and restarts only that runtime's client with
the matching Admin panel destination. It retains the API and database. Process
creation identities protect other sessions; the normal runtime stop also stops
admin. `admin.status: frontend-ready` confirms HTTP readiness only;
`admin.browserSso: unverified` requires a separate real sign-in check.

Mail diagnostics issue only `GET /health` to the owned provider port after
checking its process identity. A matching runtime ID and disabled-provider response
confirm the local stub. They report background-job configuration from the startup
record, keep application outbox state unobserved, and never claim SMTP or end-to-end
delivery. No emails are enqueued or sent by this check. A dead/reused PID, unexpected
provider response or another task's health response cannot establish readiness.

Development builds on `localhost`, `127.0.0.1` and `[::1]` expose fourteen client
WebMCP tools after sign-in when the browser provides
`document.modelContext.registerTool`. Production mode, public hosts and
unsupported browsers do not register tools. Normal UI operation is preserved.
Use the built-in Codex browser's Site tools to discover and invoke them.

- `fooddiary_read_screen_context`: current route and explicitly published
  page/dialog state, role and email-verification flags, locale/timezone,
  configured API routing, and recent request resource/status summaries. Diary state includes selected date boundaries,
  loading/error state and visible meal count. Editor state includes local
  date/time, type, comment, selected items, calories and validation/dirty state.
- `fooddiary_open_diary_date`: accepts `{ "date": "2026-10-17" }`; navigates
  to `/meals?dateFrom=2026-10-17&dateTo=2026-10-17` through the existing router.
- `fooddiary_fill_meal_draft`: accepts the following shape, resolves exact
  existing catalog names through the normal authenticated API, and fills the
  actual new-meal editor. Recipe rows instead accept `{ "recipeId": "<exact accessible ID>", "servings": 0.5 }`.
  The meal owner resolves serving mass and fills its existing display quantity;
  unknown mass keeps servings. It never submits the meal or calls AI providers.

The expanded tools reuse the current owning feature state:

| Tool                            | Supported preparation or inspection                                                                                                                                                                            |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `fooddiary_open_section`        | Allowlisted user sections; normal route guards apply.                                                                                                                                                          |
| `fooddiary_select_client`       | Existing client dashboard, using the signed-in dietologist's permissions.                                                                                                                                      |
| `fooddiary_open_entity`         | Existing ID in the current diary, product, recipe or client list. Product/recipe/meal opens the existing editor without modifying it.                                                                          |
| `fooddiary_set_screen_filters`  | Replace published diary/product/recipe filters through their owning URL decoders, or client dashboard calendar filters. Read `filterFields` first.                                                             |
| `fooddiary_set_calendar_period` | Inclusive diary/client calendar range; explicit timezone must match the browser.                                                                                                                               |
| `fooddiary_search_catalog`      | Existing products/recipes, IDs, units, basis/portion amounts and nutrition. No external provider search.                                                                                                       |
| `fooddiary_open_draft`          | New product/recipe editor, weight/waist measurement dialog, existing cycle settings drawer, or fasting setup (`kind: "fasting"`). |
| `fooddiary_fill_current_draft`  | Fill the current clean editor using its published `kind` and `draftSchema`. Cycle settings require the current `expectedRevision` and accept only three bounded integer durations.                                                                 |
| `fooddiary_clear_current_draft` | Discard an unsaved draft with `confirmDiscard: true` and the current screen `revision`. Cycle settings reset the three durations only when other settings are unchanged. Saved entity editors cannot be cleared. |
| `fooddiary_read_capabilities`   | Current available actions, draft schema and limitations. Unsupported screens publish no invented capability.                                                                                                   |
| `fooddiary_read_push_diagnostics` | Read-only browser permission, worker activity/controller, browser subscription presence and nullable expiration, provider availability, server device count/expirations. No endpoints, keys, permission requests, subscriptions or sends. |

Statistics publishes selected and applied periods, range, timezone, quantization,
units and the owning dashboard-card aggregates, including recorded-day means.
Aggregates become unavailable during a replacement load or error. Set its
`range`, `dateFrom`, `dateTo` and `nutritionTab` through `set_screen_filters`;
`set_calendar_period` also supports statistics. Dashboard publishes its selected
date and applied period; its `date` filter uses the same router helper as the UI.

Statistics, dashboard and product/recipe lists publish `provenance.queryRevision`,
`lastAppliedQueryRevision` and `lastAppliedRequestId`. The latter binds the
interceptor's actual HTTP record through HttpContext, only when the owner accepts
the response. A stale HTTP 200 cannot replace that evidence; a failed refresh
retains the previously applied ID. Missing development observations stay null.
No correlation value is added to headers, queries or API payloads.

Fasting context contains session timing, phase/occurrence and durations, excluding
check-in notes and symptoms. `open_draft` opens the ordinary setup dialog. Use its
current outer `expectedRevision` with `fill_current_draft`, kind `fasting-setup`.
Its draft accepts `mode` and integer `fastHours`; cyclic mode additionally requires
`fastDays`/`eatDays`, for example 1:2. Bounds come from the owner factories. Start,
confirmation, session changes and completion remain UI actions.

Recipe ingredients accept exactly one product source (`productId`, base-unit
`amount`, `unit`) or nested recipe source (`recipeId`, serving `amount`,
`unit: "servings"`). IDs resolve through normal authenticated catalog access before
the clean form changes. Existing partial nutrition and serving rules still apply.

### Owned audit fixtures and delivery observations

```powershell
node scripts/ai/owned-fixtures.mjs create --name my-change
node scripts/ai/owned-fixtures.mjs verify --name my-change --run <run-UUID>
node scripts/ai/owned-fixtures.mjs cleanup --name my-change --run <run-UUID>
node scripts/ai/mail-delivery-trace.mjs --name my-change --outbox-id <UUID>
```

These commands require a current, ready owned local runtime. Fixtures create two
synthetic accounts, a client relationship, product/image, nested recipes and 25
recorded days (3850 kcal total, 154 kcal per recorded day). The durable manifest
under the runtime's `fixtures/<run-UUID>/` records IDs, object keys and independent
numeric expectations; credentials are separate and never printed. Idempotency
keys and completed steps survive interrupted creation. Cleanup uses the existing
Users purge participants for the complete explicit synthetic-account set, then
drains only those image-deletion message IDs through the normal lease engine.
The one-shot API composition requires Development, matching runtime/manifest paths,
checkout and loopback database port; it creates no HTTP endpoint. Read-only database
reconciliation covers UUID references, attribution and queue/receipt payloads;
authenticated storage observations cover both buckets. Unknown storage state,
remaining data or pending/dead-lettered deletion work prevents cleanup success.
Processed image-deletion receipts are reported separately as retained history.
Before deleting any account, cleanup seals the observed manifest identity set with
an HMAC using the owned runtime's existing local credential. A changed manifest or
an unsealed account that has already disappeared is refused. After owner purge,
disposable-runtime maintenance removes only that sealed run's residual email,
achievement evaluation and audit records in one locked transaction. Mixed recipients,
foreign audit actors/subjects/targets and active queue leases are refused. This CLI
maintenance does not change production account-deletion or audit-retention policy.

Mail tracing observes the exact application outbox row and a unique correlated
relay journal entry. Optional `--inbox-id` and `--bug-id` add bounded metadata
observations; BugTriage's source ID is checked against the selected inbox ID.
The report excludes bodies, subjects, recipients, credentials and links. Inbox
presence does not establish outbound correlation, relay Sent does not establish
independent SMTP acceptance, and unavailable stages remain `unknown`. Disabled
providers cannot prove delivery. Real SMTP/push delivery still requires a separately
observed receiving endpoint or visible browser notification.

Client report ranges pass literal calendar labels to the existing dashboard API.
That API uses the client's saved timezone, falling back to UTC. The context's
`application.timeZone` identifies the browser for input validation and meal-time
display; it does not establish the client's report timezone.

The client dashboard context publishes `meals` with permission state, loaded
IDs/UTC instants, total, loaded count/page, `hasNext`, loading and load-more errors.
Rows are hidden when meal sharing is denied or a replacement period is loading.
Loading another page remains the normal UI button operation, so audits can compare
the context before and after a real click and check duplicates and ordering.

`cycle-tracking` is an allowlisted section. With an existing cycle,
`open_draft({kind:"cycle-settings"})` opens the actual settings drawer. Read its
revision, duration bounds, dirty/busy state and field validation before calling
`fill_current_draft({kind:"cycle-settings",expectedRevision:revision,draft:{averageCycleLength:28,averagePeriodLength:5,lutealLength:14}})`.
Only those three integer fields are accepted. A stale revision, dirty form, busy
operation, invalid range or extra field is refused before any change. The context
omits observations, notes and consent values. Tools never save these settings or
change consent; review and saving use the normal interface.

Request diagnostics retain resource/status and allowlisted route
templates such as `GET /api/v{version}/cycles/{id}`. IDs, query values, request
bodies and arbitrary response headers are omitted. Unknown operations stay null.
Two Development-only API response headers identify the startup binary set
(`X-FoodDiary-Api-Build`, SHA-256 over deployed FoodDiary module identities) and
process instance (`X-FoodDiary-Api-Instance`). They apply only to API requests from
a loopback remote address. Production and non-loopback requests omit them.
The frontend reports validated identities as response observations associated
with the observed request origin; proxy upstream identity is not inferred.

Owned task workspaces emit a separate `frontendBuild.sourceCapture`: checkout
revision, source/configuration fingerprint and UTC capture time. A task-owned
watcher refreshes the emitted module after frontend source changes so normal HMR
also refreshes the capture. Tests and generated output are excluded. A capture
identifies the frontend source snapshot, not the running API. Other launchers
without emitted metadata report `not-emitted`; absent API headers remain unknown.
Re-running `task-runtime.mjs admin --name ...` upgrades an existing owned workspace
and starts its identity watcher without replacing its API or database.

Product nutrients use 100 G/ML or 1 PCS; optional omitted macros default to zero.
Recipe drafts resolve `productId`, `amount` and exact `unit` for each step ingredient
through the authenticated product adapter. Nutrition is calculated by the real recipe
form. Recipe ingredients support products and accessible nested recipes; photos remain
normal UI operations. Measurement input uses the exact displayed `kg`/`lb` or `cm`/`in`
unit reported by the dialog. No implicit unit conversion is performed by a tool.

For example, after `open_draft({kind:"product"})`, read context and then call:

```json
{
    "kind": "product-editor",
    "draft": {
        "name": "Local draft",
        "baseUnit": "G",
        "defaultPortionAmount": 150,
        "caloriesPerBase": 52
    }
}
```

The separate admin app exposes four local tools with the `fooddiary_admin_`
prefix: `read_screen_context`, `open_section`, `set_screen_filters`, and
`open_entity`. They require an authenticated Admin session, use the admin app's
own contracts, and publish list state for Users, MailInbox and Outgoing emails.
Only Users publishes record opening. Mail tools expose IDs, delivery status,
attempt/count/date information and filters; message bodies and confirmation links
are excluded, and tools do not mark messages read or send mail. Admin calendar
filters retain their existing UTC period semantics. Navigation from an admin
screen without a published unsaved-change contract is refused; use the UI there.

```json
{
    "date": "2026-10-17",
    "time": "12:30",
    "mealType": "LUNCH",
    "comment": "Local pilot draft",
    "items": [
        {
            "productName": "Exact existing product name",
            "amount": 150,
            "unit": "G"
        }
    ]
}
```

Dates are local calendar dates and times are local wall-clock values. Invalid
calendar days and nonexistent local times are rejected. Product amounts use
the product's existing base unit (`G`, `ML` or `PCS`); the pilot performs no unit
conversion for products; recipe serving conversion belongs to the existing meal owner. Missing/ambiguous names, mismatched
units and stale sessions/screens fail before editor changes. Commands that change
a screen are serialized; read-only inspection remains available while a command
waits for API data. Clean published measurement dialogs close through their normal
handler before navigation; other open dialogs must be closed through the UI.
Both navigation and draft replacement refuse an already dirty published editor,
including a dialog over an underlying page. Review and save through the normal interface;
explicit revision-checked discard is also available. Existing editor
validation, nutrition calculation and unsaved-change protection remain in use.
Recoverable tool failures return `{ "ok": false, "error": "..." }` with a safe
reason, because browser bridges can hide the messages of thrown exceptions.
Navigation waits for Angular to settle before returning the screen context;
opening the same diary date again is idempotent.

Tools are removed on session cleanup. The adapter holds only the current
page/dialog capabilities and does not persist tool context or return credentials.
Request diagnostics omit IDs, query values, headers, bodies and auth resources;
they report the origin actually visible to browser HTTP. A same-origin proxy does
not reveal its upstream API. `buildRevision: null` means no revision was emitted by
the frontend. Use the owned task runtime record for checkout/startup SHA, upstream
ports and provider readiness. Browser list state cannot prove server outbox,
worker, SMTP or end-to-end delivery health; the corresponding audit P0 operations
remain runtime diagnostics, not claimed WebMCP coverage.
Reading during a diary load reports `loading: true`; inspect the rendered result
and read again after loading. Tools accelerate scenario preparation and state
inspection; they do not exercise buttons, keyboard interaction or visual layout.

The shared mechanism is the `fd-webmcp` Angular library under
`FoodDiary.Web.Client/projects/fd-webmcp`, with an explicit `src/public-api.ts`.
It owns native-browser detection, abortable tool registration, session-checked
execution and screen contracts/registry. It has no client/admin, UI, API SDK,
authentication, route or domain dependencies. Build with `npm run build:webmcp`
and test with `npm run test:ci:webmcp`; full verification, pre-push and CI include
both targets.

The client adapter remains under `src/app/composition/webmcp` and consumes the
optional `features/meals/contracts/meal-development-tools` capability and
`shared/development` capabilities published by each live owner. Admin keeps its
adapter under its own `shared/development`. Both import the library through
`fd-webmcp`; each still owns its tool names/schemas, routes, authentication checks,
API metadata/redaction and scenarios. Existing feature capability imports remain
compatible. Browsers without native WebMCP need no polyfill or separate MCP server.

In one local comparison at 1280 x 720, opening 16 October 2026 and preparing
an unsaved lunch at 12:30 with a comment and 150 g of a known product took
20 successful UI actions versus two WebMCP preparation calls. A separate
context read verified the draft. Both paths displayed 78 kcal for the fixture's
52 kcal per 100 g. Login, fixture creation, observations and cleanup are excluded;
this single scenario measures interaction count, not overall speed or test coverage.

## Owned task runtime

```powershell
./scripts/Start-FoodDiaryTask.ps1 -TaskName my-change
node scripts/ai/task-runtime.mjs status --name my-change
./scripts/Stop-FoodDiaryTask.ps1 -TaskName my-change
```

The launcher creates separate PostgreSQL, Redis and S3-compatible containers,
loopback ports, migrations, a synthetic account and build output under
`.artifacts/task-runtime/<name>`. It runs the real API and Angular frontend with a
task-owned proxy. A task profile uses relative API URLs; normal profiles retain
their existing behavior. It verifies login, a protected API request and the same
request through the frontend proxy before reporting readiness. The rendered
browser probe signs in through that proxy, restores its real HttpOnly refresh
cookie session, and verifies the owned profile and visible dashboard. Account
policy comes from the server response during session restoration.

`runtime.json` records checkout, HEAD, source fingerprint, URLs, owned container
IDs, process lifetime identities, integration status and the stop command. Generated
credentials/configuration remain in ignored local files; neither their values nor
tokens appear in the runtime record. The host and initializer load the dedicated
configuration only in Development, replacing inherited configuration providers.
Provider HTTP is limited to loopback with redirects disabled. AI, mail, payments
and background jobs are explicitly disabled; the local mail-provider stub returns
501 rather than claiming delivery. Storage is local, with separate public and
staging buckets. Enabling an additional provider needs an explicit owned fixture.

Failed preparation cleans up its owned resources. Stop checks the checkout,
container ownership labels and process lifetime before acting. It never stops
another checkout's server or removes shared data. A start lock is not removed
merely because it is old; inspect the active preparation before recovering it.
Different worktrees can launch concurrently.
`node scripts/ai/verify-task-runtime.mjs` verifies two live runtimes, browser/API
connectivity and an idempotent owned write that stays invisible to the second
runtime, then cleans up both. Frontend/API readiness is distinct
from completing user journeys or verifying uploads and external integrations.

## Coding outcome evaluations

```powershell
node scripts/ai/coding-evals.mjs list
node scripts/ai/coding-evals.mjs prepare usda-link
node scripts/ai/coding-evals.mjs grade usda-link <candidate-path-from-prepare>
node scripts/ai/coding-evals.mjs self-test
```

Six historical tasks have immutable Git baselines and separate maintained
graders: meal identity, calendar meaning, recipe quantity, USDA linking and cycle
episode actions, plus precision-preserving nutrition basis conversion. Preparation extracts an owned candidate snapshot and its task
scope. Grading compiles positive and negative witnesses against actual candidate
production capabilities, runs native-value/conversion probes, and rejects changed
files outside scope. The grader, corpus and dependency-lock fingerprints must
match preparation; original source hashes are recovered from Git, not trusted
from an editable candidate record.

`self-test` proves that each historical baseline fails and the current reference
passes. It runs no models and is not a measurement of agent productivity. The
initial corpus covers semantic contract refactoring and one pure nutrition calculation; it is not general behavioral
or security coverage. Add independently reviewed regression scenarios as actual
coding tasks accumulate, and compare trials with the same baseline, dependencies,
grader and model settings. Keep the existing Wiki retrieval/policy evals separate.

Native-value probes execute generated candidate code in a digest-pinned Node
container with no network, no capabilities and read-only mounts limited to the
frontend source, dependencies and trusted probe worker. The process has CPU,
memory, output and time limits. Docker is required for grading; there is no
fallback that executes candidate code in the host process. Verification of this
boundary is separate from ordinary offline tool tests:

```powershell
node --test scripts/ai/coding-probe-isolation.test.mjs
```

## Real agent trials and instruction comparisons

```powershell
node scripts/ai/agent-trials.mjs run usda-link control --model <model> --reasoning medium
node scripts/ai/agent-trials.mjs run usda-link api-skill --model <model> --reasoning medium
node scripts/ai/agent-trials.mjs suite --model <model> --reasoning medium --repetitions 5
node scripts/ai/agent-trials.mjs compare
```

`suite` runs the selected historical tasks sequentially with both instruction variants.
Every trial starts from its own extracted Git baseline, initializes a separate Git
root, supplies a public acceptance contract and uses Codex CLI's explicit
`:workspace` permission profile through the npm-pinned Codex CLI. An actual file-write probe distinguishes a usable
coding environment from a session that only inspected source. User configuration
is excluded from the evaluation; authentication stays in Codex's existing store.
The runner does not read or copy that authentication material.

The runner uses the CLI in the locked frontend development dependencies, not a
global executable or the parent desktop app's version. Permission profiles require
Codex 0.138.0 or newer. Legacy sandbox flags are not mixed with the selected profile.
Parent chat identity, app-tool routing and ambient permission overrides are removed
from the child environment; authentication retains its existing location. Personal
and project execpolicy rules are excluded along with user configuration so trials
do not inherit unrelated command policies. Managed requirements remain enforced.
A failed
write probe stops a suite before further model calls and is reported as an environment
failure, not as evidence about instruction quality.

An observed command-policy rejection terminates the owned agent process,
preserves the failed attempt, and stops a suite. The runner does not retry rejected
commands through alternate tools or change managed policy. A successful direct
sandbox write proves OS readiness only; it does not prove the model tool router
will accept commands. Real trials require both an exact agent-authored write probe
and independent production-code grading before success is recorded.
CLI versions that omit tool results in JSONL are checked through bounded observations
of their own session's structured tool output. Reasoning and transcript text are
not exported. Missing command telemetry stays null, with its sample coverage
reported separately; it is never represented as zero tool failures.

The model is explicit; the CLI default is never a reproducible trial setting.
Trials persist their own CLI session so the runner can read a bounded model
receipt from matching session/checkout metadata. It never reads authentication
files, exports session transcripts, or infers actual identity from the requested
model. Missing observations remain unverified. The normal CLI session-retention
policy applies to these synthetic task sessions. Cohorts separate actual model,
requested model and reasoning budget. Repetitions alternate variant order;
`--case <id>` selects one case. Five samples per variant is only a reporting
floor, not proof of a productivity effect. Reports include Wilson success-rate
intervals, median duration, tool failures and human interventions.

Each owned `trial.json` records the CLI version, baseline, instruction, skill,
grader engine, harness and dependency hashes, actual terminal outcome, external
grade, elapsed time, usage when reported, interventions and tool failure counts.
Failed and timed-out attempts remain in the data. Events contain bounded sanitized
tool actions, error excerpts and completion messages; reasoning content and full
command output are omitted. The terminal grade inspects candidate production code,
not the agent's description of its work.

Comparisons separate incompatible baselines, graders, instructions, dependencies
and harnesses. Different skill fingerprints receive separate profiles, and running
attempts stay outside terminal success rates and elapsed-time averages. Small
cohorts report `insufficient-data`; an available configuration
cohort is not proof of model identity when the CLI did not report it. A timing
difference or one successful pair does not establish a productivity gain. Keep
resource limits consistent and review transcript-derived hypotheses before changing
instructions. The separate grader self-test remains zero model trials.

## Trace feedback into reviewed regressions

```powershell
node scripts/ai/agent-feedback.mjs analyze
node scripts/ai/agent-feedback.mjs draft <candidate-id>
node scripts/ai/agent-feedback.mjs promote <proposal-path> <reviewer> <rationale>
node scripts/ai/agent-feedback.mjs evaluate <trial-json-path>
```

Analysis links each candidate to exact trial and event hashes. It distinguishes
an unusable write environment, unchanged failed production code, failed commands
and repeated reads. Expected negative tests and useful repeated reads need context;
those signals cannot become generic mandatory rules automatically.

Drafts remain under `.artifacts/agent-feedback`. Explicit review can promote the
supported outcome rules to `Tooling/ai-development/agent-regressions.json` after
rechecking source hashes. The reviewer and rationale are retained. This registry
retains reviewed evaluation rules; `evaluate` applies them to recorded trial
outcomes and fails on unresolved rules. It grants no execution permissions and does not edit
repository instructions or policy. Maintain new coding problems and graders in
the existing corpus when a failure needs a new behavioral scenario.

## Task completion tools through MCP

The Development MCP now has ten tools. Existing discovery tools retain their
contracts. `get_next_task_action(evidencePath)` selects unresolved verification,
active jobs or stale proof, then points to remaining review/acceptance validation.
`get_task_diagnostics(evidencePath, jobId, runtimeName)` returns bounded sanitized
diagnostics from owned task artifacts and timestamped cached browser observations.
`collect_task_runtime_diagnostics(runtimeName)` refreshes the rendered observation;
its write annotation reflects the synthetic login session and local diagnostic file.

`verify_task(evidencePath, checkId, timeoutMinutes)` starts one check from an
existing Wiki evidence bundle and immediately returns a job ID. The Wiki executor
revalidates the canonical command against current change policy; callers never
supply shell text. Jobs serialize writes to the same evidence bundle, limit output
and execution time, and bind results to the source fingerprint.

Poll `get_task_diagnostics` with that job ID. `cancel_task_check(jobId)` requests
termination of the worker's owned process tree after its lifetime identity matches.
A client timeout is not evidence that the job stopped. Cancellation, interruption,
failure and source changes are explicit terminal/unverified states; they are not
passing proof. Completion still uses the existing Wiki acceptance and review gates.
Verification and cancellation have write annotations and write result envelopes.

## Agent-readable local runtime observations

```powershell
node scripts/ai/runtime-diagnostics.mjs collect my-change
node scripts/ai/runtime-diagnostics.mjs status my-change
node scripts/ai/runtime-diagnostics.mjs verify-probe my-change
```

Collection verifies runtime ownership and process lifetime, authenticates only the
synthetic owned account, and observes the real dashboard through its own proxy.
It records browser/console faults, failed owned HTTP requests and network failures,
with locations and correlation identifiers when available. No request/response
bodies, authentication values or URL query values are retained. Cached observations
carry their timestamp; runtime source drift is reported explicitly.

Server/frontend/initializer excerpts read a bounded log tail. The response includes
the exact collection and stop commands. `verify-probe` intentionally makes a labeled
read-only request to a missing local route, proving that a real failed response is
captured; it is verification machinery, not a product endpoint. External providers
remain disabled by the runtime profile.

## Focused API-change skill

`.agents/skills/fooddiary-api-change/SKILL.md` routes API consumer and published
capability changes to generated operations, owner semantic types and affected
checks. Its examples reference the real Hydration decoder, authorized handler and
their existing tests. Load examples only for the layer involved. The examples are
checked against current source rather than maintained as an unrelated template.

Use the `api-skill` trial variant to exercise this skill in an isolated snapshot.
The existing Wiki instruction experiments remain the route for broader instruction
changes. Promote recommendations only from comparable actual outcomes; generic
documentation growth is not a success metric.

```powershell
node --test scripts/ai/agent-tools.test.mjs
dotnet test Tooling/tests/FoodDiary.Development.Mcp.Tests/FoodDiary.Development.Mcp.Tests.csproj --filter FullyQualifiedName~TaskToolsTests
```
