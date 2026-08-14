namespace FoodDiary.Development.Mcp;

public sealed class WikiQueryService(IWikiCommandExecutor executor) {
    public Task<WikiCommandResult> GetChangeContextAsync(
        string intent,
        string? plannedPath,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);

        List<string> arguments = ["-Intent", intent];
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            arguments.Add("-PlannedPath");
            arguments.Add(plannedPath);
        }

        return executor.ExecuteAsync("brief", arguments, cancellationToken);
    }

    public Task<WikiCommandResult> TraceBackendFlowAsync(
        string query,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return executor.ExecuteAsync("trace", ["-Query", query], cancellationToken);
    }

    public Task<WikiCommandResult> GetTestPlanAsync(
        string? intent,
        CancellationToken cancellationToken) {
        IReadOnlyList<string> arguments = string.IsNullOrWhiteSpace(intent)
            ? []
            : ["-Intent", intent];

        return executor.ExecuteAsync("test-plan", arguments, cancellationToken);
    }
}
