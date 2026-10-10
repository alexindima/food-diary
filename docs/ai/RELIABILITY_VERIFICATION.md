# Reliability verification

The local pass runs only against an owned synthetic FoodDiary task runtime.
Production changes, private backups and live AI requests are outside these
commands. Reports distinguish executable checks from unverified external facts.

## Commands and evidence

```powershell
cd FoodDiary.Web.Client
npm run test:ai-development
npm run test:recognition:reference
npm run test:alerts
npm run test:reliability:local
```

The complete driver starts one runtime, runs repeated load measurements,
restores a database dump, exercises persisted browser journeys, then stops its
processes/containers and cleans only the recorded build output. Load and recovery
must run sequentially: the recovery comparison rejects a changing source.
The three tools share a PID/creation-identity lease for the owned runtime;
concurrent execution is refused before fixture writes or snapshot capture.
On any failing step, independent later checks still run and the final summary
fails. A runtime-start failure preserves its own diagnostic record.

Individual commands can reuse a current owned runtime:

```powershell
node scripts/ai/task-runtime.mjs start --name my-verification
node scripts/ai/load-baseline.mjs my-verification
node scripts/ai/recovery-drill.mjs my-verification
node scripts/ai/verify-user-journeys.mjs --runtime my-verification
node scripts/ai/task-runtime.mjs stop --name my-verification
node scripts/ai/resource-budget.mjs clean-build my-verification
```

The journey driver preserves a supplied runtime. Evidence lives in
`.artifacts/reliability/<runtime>/`, with separate UUID directories for browser
runs. Credentials and bearer tokens remain in memory or ignored private runtime
files; stdout and reports redact literal fixture secrets as well as token/email
patterns. Traces and login screenshots are disabled.

## Recognition reference corpus

`Tooling/ai-development/recognition-evals/cases.json` contains 12 explicit-quantity
texts and eight authored SVG nutrition labels. Labels are synthetic, with
fictional declared values; text references cover identity, fractions, two foods
and EN/RU units. Unreadable label fields must remain null, and zero must remain
zero. This corpus does not establish real-food-photo identity or mass accuracy.

```powershell
node scripts/ai/recognition-evals.mjs prepare
node scripts/ai/recognition-evals.mjs grade .artifacts/my-recorded-replies.json
```

Use the prepared template's corpus hash and unique case IDs. Capture actual model
replies with model/prompt revision, elapsed time and reported cost. The grader
reports exact case accuracy, p50/p95, missing observations and reported cost
coverage; it never calls a provider or independently verifies submitted metadata.
Missing cases count as failures. Grader self-tests use an oracle and are not
model-quality or cost evidence. Rasterize SVG fixtures to PNG before a provider
run that does not accept SVG; preserve the reference text and basis.

Real food photos need owned images, explicit food annotations and measured
portion ground truth before adding them. Follow Ai's no-live-provider verification
rule unless a separate authorized evaluation explicitly changes that scope.

## Database recovery

The drill uses the labelled task PostgreSQL container and the `fooddiary_task`
source only. It creates a custom-format dump, restores it into a new
`fd_restore_<nonce>` database, and compares row counts **and row-content hashes
for every public table**, plus constraint definitions. PostgreSQL reparses CHECK
definitions on empty temporary copies before comparison, accounting for equivalent
casts introduced by historical column-type changes; no source rows are mutated.
It rejects concurrent
source writes, reports elapsed database restore time and deletes its target and
private dump. It never restores over the source or accepts a production dump.

The default 300-second budget is a local test deadline, not an agreed production
RTO. Zero observed synthetic-record loss is not a production RPO measurement.
Object-storage recovery, off-host backup durability, encryption, retention and
actual production RPO/RTO require separate operator evidence.

## Workstation load baseline

The default plan seeds 1500 private products, 100 recipes and 300 meals through
owner APIs. It runs 1, 5 and 10 concurrent users twice, using pinned native k6
v1.3.0 and 15-second hold periods. Archive and cached-binary SHA256 checks protect
the tool bootstrap; nothing is installed globally. API calls use a synthetic
owned account. Cleanup removes meals before recipes and products by recorded IDs.

Reports retain dataset, source, hardware, concurrency and metric information.
These short runs are repeatable regression evidence, not production capacity
claims. Extend hold periods and use an isolated production-like environment for
capacity decisions. Investigate failed thresholds rather than silently raising
them. Normal authentication/rate limits remain in force.

## Alert evaluation

Pinned `promtool` validates existing rules and the separate 99.9% availability
error-budget rules. Tests cover healthy traffic, missing series, client-error
masking, fast/slow budget burn, low volume, outbox recovery and failed jobs.
The new rules exclude 4xx from eligible requests and use request-volume guards.
Missing input stays absent; it is not converted to a healthy zero.

The rule tests use controlled time series without network access. They do not
prove production `rule_files`, metric labels, Grafana provisioning or notification
delivery. Load the additional rule file and verify the deployed metric/version
contract through the normal authorized operational workflow.

## Development resource and index ownership

Task startup measures free workspace space before allocating containers and
requires at least 3 GiB. API/initializer builds use at most two MSBuild nodes.
Native build cleanup requires a stopped runtime, matching receipt, contained
real path and dead recorded processes. It retains reports and never cleans the
shared `.artifacts/llm-wiki` cache. Older unregistered scopes require manual review.

During Wiki publication, quality/contract writers finish before module-page
readers start. Check mode keeps parallel read-only validation. This dependency
barrier prevents the reproduced Windows memory-map/truncation conflict without
reducing source-index parallelism.
