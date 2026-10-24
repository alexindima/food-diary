# Versioned Modern Web Guidance checks

FoodDiary WebQuality is an independent Node CLI in `Tooling/WebQuality` with
frontend adapters under `FoodDiary.Web.Client/quality/modern-web-guidance`.
It keeps Google source versions, FoodDiary adoption decisions, executable checks
and results separate. It does not change the existing lint configuration,
application behavior, Lighthouse runner, or delivery workflows.

## Architecture and boundaries

- `upstream/upstream.lock.json`: the complete published guide inventory, exact
  Google repository commit, release version and individual Git blob hashes.
- `upstream/sources/`: unmodified content-addressed Markdown for mapped guides.
  These are reference data, not instructions to the coding agent. Normal runs
  are offline and verify each cached file against its upstream Git blob.
- `policy.json`: reviewed requirements, source anchors/hashes, scope, engine,
  severity, manual procedures and narrowly scoped exceptions.
- `lib/` and `cli.mjs`: integrity, update staging/adoption, result contracts,
  provenance and HTML/JSON reports. The CLI has no npm dependencies.
- Frontend adapters resolve the **existing frontend dependency tree** for Angular
  ESLint, TypeScript, PostCSS, Playwright and axe. The source runner reuses the
  actual FoodDiary UI button rule from the existing ESLint configuration.
- The browser adapter exercises public/login states. The journeys adapter runs
  the existing authenticated axe fixtures on its own port 4318, without reusing
  another session's dev server. The performance adapter calls the existing
  Lighthouse collector and validates its real samples and repository budgets.

A separate Angular dashboard or a fork of Google's agent evaluation harness
would add maintenance without improving these checks. The generated HTML report
already provides search, engine/status filters, source links and evidence detail.

## Install and run

Use the same supported Node version as frontend CI (Node 22 or newer). No install
step is needed inside `Tooling/WebQuality`. In a new worktree, install frontend
dependencies independently. For an isolated audit without changing Git hook
configuration:

```powershell
cd FoodDiary.Web.Client
npm ci --ignore-scripts --no-audit --no-fund
node scripts/apply-dependency-patches.mjs
cd ..
node Tooling/WebQuality/cli.mjs validate
node --test Tooling/WebQuality/test/*.test.mjs
node Tooling/WebQuality/cli.mjs audit --profile source
```

The frontend also exposes additive wrappers: `npm run quality:validate`,
`npm run quality:audit`, `npm run test:web-quality` and
`npm run test:web-quality:browser`. Existing verification scripts are unchanged.
These wrappers integrate with the repository's existing evidence-command policy.
`test:web-quality` runs both unit contracts and the controlled browser/report
checks; the `:browser` variant runs only the browser checks. Use the direct Node
unit-test command above when Chrome is unavailable.

Source audits include client, admin, UI kit, tour and WebMCP runtime sources,
external HTML and literal inline Angular templates. Generated SDK files and
test/story sources are excluded. Files are parsed with their owning parsers;
malformed templates/styles or missing dependencies are engine errors. A known
invalid Angular fixture must report before application results are trusted.

Each run prints its unique report directory under `.artifacts/web-quality/runs`.
Open `index.html`; `report.json` contains machine-readable evidence. Use
`--report-dir .artifacts/web-quality/<name>` for a chosen run directory. Keep
separate directories for concurrent runs and do not overwrite a run being read.

`node Tooling/WebQuality/verify-browser.mjs` additionally tests the browser engine
against controlled valid/invalid pages and checks report filters/layout in a
real browser. It uses Chrome locally. CI explicitly installs the Chromium
version locked by the frontend Playwright package and selects that channel.

### Rendered public and login states

Build production assets with a source/output receipt, then audit them through a task-owned ephemeral
static server. Chrome is the default channel; it uses a fresh headless profile.
No production account or personal signed-in browser session is used.

```powershell
node Tooling/WebQuality/cli.mjs build
node Tooling/WebQuality/cli.mjs audit --profile source,browser --static-root FoodDiary.Web.Client/dist/browser
```

The build command runs the existing `npm run build:prod` script and records input
and output fingerprints in `.artifacts/web-quality/build.json`. Local browser
and performance audits verify that receipt before and after execution. Changed
sources, changed assets or missing receipts require a rebuild. This prevents an
old `dist` directory from being reported as evidence for current source files.
For `--base-url`, the origin and browser results are recorded, but association
with this checkout remains explicitly unverified. The report labels its Git
revision as the tool checkout revision.

The initial matrix covers EN/RU home pages at desktop/mobile sizes and the EN
login modal at both sizes, with normal/reduced motion and light/dark browser
preferences represented. This does **not** prove motion preferences or theme
behavior: those require their own assertions. Checks cover detected axe
violations, language/title/landmarks, viewport overflow/zoom metadata, rendered
login autocomplete and actual modal focus transitions. axe's incomplete checks
remain visible for manual review. Focus checks run last because they close the
dialog, verifying both tab boundaries, Escape and focus restoration.

`--base-url http://127.0.0.1:<owned-port>` can audit an existing owned origin.
`--browser firefox` or `--browser webkit` requires that Playwright browser to be
installed; unavailable engines fail explicitly. A WebKit run is not proof on
every supported Safari/OS combination. No universal browser support claim is
made by a Chrome-only run.

The static server inherits the existing Lighthouse server's behavior: API
requests fail unless `--api-origin` is supplied. That option forwards anonymous
requests and strips credentials. This is suitable for anonymous UI, not a
replacement for authenticated live-backend testing. Production builds and
signed-out pages intentionally expose no FoodDiary WebMCP tools.

### Authenticated accessibility fixtures

```powershell
node Tooling/WebQuality/cli.mjs audit --profile journeys
```

This starts a separate dev server from the current checkout at port 4318 and
runs the existing `authenticated accessibility` suite with a JSON reporter.
Occupied ports fail; existing servers are not silently reused. The suite uses
mocked API fixtures and covers only the routes/states in the existing tests.
Skipped tests, empty reports and runner errors cannot establish a passing result.
Logs, failure screenshots and traces stay in the report directory. These runs
do not sign in to or change production.

### Performance

```powershell
node Tooling/WebQuality/cli.mjs audit --profile performance
node Tooling/WebQuality/cli.mjs audit --profile performance --routes=/ --devices=desktop
```

Build production first. The default collects the existing four-route Lighthouse
smoke inventory on mobile and desktop, with three sequential cold runs per case.
The narrower example measures only the explicit selected scope. Reports preserve
the actual route/device plan and measurement context. Do not run CPU-heavy jobs
concurrently with measurements.

This adapter applies the **repository's existing budgets**, not a claim that
Google mandates those thresholds. It validates at least three samples and
the complete collection before accepting measurements. Lighthouse scores and
TBT do not establish field INP or complete Core Web Vitals conformance. Existing
baseline comparison remains available through the original runner; see
[Public Lighthouse](PUBLIC_LIGHTHOUSE.md). A standalone saved summary is not
silently treated as execution evidence for the current source revision.

`--profile all --static-root FoodDiary.Web.Client/dist/browser` runs source,
public browser, authenticated fixture and performance engines sequentially.

## Reading the result

Requirement statuses are `pass`, `fail`, `error`, `not-run`, `manual`,
`not-applicable` and `exception`. An error is failure of execution/integrity,
not an application finding. Missing engines stay `not-run`; zero observations
cannot pass. Manual requirements carry concrete verification procedures. They
remain manual until a separately reviewed evidence workflow is introduced.

Guide coverage is tracked independently: `unreviewed`, `partial`, `reviewed`,
or `not-applicable`. Mapping some recommendations marks a guide only `partial`.
Whole-guide review decisions must include a reason and match the pinned source
blob. A changed guide invalidates its previous review. Inapplicability must be
explicit; a feature that was never evaluated is not automatically inapplicable.

The initial policy maps 31 requirements across eight guides from Google version
0.0.195 and indexes all 192 published guides at commit
`65e7edd67e77420eaf02c5df00fdc0521417d809`. It intentionally reports the remaining
guides as unreviewed. Examples of residual work are screen-reader verification,
OS text scaling, reduced motion, supported-browser fallback review and real-user
performance. Source lint does not prove runtime behavior or complete WCAG.

Exit codes:

- `0`: no blocking findings in the executed scope. Other requirements or guides
  may remain untested/unreviewed; inspect the report.
- `1`: an error-severity application finding, a warning under `--strict`, or
  incomplete coverage requested with `--complete`.
- `2`: configuration/integrity failure or an engine could not execute.

`--complete` also rejects partial/unreviewed guides and manual/not-run/exception
requirements. It cannot currently pass the initial adoption profile. This is
intentional. There is no misleading aggregate conformance percentage.

Typography preferences currently have warning severity. Robust static rules,
browser assertions and engine failures block their selected audit. Exceptions
require a requirement ID, an exact source file or browser case ID, owner, reason
and ISO expiry date (UTC). They match only when all findings belong to that exact
scope. Wildcard folder exemptions and engine-error exemptions are not supported.

## Update and extend the adopted guidance

Google publishes source-authoring material separately from the compiled guides.
We pin the published `GoogleChrome/modern-web-guidance` tree, so incomplete source
stubs do not become application requirements. The updater downloads metadata
and selected Markdown, verifies Git blob hashes and never installs or executes
upstream skills, graders or scripts.

```powershell
node Tooling/WebQuality/cli.mjs update --ref main --output .artifacts/web-quality-candidate
node Tooling/WebQuality/cli.mjs diff --candidate .artifacts/web-quality-candidate
```

Only update preparation accesses the network. The candidate records an immutable
commit and `changes.json` lists added, changed and removed guides plus dependent
requirement IDs. The accepted snapshot is unchanged. Add `--cache <guide-id>` to
stage text for a previously unreviewed guide; comma-separated IDs are supported.

Review changed text and compatibility notes. For every changed mapped guide,
reconsider each requirement's meaning, scope and checker; then update
`source.reviewedBlob` and its anchor in `policy.json`. Update whole-guide review
records too. New guides remain explicitly unreviewed until evaluated. Removed
mapped guides cannot be adopted without updating/retiring their requirements;
do not silently delete a useful project guard just because upstream removed it.

```powershell
node Tooling/WebQuality/cli.mjs adopt --candidate .artifacts/web-quality-candidate --note "Reviewed affected requirements, scopes and compatibility"
node Tooling/WebQuality/cli.mjs validate
node --test Tooling/WebQuality/test/*.test.mjs
```

Adoption validates the candidate against the **current reviewed policy** before
writing anything accepted. Stale hashes and missing anchors fail. Source files
are content-addressed and copied first; the lock is atomically replaced last.
`review.json` records the adoption note. Inspect the Git diff and rerun affected
application audits before committing the new standard and its checks together.

To add a requirement, select a precise source assertion, declare its scope and
choose an existing check or add a focused adapter assertion. Provide invalid
and valid fixtures. A rule that checks where `scope` is legal, for example,
does not establish that every table header declares scope. Keep these contracts
as narrow as their actual assertions.

## CI and isolation

`.github/workflows/web-quality.yml` is independent of current CI/deployment files.
Frontend/tool pull requests validate the pinned catalogue, run the tool and
controlled browser-engine/report tests, and execute source audits. Application
browser audits, authenticated fixtures and repeated
Lighthouse collection are explicit manual-workflow options. Reports upload even
after failure. The workflow never updates accepted sources or deploys anything.

Use a separate worktree and its own dependencies/build outputs for independent
work. Automated browser profiles, temporary servers, logs and report directories
are task-owned. Existing sessions and the primary checkout are not reused or
cleaned up. For interactive FoodDiary verification follow the repository browser
QA skill; the automatic audit remains headless.
