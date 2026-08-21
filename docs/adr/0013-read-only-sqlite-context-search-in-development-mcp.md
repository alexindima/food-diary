# ADR 0013: Read-only SQLite Context Search in the Development MCP

- Status: Superseded by ADR 0014
- Date: 2026-08-21
- Owners: Developer experience
- Related: ADR 0014
- Supersedes: None

## Context

The LLM Wiki publishes a reconstructable SQLite FTS5 projection for natural-language code and documentation discovery. The first shadow reader invoked a Node process through PowerShell for every query. That route proved ranking quality without changing the JSON-authoritative workflow, but repeated process startup made it unsuitable for the interactive Development MCP path.

Replacing JSON context immediately would also conflate two separate decisions: reading the derived database efficiently and treating its candidate set as authoritative. The retrieval corpus did not yet meet the quality required for the latter.

## Decision Drivers

- Interactive MCP requests should not launch a Node and PowerShell process solely to read an existing database.
- The database must remain a disposable cache rebuilt by the existing Wiki writer.
- Ranking behavior must stay comparable between the Node diagnostic path and the MCP path.
- Sensitive source content must not be added merely to improve natural-language matching.
- A reader failure must not make the JSON-authoritative development context unavailable.

## Considered Options

1. Keep the PowerShell/Node subprocess as the only SQL reader.
2. Let the Development MCP read the database directly and keep SQL in shadow mode.
3. Replace JSON context selection with direct SQLite results immediately.

## Decision

Use `Microsoft.Data.Sqlite` in the Development MCP to read the existing SQLite database in-process and read-only.

- The Node graph manager remains the only builder and writer. The MCP does not create, migrate, refresh, or repair the database.
- The MCP opens the database with `Mode=ReadOnly`, a bounded command timeout, and the provider's default cache mode. It does not enable SQLite shared-cache mode alongside WAL.
- Node and .NET readers consume the same committed ranking policy. The policy may expand privacy and provider intent terms and apply deterministic path boosts.
- Query expansion does not justify indexing arbitrary C# string literals or runtime values. Existing symbol, path, documentation, and explicitly supported source projections remain the indexed surface.
- SQL results are attached as shadow diagnostics. They do not change JSON-selected scope, checks, or source-of-truth claims.
- Missing, stale, inaccessible, or invalid SQL projections return an unavailable shadow result. Cancellation still propagates.
- Promotion requires the committed corpus to meet its separate `switchCriteria`; passing the lower regression thresholds is not sufficient.

## Consequences

### Positive

- A batch of context queries avoids repeated PowerShell and Node process startup.
- Both SQL readers can be compared against the same 30-case corpus and policy.
- JSON remains a safe fallback while retrieval gaps are visible and measurable.
- The MCP adds no write contention or new database lifecycle owner.

### Negative

- The MCP carries the bundled SQLite native dependency.
- Ranking logic exists in Node and C# and requires parity tests plus corpus comparison.
- Until switch criteria pass, both JSON and SQL context paths continue to exist.

## Enforcement

- `SqliteWikiContextSearchTests` verifies read-only availability behavior, privacy-intent ranking, provider-intent ranking, and telemetry.
- `WikiQueryServiceTests` verifies that the SQL shadow cannot alter JSON-selected scope.
- `.llm-wiki/evals/context-search.json` contains regression thresholds and independent switch criteria.
- `Test-LlmWikiSqlContextEvaluation.ps1` requires at least 30 representative cases.
- `--evaluate-context-search` exercises the in-process reader against the same corpus used by the Node reader.

## Follow-up

Improve the remaining corpus misses. Promote SQL authority only in a later change after the committed switch criteria pass and a consumer scan identifies which compatibility JSON reads can be removed.

ADR 0014 completed that promotion for Development MCP context selection while retaining a bounded JSON fallback.
