namespace FoodDiary.Development.Mcp;

public sealed class WikiQueryService(
    IWikiCommandExecutor executor,
    IChangeSetSnapshotService snapshots) {
    public async Task<WikiCommandResult> GetChangeContextAsync(
        string intent,
        string? plannedPath,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);

        List<string> arguments = ["-Intent", intent];
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            arguments.Add("-PlannedPath");
            arguments.Add(plannedPath);
        }

        AddChangeSet(arguments, await snapshots.GetAsync(cancellationToken).ConfigureAwait(false));

        return await executor.ExecuteAsync("brief", arguments, cancellationToken).ConfigureAwait(false);
    }

    public Task<WikiCommandResult> TraceBackendFlowAsync(
        string query,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return executor.ExecuteAsync("trace", ["-Fast", "-Query", query], cancellationToken);
    }

    public async Task<WikiCommandResult> GetTestPlanAsync(
        string? intent,
        CancellationToken cancellationToken) {
        List<string> arguments = string.IsNullOrWhiteSpace(intent)
            ? []
            : ["-Intent", intent];
        AddChangeSet(arguments, await snapshots.GetAsync(cancellationToken).ConfigureAwait(false));

        return await executor.ExecuteAsync("test-plan", arguments, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DevelopmentContext> GetDevelopmentContextAsync(
        string intent,
        string query,
        string? plannedPath,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        ChangeSetSnapshot snapshot = await snapshots.GetAsync(cancellationToken).ConfigureAwait(false);
        List<string> briefArguments = ["-Intent", intent];
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            briefArguments.Add("-PlannedPath");
            briefArguments.Add(plannedPath);
        }
        AddChangeSet(briefArguments, snapshot);

        List<string> testArguments = ["-Intent", intent];
        AddChangeSet(testArguments, snapshot);

        Task<WikiCommandResult> brief = executor.ExecuteAsync("brief", briefArguments, cancellationToken);
        Task<WikiCommandResult> trace = executor.ExecuteAsync(
            "trace",
            ["-Fast", "-Query", query],
            cancellationToken);
        Task<WikiCommandResult> testPlan = executor.ExecuteAsync(
            "test-plan",
            testArguments,
            cancellationToken);
        await Task.WhenAll(brief, trace, testPlan).ConfigureAwait(false);

        return new DevelopmentContext(
            snapshot.Fingerprint,
            snapshot.GitHead,
            await brief.ConfigureAwait(false),
            await trace.ConfigureAwait(false),
            await testPlan.ConfigureAwait(false));
    }

    private static void AddChangeSet(List<string> arguments, ChangeSetSnapshot snapshot) {
        if (snapshot.ChangedPaths.Count == 0) {
            return;
        }

        arguments.Add("-ChangedPathList");
        arguments.Add(string.Join('\n', snapshot.ChangedPaths));
    }
}
