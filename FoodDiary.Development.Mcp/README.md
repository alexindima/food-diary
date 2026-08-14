# FoodDiary Development MCP

Local, read-only stdio MCP server that exposes stable `.llm-wiki/wiki.ps1`
analysis entrypoints without replacing repository source-of-truth checks.

## Tools

- `get_change_context` wraps `wiki.ps1 brief`.
- `trace_backend_flow` wraps `wiki.ps1 trace`.
- `get_test_plan` wraps `wiki.ps1 test-plan`.
- `get_development_context` runs brief, trace, and test planning concurrently
  against one immutable Git/worktree snapshot.
- `get_server_status` reports repository, Git HEAD, Wiki, version, and index health.

The server does not expose governed task lifecycle, generation, delivery, or
repair commands. Wiki output remains derived navigation: callers must verify
change-sensitive conclusions in the referenced code, tests, ADRs, current docs,
and scoped `AGENTS.md` files.

## Run

From the repository root:

```powershell
dotnet build FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj
dotnet run --project FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj --no-launch-profile --no-build
```

The client must launch the process from within the repository tree. When that is
not possible, set `FOODDIARY_REPOSITORY_ROOT` to the absolute repository path.
All protocol traffic uses stdout; host diagnostics use stderr.

The registered client uses `--no-build`, and the project disables the Windows
apphost. Concurrent clients therefore do not trigger competing builds or lock a
generated `.exe`. Build the project after changing or pulling the MCP
implementation, then restart Codex so it launches the updated binary.

The trusted-project `.codex/config.toml` registers this server for Codex. Restart
the desktop app or extension after pulling/building the project.
