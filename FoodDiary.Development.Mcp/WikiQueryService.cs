namespace FoodDiary.Development.Mcp;

public sealed class WikiQueryService(
    IWikiCommandExecutor executor,
    IChangeSetSnapshotService snapshots) {
    public async Task<WikiCommandResult> GetChangeContextAsync(
        string intent,
        string? plannedPath,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);

        List<string> arguments = ["-Format", "Json", "-Intent", intent];
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
        return executor.ExecuteAsync(
            "trace",
            ["-Format", "Json", "-Fast", "-Query", query],
            cancellationToken);
    }

    public async Task<WikiCommandResult> GetTestPlanAsync(
        string? intent,
        CancellationToken cancellationToken) {
        List<string> arguments = string.IsNullOrWhiteSpace(intent)
            ? ["-Format", "Json"]
            : ["-Format", "Json", "-Intent", intent];
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
        List<string> briefArguments = ["-Format", "Json", "-Compact", "-Intent", intent];
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            briefArguments.Add("-PlannedPath");
            briefArguments.Add(plannedPath);
        }
        AddChangeSet(briefArguments, snapshot);

        List<string> testArguments = ["-Format", "Json", "-Fast", "-Intent", intent];
        AddChangeSet(testArguments, snapshot);

        WikiCommandResult brief = await executor.ExecuteAsync(
            "brief",
            briefArguments,
            cancellationToken).ConfigureAwait(false);
        WikiCommandResult trace = await executor.ExecuteAsync(
            "trace",
            ["-Format", "Json", "-Fast", "-Query", query],
            cancellationToken).ConfigureAwait(false);
        WikiCommandResult testPlan = await executor.ExecuteAsync(
            "test-plan",
            testArguments,
            cancellationToken).ConfigureAwait(false);

        return new DevelopmentContext(
            snapshot.Fingerprint,
            snapshot.GitHead,
            brief,
            trace,
            testPlan);
    }

    private static void AddChangeSet(List<string> arguments, ChangeSetSnapshot snapshot) {
        if (snapshot.ChangedPaths.Count == 0) {
            return;
        }

        arguments.Add("-ChangedPathList");
        arguments.Add(string.Join('\n', snapshot.ChangedPaths));
    }
}
