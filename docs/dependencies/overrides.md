# Temporary dependency constraints

## Stylelint SCSS compatibility

Reviewed on 2026-11-09. The web workspace uses Stylelint `^17.16.0` and
`stylelint-config-standard ^40.0.0`. The current SCSS preset and plugin require
Stylelint 17, while standard config 41 requires Stylelint 18. Installing the
newer pair with the current SCSS preset caused `npm ci` to fail with `ERESOLVE`.
[SCSS preset peer range](https://github.com/stylelint-scss/stylelint-config-standard-scss/blob/v17.0.0/package.json)
[SCSS plugin peer range](https://github.com/stylelint-scss/stylelint-scss/blob/v7.4.0/package.json)

Dependabot skips major version updates for the two constrained packages. Remove
the exclusions when the SCSS preset and plugin both declare support for the next
Stylelint major. Update the compatible set together, then run a clean `npm ci`
and `npm run stylelint` to exercise the SCSS preset and local rules.

## npm minimatch and mobile glob

Reviewed on 2026-11-01. The web workspace overrides `minimatch` to `11.0.3`
for [GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249).
All versions before `11.0.1` are affected by pattern amplification and quadratic
preprocessing at `optimizationLevel: 2`. The previous graph contained both
`10.2.6` and a nested `10.2.5` pinned by `ng-morph`.

The web consumers are ESLint, `@eslint/config-array`,
`@typescript-eslint/typescript-estree`, `eslint-plugin-sonarjs`, `ng-morph`,
`multimatch`, and `@ts-morph/common`. Their installed callers use the retained
`minimatch()`, `minimatch.match()`, and `Minimatch.match()` APIs. None uses the
removed `makeRe()` or `AST` API or assumes that compiled pattern parts are
`RegExp` objects. Current parent releases still require minimatch 10, so a
parent-only update cannot resolve the complete graph.

The mobile workspace instead overrides only `rimraf`'s `glob` dependency to
`14.0.3`, which brings minimatch 11 and also fixes
[GHSA-3g3m-2fjr-6pw4](https://github.com/advisories/GHSA-3g3m-2fjr-6pw4).
Latest `rimraf 6.1.3` still requests glob 13. Overriding minimatch alone under
glob 13 would break its assumptions about `RegExp` pattern parts. Rimraf uses
the retained public `glob()` and `globSync()` APIs, with `withFileTypes: false`.
Capacitor CLI's observed rimraf calls use literal paths without enabling glob.

These are development dependencies. No FoodDiary application endpoint accepting
patterns for these consumers was identified. Storybook 10.6.3 separately bundles
an older glob/minimatch implementation; npm overrides do not replace vendored
code. Its observed inputs are local configuration and package scanning, with no
demonstrated application-user attack path. A clean npm audit covers the installed
package graph, not that bundled implementation.

Remove each override after every affected parent supports a patched matcher in
a fresh resolution without it. Recheck all nested copies and both npm audits.
Verify bounded preprocessing in CommonJS and ESM, ordinary and Windows/partial
matching, actual ESLint/import-boundary and ng-morph consumers, web builds, and
mobile rimraf literal/glob operations plus cancelled glob traversal. Retain the
other constraints until their own documented security or compatibility conditions
are satisfied; this update does not establish that they can safely be removed.

## Angular build Vite override

Reviewed on 2026-10-22. `@angular/build 22.2.3`, still the latest release,
requires exactly `vite 8.3.0`. The web workspace's scoped `@angular/build`
override uses `$vite` to share its direct `vite ^8.3.4` dependency, locked to
8.3.4, instead of retaining a vulnerable nested installation. This stays within
Vite 8.3 and does not override unrelated consumers. TechRadar separately raises
its direct Vite minimum to `^8.2.4`, locked to the patched 8.2.4 release.

The affected Vite versions bundle vulnerable development-server code:

- Editor launch on Windows and WSL:
  [existence-check bypass](https://github.com/advisories/GHSA-587h-7wmj-h5pm),
  [Windows percent expansion](https://github.com/advisories/GHSA-698v-4r3x-3vcv),
  and [editor argument injection](https://github.com/advisories/GHSA-r825-w8mm-5pxw).
- [Custom HTML transform script injection](https://github.com/advisories/GHSA-9jrq-w75r-8gcw),
  [non-Windows filesystem allow bypass](https://github.com/advisories/GHSA-rq7h-c2jc-7f22),
  and [denied-file WASM error disclosure](https://github.com/advisories/GHSA-vfpm-58rq-9qcg).

Angular's dev server loads Vite through `@angular/build`; a patched root Vite
alone did not repair that nested copy. TechRadar's development command exposes
Vite with `--host 0.0.0.0`. These advisories concern development tooling;
installation does not by itself prove every attack prerequisite or an affected
production runtime.

Remove the scoped override after updating Angular build to a compatible release
whose dependency declaration resolves patched Vite without it. Check a fresh
resolution and every nested copy, both npm audits, harmless advisory probes,
Angular development serving and tests, client/admin and Storybook builds, and
the TechRadar build. Keep the web workspace's direct safe Vite minimum while
other direct Vite consumers still require it.

## Removed qs override

Removed on 2026-10-25. The web workspace's global `qs 6.16.0` override
originated in dependency-security commit `b122206c83`. It is now unnecessary:
the only consumers are `express 5.3.0`, which requires `^6.16.0`, and
`body-parser 2.4.0`, which requires `^6.16.1`. Keeping the old override would
force the latter below its declared minimum.

The body-parser update fixes
[GHSA-27r3-jxmh-p828](https://github.com/advisories/GHSA-27r3-jxmh-p828).
A fresh isolated resolution of both exact parent versions without an override
selects a single `qs 6.16.1`; the retained application lockfile uses that same
version. Verify all copies, npm audit, nested-query/array-limit/prototype controls,
and compressed JSON parsing when updating these parents. Keep the override
removed while every consumer's declared range resolves a safe version.

## npm braces depth mitigation

Reviewed on 2026-10-17. The web workspace resolves `braces 3.0.3` through
`micromatch 4.0.8`, consumed by `eslint-plugin-boundaries 7.2.0` and
`stylelint 17.16.0` (including fast-glob/globby). These are development tools;
no application endpoint accepting user-provided glob patterns was identified.
[GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm)
currently lists no patched version, and npm still publishes 3.0.3 as latest.

`FoodDiary.Web.Client/scripts/apply-dependency-patches.mjs` applies a temporary
local patch after installation. It verifies the upstream source hashes and
version of every lockfile-listed installation before writing. Parser nesting
and direct AST walkers reject depths above 128, with an iterative cycle/depth
check before recursive execution. The patch is idempotent and fails on upstream
source drift. CI jobs using `--ignore-scripts` invoke it explicitly; the client
Dockerfile copies the installer before `npm ci`. Lint, Stylelint and the default
build check that the patch is installed.

Run `npm run test:dependency-patches` for deep brace/parenthesis, unclosed-pattern,
direct AST and legitimate range/nested-alternative/matcher regressions. Import
boundary tests, ESLint, Stylelint and the frontend build exercise actual consumers.
The lockfile keeps the upstream version and integrity: `npm audit` continues to
report the advisory and its dependent packages. This is a local mitigation, not
an upstream resolution or an alert dismissal.

Remove the patch, lifecycle/check scripts and explicit CI/Docker installation
steps after every parent resolves an official compatible fix. Verify all nested
copies, a fresh installation, the advisory probes, consumer checks and npm audit
before removal. Existing unrelated npm overrides and NuGet security pins remain
unchanged.

## NuGet cryptography packages

`Directory.Packages.props` pins `System.Security.Cryptography.Xml` to `10.0.13`.
Full persistence composition, Identity infrastructure, and Wearables infrastructure
reference it explicitly because they use ASP.NET Core DataProtection `10.0.11`.
The patched version covers [GHSA-7hqp-7c5m-4vq6](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6),
[GHSA-c399-9853-6jmj](https://github.com/advisories/GHSA-c399-9853-6jmj), and
[GHSA-r955-8q5j-3h83](https://github.com/advisories/GHSA-r955-8q5j-3h83).

MailKit `4.18.2` brings in MimeKit `4.18.2`, which requires
`System.Security.Cryptography.Pkcs` `10.0.13` or later. This covers
[GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7) and
[GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx)
for consumers that resolve Pkcs as a package. The .NET 11 ASP.NET Core shared
framework supplies Pkcs for consumers that reference it.

Remove the XML pin and its exact architecture guard entry once the parent
packages select a patched version on every target framework. Verify the
regenerated lockfiles, transitive NuGet audit, and consumer tests.
