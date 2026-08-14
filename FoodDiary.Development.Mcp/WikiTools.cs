using System.ComponentModel;
using ModelContextProtocol.Server;

namespace FoodDiary.Development.Mcp;

[McpServerToolType]
public sealed class WikiTools(WikiQueryService queries, IServerStatusService statusService) {
    [McpServerTool(Name = "get_change_context", ReadOnly = true, Idempotent = true)]
    [Description("Builds a source-linked FoodDiary change brief from the repository wiki. The wiki is navigation; verify conclusions in its declared authoritative sources.")]
    public Task<DevelopmentMcpResult<WikiCommandResult>> GetChangeContextAsync(
        [Description("The intended code or architecture change.")] string intent,
        [Description("Optional likely repository path used to focus the result.")] string? plannedPath,
        CancellationToken cancellationToken) =>
        ToolExecution.RunAsync(
            () => queries.GetChangeContextAsync(intent, plannedPath, cancellationToken),
            cancellationToken);

    [McpServerTool(Name = "trace_backend_flow", ReadOnly = true, Idempotent = true)]
    [Description("Traces an existing FoodDiary backend command, query, route, or feature through source-linked wiki indexes.")]
    public Task<DevelopmentMcpResult<WikiCommandResult>> TraceBackendFlowAsync(
        [Description("Command, query, route, handler, or feature to trace.")] string query,
        CancellationToken cancellationToken) =>
        ToolExecution.RunAsync(
            () => queries.TraceBackendFlowAsync(query, cancellationToken),
            cancellationToken);

    [McpServerTool(Name = "get_test_plan", ReadOnly = true, Idempotent = true)]
    [Description("Builds the focused FoodDiary verification plan for the current change set, optionally narrowed by an intent.")]
    public Task<DevelopmentMcpResult<WikiCommandResult>> GetTestPlanAsync(
        [Description("Optional change intent used to focus the test plan.")] string? intent,
        CancellationToken cancellationToken) =>
        ToolExecution.RunAsync(
            () => queries.GetTestPlanAsync(intent, cancellationToken),
            cancellationToken);

    [McpServerTool(Name = "get_development_context", ReadOnly = true, Idempotent = true)]
    [Description("Runs change context, backend trace, and focused test planning concurrently against one immutable Git/worktree snapshot.")]
    public Task<DevelopmentMcpResult<DevelopmentContext>> GetDevelopmentContextAsync(
        [Description("The intended code or architecture change.")] string intent,
        [Description("The backend command, query, route, handler, or feature to trace.")] string query,
        [Description("Optional likely repository path used to focus the result.")] string? plannedPath,
        CancellationToken cancellationToken) =>
        ToolExecution.RunAsync(
            () => queries.GetDevelopmentContextAsync(intent, query, plannedPath, cancellationToken),
            cancellationToken);

    [McpServerTool(Name = "get_server_status", ReadOnly = true, Idempotent = true)]
    [Description("Returns FoodDiary Development MCP, repository, Git, wiki, and generated-index diagnostics without changing repository state.")]
    public Task<DevelopmentMcpResult<ServerStatus>> GetServerStatusAsync(
        CancellationToken cancellationToken) =>
        ToolExecution.RunAsync(
            () => statusService.GetStatusAsync(cancellationToken),
            cancellationToken);
}
