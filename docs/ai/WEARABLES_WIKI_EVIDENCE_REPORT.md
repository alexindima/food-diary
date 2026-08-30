# Wearables extraction: Wiki evidence report

## Useful signals

- Root Wiki routing correctly identified Wearables as an aggregate owner and pointed to the application, abstractions, domain, persistence, Integrations adapter, presentation, and tests.
- Privacy classified `CaloriesBurned` as health data and reinforced the need to treat summaries as sensitive.
- Decision correctly found no mandatory ADR trigger: the extraction follows existing accepted module precedents and preserves runtime/EF contracts.

## False positives and false negatives

- `test-plan` returned zero focused files, commands, and scenarios. Current sources contained focused application, domain, infrastructure, integration, presentation, host-token, architecture, and provider-adapter tests.
- Scoped `topology` returned no HTTP clients or background registrations although `FoodDiary.Integrations/Wearables/FitbitClient.cs` is a typed external HTTP adapter. Direct code verification also established that no recurring wearable background job is registered.
- Privacy found `CaloriesBurned` but missed steps, distance, provider external-user identity, access/refresh credentials, OAuth state, and synchronization history.
- The generated contract index retained legacy `FoodDiary.Application.Wearables` and `FoodDiary.Application.Abstractions/Wearables` source paths after files moved.
- JSON fallback research reported zero relevant workspace paths for a query containing exact Wearables concepts.

## General tooling defects

- With TypeScript prerequisites unavailable, Wiki facade commands fall back to the read-only JSON baseline; `start`, `research`, and `brief` can spend longer than the facade response window in Git-precedent workers.
- `task-refresh -CompiledIndexSource Json` still attempted the missing SQLite projection, so the governed acceptance packet could not be refreshed after new module paths were created. The resulting stale packet rejected valid `Modules/Wearables` evidence paths, and `delivery-validate` remained blocked despite successful source, build, test, EF, and Wiki verification evidence.
- These defects are cross-cutting rather than Wearables-specific. No generator was patched without a repeatable fixture proving the correct generic behavior.

All architectural claims used for extraction were revalidated against current `.csproj` references, source, tests, migration snapshot identity, repository docs, and existing extracted-module precedents.
