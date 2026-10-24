# FoodDiary WebQuality

An independent Node CLI for adopting published Google Modern Web Guidance with
explicit source provenance and real execution evidence. No server, database,
additional npm dependencies, AI service or second Angular installation is needed.

From the repository root:

```powershell
node Tooling/WebQuality/cli.mjs help
node Tooling/WebQuality/cli.mjs validate
node --test Tooling/WebQuality/test/*.test.mjs
node Tooling/WebQuality/cli.mjs audit --profile source
```

Install the frontend's locked dependencies before tests or audits. `validate`
and catalogue browsing work without installed dependencies or network access.

See [the operating guide](../../docs/frontend/WEB_QUALITY.md) for browser,
authenticated journey and Lighthouse runs, upstream updates, policy semantics,
scope limitations and the CI workflow.

The initial profile maps **31 requirements** across eight published guides and
indexes all **192 guides** in Google version **0.0.195**. Those eight guides are
partially mapped. The remaining guides are explicitly unreviewed. This is a
maintained adoption profile, not a certification of complete conformance.
