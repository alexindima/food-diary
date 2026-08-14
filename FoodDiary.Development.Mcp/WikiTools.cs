using System.ComponentModel;
using ModelContextProtocol.Server;

namespace FoodDiary.Development.Mcp;

[McpServerToolType]
public sealed class WikiTools(WikiQueryService queries) {
    [McpServerTool(Name = "get_change_context", ReadOnly = true, Idempotent = true)]
    [Description("Builds a source-linked FoodDiary change brief from the repository wiki. The wiki is navigation; verify conclusions in its declared authoritative sources.")]
    public Task<WikiCommandResult> GetChangeContextAsync(
        [Description("The intended code or architecture change.")] string intent,
        [Description("Optional likely repository path used to focus the result.")] string? plannedPath,
        CancellationToken cancellationToken) =>
        queries.GetChangeContextAsync(intent, plannedPath, cancellationToken);

    [McpServerTool(Name = "trace_backend_flow", ReadOnly = true, Idempotent = true)]
    [Description("Traces an existing FoodDiary backend command, query, route, or feature through source-linked wiki indexes.")]
    public Task<WikiCommandResult> TraceBackendFlowAsync(
        [Description("Command, query, route, handler, or feature to trace.")] string query,
        CancellationToken cancellationToken) =>
        queries.TraceBackendFlowAsync(query, cancellationToken);

    [McpServerTool(Name = "get_test_plan", ReadOnly = true, Idempotent = true)]
    [Description("Builds the focused FoodDiary verification plan for the current change set, optionally narrowed by an intent.")]
    public Task<WikiCommandResult> GetTestPlanAsync(
        [Description("Optional change intent used to focus the test plan.")] string? intent,
        CancellationToken cancellationToken) =>
        queries.GetTestPlanAsync(intent, cancellationToken);
}
