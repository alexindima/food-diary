# Development MCP Guidelines

## Scope

Rules for `FoodDiary.Development.Mcp/`.

## Role

- Expose bounded, read-only development context through MCP.
- Treat Git-backed source, tests, ADRs, current documentation, and scoped `AGENTS.md` files as authority; Wiki output is derived navigation.
- Keep the Node code-graph manager as the only writer of the SQLite projection.

## Rules

- MCP tools must not expose Wiki generation, governed task mutation, delivery, repair, or repository write operations.
- Preserve explicit stale, unavailable, ambiguous, and partial-result states. Never replace missing evidence with an inferred successful answer.
- Validate the exact repository/worktree fingerprint before accepting cached or indexed context.
- Keep queries and private path payloads out of persisted telemetry; record only bounded aggregate routing and timing data.
- Preserve cancellation, timeout, output-size, and process-tree termination safeguards for PowerShell subprocesses.
- Keep structured MCP contracts backward compatible. When a contract changes, update protocol types, tool mappings, README documentation, and focused tests together.
- Use the in-process read-only SQLite path for interactive context selection. Recovery may refresh through the existing graph writer but must not silently fall back to stale JSON.

## Commands

- Build: `dotnet build FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj --artifacts-path .artifacts/development-mcp`
- Tests: `dotnet test tests/FoodDiary.Development.Mcp.Tests/FoodDiary.Development.Mcp.Tests.csproj --artifacts-path .artifacts/development-mcp-tests`
- Start: `./scripts/Start-FoodDiaryDevelopmentMcp.cmd --build-if-stale`
