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
request through the frontend proxy before reporting readiness.

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

Five historical tasks have immutable Git baselines and separate maintained
graders: meal identity, calendar meaning, recipe quantity, USDA linking and cycle
episode actions. Preparation extracts an owned candidate snapshot and its task
scope. Grading compiles positive and negative witnesses against actual candidate
production capabilities, runs native-value/conversion probes, and rejects changed
files outside scope. The grader, corpus and dependency-lock fingerprints must
match preparation; original source hashes are recovered from Git, not trusted
from an editable candidate record.

`self-test` proves that each historical baseline fails and the current reference
passes. It runs no models and is not a measurement of agent productivity. The
initial corpus covers semantic contract refactoring; it is not general behavioral
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
node scripts/ai/agent-trials.mjs run usda-link control
node scripts/ai/agent-trials.mjs run usda-link api-skill
node scripts/ai/agent-trials.mjs suite
node scripts/ai/agent-trials.mjs compare
```

`suite` runs the five historical tasks sequentially with both instruction variants.
Every trial starts from its own extracted Git baseline, initializes a separate Git
root, supplies a public acceptance contract and uses Codex CLI's explicit
`:workspace` permission profile. An actual file-write probe distinguishes a usable
coding environment from a session that only inspected source. User configuration
is excluded from the evaluation; authentication stays in Codex's existing store.
The runner does not read or copy that authentication material.

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
