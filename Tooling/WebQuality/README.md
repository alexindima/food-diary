# FoodDiary WebQuality

An independent Node CLI for adopting published Google Modern Web Guidance with
explicit source provenance and real execution evidence. The CLI core has no
separate dependency installation, database, AI service or Angular application.
Its adapters use the frontend's locked runtime and testing dependencies.

From the repository root:

```powershell
node Tooling/WebQuality/cli.mjs help
node Tooling/WebQuality/cli.mjs validate
node --test Tooling/WebQuality/test/*.test.mjs
node Tooling/WebQuality/cli.mjs audit --profile source
```

Install the frontend's locked dependencies before validation, tests or audits;
the full engine registry uses their parsers. Accepted sources and catalogue
browsing are local data. Policy/source validation works offline after installation;
rendered audits may request application-declared fonts and provider resources.

See [the operating guide](../../docs/frontend/WEB_QUALITY.md) for browser,
authenticated journey and Lighthouse runs, upstream updates, policy semantics,
scope limitations and the CI workflow.

All **192 guides** in Google version **0.0.195** have source-bound decisions:
**152 applicable reviews** and **40 guarded absence decisions**. The current
policy contains **68 requirements**, including seven explicit human/field
assessments. CI automatically runs source checks, actual owner suites, public
checks in three browser engines, client/admin journeys and repeated Lighthouse.
The report distinguishes passing execution, missing evidence and manual work;
review coverage is not a certificate of complete conformance.
