# OpenFoodFacts Wiki-first extraction report

## Useful signals

- `research` ranked the explicit Products consumer, Integrations adapter, cache ports, provider tests, and domain tests as current-source evidence.
- Git precedent discovery found recent vertical-module extractions, which correctly supported preserving CLR/EF identity and keeping the shared DbContext, migrations, and snapshot central.
- `privacy` identified the product image URL and nutrition fields as review leads; code verification confirmed catalog metadata rather than user-private payloads.
- The governed design checkpoint forced an explicit record of the provider, persistence, consumer, and compatibility boundaries before edits.

## False positives and over-broad results

- The initial `start` planned 41 paths, including unrelated AI, authentication, billing, and MailRelay files. None were authoritative for OpenFoodFacts extraction.
- The first `brief` rated the change low-risk with score 0 and `test-plan` returned zero tests because the commands did not ground the supplied query as intent/planned paths. This contradicted the cross-project/EF/provider scope and was rejected.
- `privacy` classified public provider image URLs as private content and nutrient values as health data without proving user linkage. These were treated as leads only.
- Before regeneration, `topology` returned no HTTP clients even though `OpenFoodFactsService` issues external HTTP requests. After `wiki update`, the same JSON-baseline query found the typed client and its timeout/concurrency/network-policy signals, showing that the generated baseline was stale rather than the adapter being undiscoverable.

## False negatives

- The JSON baseline did not surface the dual-cache lifecycle: durable PostgreSQL cache in the module and bounded fresh/stale in-memory provider-response cache in Integrations.
- It did not identify the single-flight refresh, concurrency gate, in-flight cap, explicit search timeout, stale fallback, or cancellation distinction that provider tests cover.
- It did not infer that Products should depend on a Contracts project instead of the application implementation.
- It did not identify central EF model-builder registration as the seam required to preserve model identity after moving the configuration assembly.

## General Wiki defects and disposition

- TypeScript prerequisites were unavailable, so all required commands used the read-only JSON baseline; runtime-flow output was explicitly not rated.
- Post-diff `ownership` and `api-compat` could not complete: both require the SQLite compiled projection/full graph, whose build is blocked until the frontend TypeScript prerequisites are installed with `npm ci`.
- Query-only `brief`/`test-plan` invocation can silently produce empty, misleading output instead of requesting grounding.
- Runtime topology omits ordinary external `HttpClient` adapters unless they match registered topology declarations.
- No generator change was made: the observed limitations require evidence across multiple modules before changing shared Wiki tooling.
