# Temporary dependency constraints

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

Reviewed on 2026-10-15. `Directory.Packages.props` sets
`System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` to
`10.0.13`. Explicit security references constrain their versions at the existing
parent consumers:

- `Pkcs`: Admin infrastructure and the BugTriage, MailInbox, and MailRelay
  infrastructure projects that reference MailKit.
- `Xml`: full persistence composition, Identity infrastructure, and Wearables
  infrastructure that reference ASP.NET Core DataProtection.

Dependent hosts and tests inherit these constraints. Global central transitive
pinning remains disabled so unrelated central package versions do not change
transitive resolutions.

The architecture guards retain their exact package sets and explicitly permit
these security references in full persistence composition and mail service
infrastructure. Remove those exact guard entries with the corresponding
references when the parent packages supply patched versions themselves.

The current dependency graph brings in `Pkcs` through `MimeKit 4.17.0` and
`Xml`, and brings in `Xml` through `Microsoft.AspNetCore.DataProtection
10.0.11`. The packages reach mail services, Admin infrastructure, application
hosts, and their tests. NuGet reports denial-of-service vulnerabilities in
versions through `10.0.12`:

- `Pkcs`: [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7)
  and [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx).
- `Xml`: [GHSA-7hqp-7c5m-4vq6](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6),
  [GHSA-c399-9853-6jmj](https://github.com/advisories/GHSA-c399-9853-6jmj), and
  [GHSA-r955-8q5j-3h83](https://github.com/advisories/GHSA-r955-8q5j-3h83).

Both packages have fixes in `10.0.13`. Even `MimeKit 4.18.1`, the latest stable
release at this review, still declares `Pkcs 10.0.0`. The two transitive pins
keep the remediation bounded while preserving current parent dependency
versions and vulnerability enforcement.

Remove each security reference and its central version after updating all of
its parent dependencies so a fresh
resolution without the pin selects a patched version on every target framework.
Verify the regenerated lockfiles, a transitive NuGet vulnerability audit, and
the existing consumer tests before removal. A package audit alone does not establish
whether an application exposes a vulnerable operation.
