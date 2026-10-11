# Public page Lighthouse audits

Run these commands in `FoodDiary.Web.Client`. Chrome and the existing npm
dependencies are required; the runner owns an isolated headless Chrome profile.
It never uses a signed-in browser session.
On Windows, a locked Chrome log can remain in the dedicated temporary profile
after Chrome stops. This cleanup warning does not invalidate saved measurements
or turn a completed collection into a failure.

## Collect reports

```powershell
npm run audit:lighthouse
```

This builds **production** assets and audits every route in
`src/prerender-routes.txt`. The current inventory has 48 pages: 24 English and
24 Russian. Each page gets the official Lighthouse mobile and desktop presets,
with three sequential cold runs per preset: 288 audits. Lighthouse clears browser
storage and the network cache between runs. The collection is deliberately
opt-in; do not run another build, browser audit or CPU-heavy job concurrently.
Dynamic recipe pages and authenticated screens are outside this static inventory.

For a faster representative pass, build once and reuse the output:

```powershell
npm run build:prod
npm run audit:lighthouse:smoke
```

The smoke set is `/`, `/calorie-counter`, `/ru`, `/ru/calorie-counter`: 24 audits.
The command audits the existing `dist/browser`; rebuild after changing sources.
Custom collection and preflight examples:

```powershell
npm run audit:lighthouse:collect -- --routes=/,/ru/pricing --devices=mobile --runs=3
npm run audit:lighthouse:collect -- --dry-run
npm run audit:lighthouse:collect -- --smoke --runs=1
npm run audit:lighthouse:collect -- --api-origin=http://localhost:5300
npm run audit:lighthouse:collect -- --base-url=https://fooddiary.club --smoke
```

`--dry-run` verifies the selected static HTML files and prints the plan without
launching Chrome. Missing pages and assets return errors rather than a homepage
fallback. The local server delivers gzip for text assets. It models static hosting,
not production TLS, CDN latency, cache policy or server load.

Generated files under `reports/lighthouse/` are excluded from ESLint: report HTML
is standalone output, not an Angular application template. Source templates and
the deterministic runner checks retain their normal verification.

Without `--api-origin`, API requests fail (404 for GET/HEAD, 405 for other methods). Supply the real local backend
origin when checking application startup with its anonymous API responses. The
proxy preserves the upstream response and strips authentication/cookies. It does
not mock API data. A remote `--base-url` uses the site's own API and hosting.
Local/remote targets and API origins are separate comparison contexts. Only
HTTP(S) origins without embedded credentials, paths, queries or fragments are
accepted. Do not audit production with any private session or tokens.

## Read and check results

Open `reports/lighthouse/summary.html`. It includes all four Lighthouse scores,
FCP, LCP, TBT, CLS and total transfer bytes. Each cell shows the median and
min–max range. Individual HTML/JSON reports and a summary snapshot are preserved
under `reports/lighthouse/runs/<timestamp>-<pid>/`. The latest machine-readable
summary is `reports/lighthouse/summary.json`; collection also records budget
violations there. Failed or missing runs never count as valid medians.

Collection exits with a failure for incomplete audits. Budget violations are
reported without stopping collection, so all pages can be investigated. After
reviewing a complete run with at least three samples per case, explicitly save
the comparison baseline:

```powershell
npm run audit:lighthouse:baseline
npm run audit:lighthouse:check
```

The default baseline is `lighthouse.baseline.json` in the frontend workspace.
It contains medians, ranges and measurement context, without individual report
paths. Commit it only if the team intends to reuse this measurement environment;
otherwise use a baseline path outside the repository:

```powershell
npm run audit:lighthouse:baseline -- --baseline=../.artifacts/lighthouse-baseline.json
npm run audit:lighthouse:check -- --baseline=../.artifacts/lighthouse-baseline.json
```

Check and baseline commands read saved summaries without launching Chrome or
building. `--summary=<file>` selects a historical summary; `--reports-dir=<dir>`
sets its report root. `--config=<file>` and `--baseline=<file>` select policy and
reference files. Check writes `summary.checked.html` next to the selected JSON,
prints actionable failures and returns exit code 1 for a violation. It never
updates the baseline. A missing baseline fails with setup instructions.

Checks compare matching route/device pairs and allow a candidate subset of a
larger baseline. Newly added pages require a reviewed baseline containing those
pages. Baseline comparisons require at least three valid samples per case and
matching Lighthouse version, Chrome major, OS/architecture, CPU, RAM, target
mode/origin/API and device settings. After a browser/tool update or environment
change, collect a new comparable pair or review a new baseline explicitly.

## Policy and limits

`lighthouse.public.config.json` owns route discovery, smoke selection, run count,
budgets and regression tolerances. Initial budgets are a repository policy:

| Device  | Minimum performance | Maximum LCP | Maximum TBT | Maximum CLS |
| ------- | ------------------- | ----------- | ----------- | ----------- |
| Mobile  | 85                  | 3500 ms     | 300 ms      | 0.1         |
| Desktop | 90                  | 2000 ms     | 200 ms      | 0.1         |

For each configured regression metric, a worsening median fails when its change
exceeds the **larger** absolute or relative allowance. Defaults: performance
5 points; LCP 300 ms / 15%; FCP 200 ms / 15%; TBT 75 ms / 25%; CLS 0.03;
transfer 50 KiB / 10%. Improvements pass. Accessibility, best practices and SEO
are reported; add their minimum score budgets if needed. Never lower policy
silently to make a failing change pass.

These are laboratory checks. A three-run median reduces noise but does not
eliminate it. Review the range and repeat an unexpected regression under matching
conditions. Lighthouse scores can vary with machine load and network-dependent
assets, including fonts. See [Chrome's performance scoring guide](https://developer.chrome.com/docs/lighthouse/performance/performance-scoring).

The budget is not a claim of passing Core Web Vitals. Field LCP/CLS use real
visits at the 75th percentile; Lighthouse TBT does not measure field INP.
After publishing, inspect real-user data separately. See
[LCP measurement guidance](https://web.dev/articles/lcp) and
[Core Web Vitals](https://web.dev/articles/vitals).

Deterministic discovery, summary, budget, baseline, CLI and HTTP transport tests
run through `npm run test:performance-build` and `npm run verify`. They do not
launch Chrome or perform the expensive public-page collection.
