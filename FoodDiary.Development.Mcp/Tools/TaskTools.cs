using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FoodDiary.Development.Mcp.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FoodDiary.Development.Mcp.Tools;

[McpServerToolType]
[ExcludeFromCodeCoverage]
public sealed class TaskTools(ITaskToolExecutor executor) {
    [McpServerTool(Name = "get_next_task_action", ReadOnly = true, Idempotent = true)]
    [Description("Returns the next unresolved canonical check, active job, stale proof or remaining review action from an existing task-owned Wiki evidence bundle.")]
    public Task<CallToolResult> GetNextTaskActionAsync(string evidencePath, CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(() => executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.Next, EvidencePath: evidencePath), cancellationToken), cancellationToken);

    [McpServerTool(Name = "get_task_diagnostics", ReadOnly = true, Idempotent = true)]
    [Description("Reads bounded sanitized check failures, job status and cached browser observations with their timestamps plus current owned server logs. Does not log in or refresh browser observations. Paths are constrained to this checkout's task artifacts.")]
    public Task<CallToolResult> GetTaskDiagnosticsAsync(string? evidencePath = null, string? jobId = null, string? runtimeName = null, CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(() => executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.Diagnostics, EvidencePath: evidencePath, JobId: jobId, RuntimeName: runtimeName), cancellationToken), cancellationToken);

    [McpServerTool(Name = "collect_task_runtime_diagnostics", ReadOnly = false, Idempotent = false, Destructive = false)]
    [Description("Refreshes browser/API diagnostics on one owned local runtime using its synthetic account. Authentication stores a local session and the observed events are written to the runtime artifact. Returns sanitized failures and source freshness; never accepts arbitrary URLs or credentials.")]
    public Task<CallToolResult> CollectTaskRuntimeDiagnosticsAsync(string runtimeName, CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(() => executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.CollectRuntime, RuntimeName: runtimeName), cancellationToken), cancellationToken, readOnly: false);

    [McpServerTool(Name = "verify_task", ReadOnly = false, Idempotent = false, Destructive = false)]
    [Description("Starts one canonical allowlisted check from an existing Wiki evidence plan. Writes task-owned evidence/logs, returns a job ID immediately and refuses concurrent checks on the same bundle. Poll get_task_diagnostics; this does not complete or publish the task.")]
    public Task<CallToolResult> VerifyTaskAsync(string evidencePath, string checkId, int timeoutMinutes = 20, CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(() => executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.Verify, EvidencePath: evidencePath, CheckId: checkId, TimeoutMinutes: timeoutMinutes), cancellationToken), cancellationToken, readOnly: false);

    [McpServerTool(Name = "cancel_task_check", ReadOnly = false, Idempotent = true, Destructive = false)]
    [Description("Requests cancellation of one verification job owned by this checkout. The worker checks process lifetime identity before stopping its own process tree. Poll diagnostics for terminal status.")]
    public Task<CallToolResult> CancelTaskCheckAsync(string jobId, CancellationToken cancellationToken = default) =>
        ToolExecution.RunToolAsync(() => executor.ExecuteAsync(new TaskToolRequest(TaskToolAction.Cancel, JobId: jobId), cancellationToken), cancellationToken, readOnly: false);
}
