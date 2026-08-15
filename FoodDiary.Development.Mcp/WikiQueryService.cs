namespace FoodDiary.Development.Mcp;

public sealed class WikiQueryService(
    IWikiCommandExecutor executor,
    IChangeSetSnapshotService snapshots) {
    public async Task<WikiCommandResult> GetChangeContextAsync(
        string intent,
        string? plannedPath,
        bool compact,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);

        List<string> arguments = ["-Format", "Json", "-Objective", intent];
        if (compact) {
            arguments.Add("-Compact");
        }
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            arguments.Add("-ProposedPath");
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
        IReadOnlyList<string>? plannedPaths,
        IReadOnlyList<string>? changedPaths,
        CancellationToken cancellationToken) {
        List<string> arguments = string.IsNullOrWhiteSpace(intent)
            ? ["-Format", "Json"]
            : ["-Format", "Json", "-Objective", intent];
        ChangeSetSnapshot snapshot = await snapshots.GetAsync(cancellationToken).ConfigureAwait(false);
        bool hasChangeScope = AddPaths(arguments, "-ChangedPath", changedPaths);
        if (!hasChangeScope) {
            hasChangeScope = AddChangeSet(arguments, snapshot);
        }
        AddPaths(arguments, "-ProposedPath", plannedPaths);

        if (!HasScope(arguments)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.TestPlanScopeRequired,
                "Provide plannedPaths or changedPaths when the Git worktree is clean.");
        }

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
        List<string> briefArguments = ["-Format", "Json", "-Compact", "-Objective", intent];
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            briefArguments.Add("-ProposedPath");
            briefArguments.Add(plannedPath);
        }
        AddChangeSet(briefArguments, snapshot);

        List<string> testArguments = ["-Format", "Json", "-Fast", "-Objective", intent];
        if (!AddChangeSet(testArguments, snapshot) && !string.IsNullOrWhiteSpace(plannedPath)) {
            testArguments.Add("-ProposedPath");
            testArguments.Add(plannedPath);
        }

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

    private static bool AddChangeSet(List<string> arguments, ChangeSetSnapshot snapshot) {
        if (snapshot.ChangedPaths.Count == 0) {
            return false;
        }

        AddPaths(arguments, "-ChangedPath", snapshot.ChangedPaths);
        return true;
    }

    private static bool AddPaths(
        List<string> arguments,
        string parameter,
        IReadOnlyList<string>? paths) {
        string[] normalized = [.. (paths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        foreach (string path in normalized) {
            arguments.Add(parameter);
            arguments.Add(path);
        }
        return normalized.Length > 0;
    }

    private static bool HasScope(IReadOnlyList<string> arguments) =>
        arguments.Contains("-ChangedPath", StringComparer.Ordinal) ||
        arguments.Contains("-ProposedPath", StringComparer.Ordinal);
}
