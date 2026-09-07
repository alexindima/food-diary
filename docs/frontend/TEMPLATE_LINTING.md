# Angular template linting

`npm run lint` first checks that the configured Angular template rules actually
report a known-invalid template. Separate bundled compiler instances can cause
rules that use `instanceof` to silently miss parser nodes; a successful lint run
must not depend on that installation accident.

The Angular ESLint 22.2 upgrade exposed existing template complexity. The
`FoodDiary.Web.Client/eslint.template-complexity-baseline.json` file records the
measured limits for 27 existing templates on September 7, 2026. Each entry caps
that template at its existing complexity; the default limit remains five for
all other templates. Reduce or remove entries as components are simplified.
Do not add entries or increase limits merely to pass CI.

Other reported violations are fixed in source: use typed template references
instead of `$any`, built-in uppercase pipes, template literals, and simple
expressions. The baseline applies only to cyclomatic complexity.
