# FoodDiary Development MCP

Local, read-only stdio MCP server that exposes stable `.llm-wiki/wiki.ps1`
analysis entrypoints without replacing repository source-of-truth checks.

## Tools

- `get_change_context` wraps `wiki.ps1 brief`.
- `trace_backend_flow` wraps `wiki.ps1 trace`.
- `get_test_plan` wraps `wiki.ps1 test-plan` and accepts explicit
  `changedPaths` or fallback `plannedPaths` when the worktree is clean.
- `get_development_context` runs a SQLite-backed trace first, then a compact
  brief and fast test plan concurrently. It refreshes the Git/worktree
  fingerprint between phases and rejects the result if the snapshot changed.
- `get_server_status` reports repository, Git HEAD, Wiki, index presence, and
  runtime identity (PID, process start, startup HEAD, assembly MVID/hash, and
  build timestamps). `runningCodeMatchesRepositoryHead` makes a process that
  survived a commit visible. Index content is fingerprinted, while source-to-index
  freshness remains a lightweight timestamp-based diagnostic rather than deep
  regeneration and verification.

The server does not expose governed task lifecycle, generation, delivery, or
repair commands. Wiki output remains derived navigation: callers must verify
change-sensitive conclusions in the referenced code, tests, ADRs, current docs,
and scoped `AGENTS.md` files.

MCP queries request JSON from the Wiki. This enables its snapshot-keyed query
cache; repeated requests against the same Git HEAD and worktree avoid repeating
expensive discovery. The standalone `get_test_plan` remains comprehensive,
while the aggregate context deliberately uses the fast graph plan to stay
within interactive tool timeouts.

Large and Unicode-rich intents/path lists are serialized to a temporary JSON
request file instead of being placed on the Windows command line. Tool results
use MCP `structuredContent`; verbose `rawOutput` and duplicate `outputLines` are
omitted unless `includeRawOutput` is explicitly enabled.

## Run

From the repository root:

```powershell
dotnet build FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj
./scripts/Start-FoodDiaryDevelopmentMcp.cmd --build-if-missing
```

The client must launch the process from within the repository tree. When that is
not possible, set `FOODDIARY_REPOSITORY_ROOT` to the absolute repository path.
All protocol traffic uses stdout; host diagnostics use stderr.

The registered client uses `--build-if-missing`. The common path stays build-free;
after a clean or fresh checkout the launcher builds the missing output once. It
then copies the output to a unique temporary directory before starting each
session, so concurrent MCP clients do not lock the shared `bin` output. Build the
project after changing or pulling an already-built MCP implementation, then
restart Codex so it launches the updated binary. The repository registration marks this server as
required and allows 120 seconds for startup, so startup failures are reported
instead of silently leaving the FoodDiary tools unavailable.

The trusted-project `.codex/config.toml` registers this server for Codex. Restart
the desktop app or extension after pulling/building the project.
