using System.ComponentModel;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Protocol;
using FoodDiary.Development.Mcp.Wiki;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FoodDiary.Development.Mcp.Tools;

[McpServerToolType]
public sealed class WikiTools(WikiQueryService queries, IServerStatusService statusService) {
    [McpServerTool(Name = "get_change_context", ReadOnly = true, Idempotent = true)]
    [Description("Builds a source-linked FoodDiary change brief from the repository wiki. The wiki is navigation; verify conclusions in its declared authoritative sources.")]
    public Task<CallToolResult> GetChangeContextAsync(
        [Description("The intended code or architecture change.")] string intent,
        [Description("Optional likely repository path used to focus the result.")] string? plannedPath = null,
        [Description("Return the complete Wiki brief instead of the compact default summary.")] bool includeDetailedContext = false,
        [Description("Include verbose raw Wiki output for diagnostics. Defaults to false.")] bool includeRawOutput = false,
        CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(
            async () => {
                WikiCommandResult result = await queries
                    .GetChangeContextAsync(intent, plannedPath, !includeDetailedContext, cancellationToken)
                    .ConfigureAwait(false);
                if (!includeDetailedContext) {
                    result = result.ToCompactChangeContext(includeRawOutput: includeRawOutput);
                }
                return includeRawOutput ? result : result.WithoutRawOutput();
            },
            cancellationToken);

    [McpServerTool(Name = "trace_backend_flow", ReadOnly = true, Idempotent = true)]
    [Description("Traces an existing FoodDiary backend command, query, route, or feature through source-linked wiki indexes.")]
    public Task<CallToolResult> TraceBackendFlowAsync(
        [Description("Command, query, route, handler, or feature to trace.")] string query,
        [Description("Include verbose raw Wiki output for diagnostics. Defaults to false.")] bool includeRawOutput = false,
        CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(
            async () => {
                WikiCommandResult result = await queries
                    .TraceBackendFlowAsync(query, cancellationToken)
                    .ConfigureAwait(false);
                return includeRawOutput ? result : result.WithoutRawOutput();
            },
            cancellationToken);

    [McpServerTool(Name = "get_test_plan", ReadOnly = true, Idempotent = true)]
    [Description("Builds the focused FoodDiary verification plan for the current change set, optionally narrowed by an intent.")]
    public Task<CallToolResult> GetTestPlanAsync(
        [Description("Optional change intent used to focus the test plan.")] string? intent = null,
        [Description("Optional planned repository paths used when no explicit or Git changes exist.")] string[]? plannedPaths = null,
        [Description("Optional explicit changed repository paths. These take precedence over the Git worktree.")] string[]? changedPaths = null,
        [Description("Include verbose raw Wiki output for diagnostics. Defaults to false.")] bool includeRawOutput = false,
        CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(
            async () => {
                WikiCommandResult result = await queries
                    .GetTestPlanAsync(intent, plannedPaths, changedPaths, cancellationToken)
                    .ConfigureAwait(false);
                return includeRawOutput ? result : result.WithoutRawOutput();
            },
            cancellationToken);

    [McpServerTool(Name = "get_development_context", ReadOnly = true, Idempotent = true)]
    [Description("Runs change context, backend trace, and focused test planning concurrently against one immutable Git/worktree snapshot.")]
    public Task<CallToolResult> GetDevelopmentContextAsync(
        [Description("The intended code or architecture change.")] string intent,
        [Description("The backend command, query, route, handler, or feature to trace.")] string query,
        [Description("Optional likely repository path used to focus the result.")] string? plannedPath = null,
        [Description("Include verbose raw Wiki output for diagnostics. Defaults to false.")] bool includeRawOutput = false,
        CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(
            async () => {
                DevelopmentContext result = await queries
                    .GetDevelopmentContextAsync(intent, query, plannedPath, cancellationToken)
                    .ConfigureAwait(false);
                return includeRawOutput ? result : result.WithoutRawOutput();
            },
            cancellationToken);

    [McpServerTool(Name = "get_server_status", ReadOnly = true, Idempotent = true)]
    [Description("Returns FoodDiary Development MCP, repository, Git, wiki, and generated-index diagnostics without changing repository state.")]
    public Task<CallToolResult> GetServerStatusAsync(CancellationToken cancellationToken) =>
        ToolExecution.RunToolAsync(
            () => statusService.GetStatusAsync(cancellationToken),
            cancellationToken);
}
