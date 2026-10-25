# Versioned Modern Web Guidance checks

FoodDiary keeps Google's guidance as a versioned engineering policy in
`Tooling/WebQuality`, with application adapters in
`FoodDiary.Web.Client/quality/modern-web-guidance`. The accepted snapshot contains
all **192 guides** from Google version **0.0.195**, commit
`65e7edd67e77420eaf02c5df00fdc0521417d809`.

Every guide has a source-bound applicability decision: **152 reviewed as
applicable**, and **40 conditionally inapplicable** because the corresponding
capability is absent. There are **68 requirements**: 61 executable assertions or
linked owner suites, and seven explicit human/field assessments. Several guides
share the same underlying assertion. A review decision is not a certificate that
every sentence in a guide has been satisfied.

## Ownership and execution

- `upstream/upstream.lock.json` records the complete inventory, release, commit
  and individual Git blob hashes. `upstream/sources/` contains all 192 unmodified,
  content-addressed texts. They are reference data, never executable agent
  instructions. Policy/source audits are offline and validate source integrity.
- `policy.json` owns the per-guide decisions, source anchors, evidence paths,
  requirement mappings, scopes, severity and manual procedures. The full-catalogue
  mode rejects missing decisions, stale blobs and invalid mappings.
- Forty absence decisions include concrete feature probes and positive fixtures.
  Every source audit rechecks them against application/library sources and
  relevant configuration. A newly detected feature fails the audit until its
  guide is reconsidered. This is a bounded adoption guard, not a proof against
  every possible alias or dynamically assembled API call.
- The CLI has no separate npm dependency tree or running server. Frontend
  adapters use the workspace's locked Angular ESLint, TypeScript, PostCSS,
  Playwright and axe dependencies. Existing ESLint rules and owner tests are
  reused; Google's example harness and graders are not executed.
- HTML and JSON reports contain searchable requirements, engine/status filters,
  per-guide reasons, source links, findings and execution provenance. An
  additional Angular dashboard would duplicate this maintained interface.

The source engine covers client, admin, UI kit, tour and WebMCP runtime code,
entry documents and literal inline Angular templates. Generated SDKs, tests and
stories are excluded. It checks concrete semantics, metadata, form transport,
media/resource hints, CSS fallbacks and scaling, selected injection/message/code
boundaries and persistent credential writes. Parsers and representative invalid
fixtures establish that the assertions can actually fail; malformed inputs and
missing dependencies are execution errors.

The contracts engine executes actual UI kit, tour, WebMCP, calendar, client
platform, admin platform and production/PWA suites. Required suite names,
nonzero assertion counts and executed outcomes are validated. Missing, skipped,
failed or inconsistent evidence cannot become a passing result.

## Running locally

Use Node 22 or newer and install the frontend's locked dependencies. An isolated
installation can avoid changing shared hook configuration:

```powershell
cd FoodDiary.Web.Client
npm ci --ignore-scripts --no-audit --no-fund
node scripts/apply-dependency-patches.mjs
cd ..
node Tooling/WebQuality/cli.mjs validate
node --test Tooling/WebQuality/test/*.test.mjs
node Tooling/WebQuality/cli.mjs audit --profile source,contracts
```

Frontend wrappers include `quality:validate`, `quality:audit`,
`test:web-quality:unit` and `test:web-quality:browser`. `test:web-quality` runs
both the unit and controlled browser/report checks. The inexpensive policy,
unit, document and source checks also run through `npm run verify`.

Each audit prints its report directory under `.artifacts/web-quality/runs`.
Open `index.html`; retain `report.json` and linked logs for machine-readable
evidence. Use `--report-dir .artifacts/web-quality/<name>` for a chosen directory.
Concurrent runs must use different directories.

### Production browser matrix

```powershell
node Tooling/WebQuality/cli.mjs build
node Tooling/WebQuality/cli.mjs audit --profile browser --static-root FoodDiary.Web.Client/dist/browser
node Tooling/WebQuality/cli.mjs audit --profile browser --browser firefox --static-root FoodDiary.Web.Client/dist/browser
node Tooling/WebQuality/cli.mjs audit --profile browser --browser webkit --static-root FoodDiary.Web.Client/dist/browser
```

The build uses `npm run build:prod` and records input/output fingerprints in
`.artifacts/web-quality/build.json`. Local browser and performance runs validate
the receipt before and after execution. Changed sources or assets require a
rebuild. A supplied `--base-url` records its actual origin, but does not establish
that the remote deployment matches the checkout.

The public matrix includes EN/RU home, SEO, login and registration states at
desktop/mobile sizes. Assertions cover axe, semantic structure, keyboard and
modal focus, current-document skip navigation, credential/autofill metadata,
invalid submit feedback, 200% root text, 320 CSS pixel reflow, reduced motion and
resource priorities. System colors are represented. A bounded, named,
keyboard-scrollable data table qualifies for the two-dimensional reflow
exception; an inaccessible or clipped container does not.

Axe issue [#3978](https://github.com/dequelabs/axe-core/issues/3978) mixes
unforced text fill with forced backgrounds. For system-color scenarios semantic
checks run in the requested mode and author contrast runs separately in normal
colors. The report records this limitation. Actual system-palette legibility
remains a human assessment; no application color is changed merely to hide that
tooling error.

The two controlled browser verifiers test good and broken fixtures, nested base
URLs, scrolling exceptions and report interactions. A fixture failure blocks
trust in the corresponding application result. Chrome is the local default;
CI installs the browser engines locked by the frontend Playwright package.

### Client and admin journeys

```powershell
node Tooling/WebQuality/cli.mjs audit --profile journeys
```

This starts owned development servers on ports **4318 and 4319**, without
reusing existing sessions. Occupied ports fail. Three required projects cover
client authentication/accessibility and mobile navigation, failed saves and
draft protection, product form/tour behavior, admin SSO/navigation and moderation
recovery, plus platform regressions. These include system-color focus, failed
font loading, SEO text enlargement and an offscreen preview that loads only when
the reader reaches it.

Authenticated tour and update-notice UI are deferred until the authenticated shell
is used. Actual production startup metadata rejects their accidental eager
inclusion. Public sections with actionable controls use ordinary hydration so
the first keyboard activation works across browser engines. Prerender checks
protect their server-visible content.

Packaged SSR translations are transferred by language and requested bundle;
marketing-page startup does not transfer the private application dictionary or
unrelated landing copy into SEO/privacy pages. Initial
public language follows the URL before browser/stored preferences, matching the
route guard. Owner tests cover transfer/cache behavior and the CI prerender
check verifies the needed bundles in all 48 generated localized documents.

Production/staging use an empty replacement for the local WebMCP providers;
admin production does the same. A runtime `isDevMode()` branch alone did not
remove the development implementation bytes. Production build guards inspect
actual bundle inputs, including lazy chunks, and reject these implementations.
Development builds retain their existing tools.

The API fixtures are mocked; this is not a live-backend audit or evidence of
production delivery. Skips, empty/missing projects, retries and runner failures
cannot silently establish success. Logs, attachments, failed screenshots and
traces are retained. Interactive FoodDiary verification follows the repository's
browser QA skill; automated audits use fresh headless profiles.

### Performance and field data

```powershell
node Tooling/WebQuality/cli.mjs audit --profile performance
node Tooling/WebQuality/cli.mjs audit --profile performance --routes=/ --devices=desktop
```

Build production first. The default runs the existing four-route public smoke
inventory on mobile and desktop, with three sequential cold runs per case
(24 measurements). It validates complete comparable samples and applies the
repository's existing budgets. Do not run CPU-heavy jobs concurrently. Broader
prerender inventory and baseline comparison remain available through the
original runner; see [Public Lighthouse](PUBLIC_LIGHTHOUSE.md).

Inter is served locally as pinned Google Fonts v20 WOFF2 subsets, with its OFL
license, source URLs and SHA-256 manifest under `src/fonts/inter`. Both apps use
the same weight range and Unicode coverage. The font asset contract checks bytes,
font-face metadata and entry documents, so a remote font stylesheet cannot silently
return to the startup path. Only used subsets are requested; unused fonts remain
in the existing lazy PWA asset group. Browser scenarios also block font delivery
and check that content and authentication controls remain usable.

The Lighthouse static host defaults to HTTPS with negotiated HTTP/2, matching the
public nginx configuration. Its ephemeral loopback certificate stays in memory;
Chrome trusts only that generated public-key fingerprint. Each local sample must
contain actual `h2` network evidence. This is not a production TLS assessment.
HTTP/1 diagnostic runs remain available, and their distinct comparison context
cannot reuse an HTTP/2 baseline. Scores from the two transports are not a code
before/after comparison. The certificate helper is a locked development dependency.

Private client document entries dynamically load Google's `web-vitals` once and report
CLS, INP, LCP, FCP and TTFB through the owned generated logging SDK. Final updates
and bfcache metric IDs are retained; CLS is not rounded to zero. Transport uses
low priority and keepalive. Known credential/PII patterns and URL parameters are
masked, and diagnostics are bounded. No raw performance entries, DOM nodes or
third-party analytics endpoint are added. The API accepts all five metrics and
retains only the bounded official anonymous metric-ID format, never arbitrary
details or session IDs. Document-lifetime metrics keep their entry route when
SPA navigation occurs; they are not presented as measurements of every soft
navigation. Server log routes are redacted independently of client masking.

Field analysis must use the latest value per metric ID, sufficient sample counts
and appropriate release/device/route cohorts. Public/admin field coverage and
actual delivery loss require separate evidence. Lighthouse TBT is not field INP;
passing laboratory budgets or checking request options does not establish p75
Core Web Vitals conformance.

`--profile all --static-root FoodDiary.Web.Client/dist/browser` runs source,
contracts, public browser, owned journeys and performance sequentially.

## Interpreting results and limits

Requirement statuses are `pass`, `fail`, `error`, `not-run`, `manual`,
`not-applicable` and `exception`. An error denotes failed execution/integrity.
Unexecuted engines stay `not-run`; zero observations cannot pass. Reports do not
convert review coverage into an aggregate conformance percentage.

Seven manual requirements retain concrete procedures for screen readers, actual
OS text scaling, motion comfort, browser/device fallbacks and system palettes,
field performance, privacy purposes/retention/deletion, and deployed security
policies. The current frontend CSP permits `unsafe-inline` for existing startup,
prerender and provider dependencies; strict nonce/hash CSP and Trusted Types need
a staged integration review. The automatic checks do not certify that rollout,
privacy compliance, reverse-proxy configuration or every application screen.

Access credentials now remain in tab storage. Remember-me and reload recovery
use the existing HttpOnly refresh-cookie flow. Legacy persistent credentials
migrate once; non-secret cross-tab ownership/invalidation hints remain separate.
Session storage still requires application script isolation and is not an
HttpOnly credential boundary. Owner suites protect account switching, logout,
SSO, impersonation and late responses.

Exit codes are `0` for no blocking findings in the executed scope, `1` for
blocking findings or requested incomplete coverage, and `2` for engine or
configuration errors. `--strict` promotes warnings. `--complete` also rejects
manual, not-run, exception and partial/unreviewed states, so it intentionally
cannot pass while the manual assessments remain unresolved.

## Updating the accepted standard

```powershell
node Tooling/WebQuality/cli.mjs update --ref main --output .artifacts/web-quality-candidate
node Tooling/WebQuality/cli.mjs diff --candidate .artifacts/web-quality-candidate
```

Updates stage a separate candidate and structured diff without changing the
accepted policy. The candidate retains cached content-addressed texts; use
`--cache <guide-id>` to request additional texts. Review new, changed and removed
guides, normative assertions and browser compatibility. Update every affected
guide decision, evidence path, source anchor/hash and requirement/checker. A new
guide needs an explicit decision; removing a guide is not permission to silently
remove a useful FoodDiary guard. An absence decision needs feature probes,
positive fixtures and an executable adoption guard.

```powershell
node Tooling/WebQuality/cli.mjs adopt --candidate .artifacts/web-quality-candidate --note "Reviewed affected assertions and compatibility"
node Tooling/WebQuality/cli.mjs validate
node --test Tooling/WebQuality/test/*.test.mjs
```

Adoption validates the candidate against the reviewed policy before writing
accepted state. Stale blobs, missing anchors or incomplete full-catalogue
decisions fail. Source files are copied first and the lock is replaced last;
`review.json` records the decision. Commit policy, source changes and working
checker fixtures together, then rerun affected owner/browser/performance checks.
Upstream publication is never automatically treated as an approved policy.

## Automatic CI

`CI Tests` calls `.github/workflows/web-quality.yml` for pull requests,
master/main pushes, manual runs and the existing weekly schedule. Policy/tool
contracts, controlled browser verifiers, source/owner audits, production build,
Chromium/Firefox/WebKit public checks, client/admin journeys and repeated
Lighthouse all execute automatically. There are no opt-in booleans for these
engines. A failing quality job makes the overall CI workflow fail, which also
prevents the existing success-based deployment continuation.

Reports and failed-run evidence upload even after failure. The workflow does
not adopt upstream changes, merge branches or deploy on its own. Worktrees keep
their own dependency trees, build outputs, servers and reports; personal browser
sessions and unrelated development processes are not reused or cleaned up.
