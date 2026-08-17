using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class WikiQueryService(
    IWikiCommandExecutor executor,
    IChangeSetSnapshotService snapshots) {
    public Task<WikiCommandResult> GetTestPlanAsync(
        string? intent,
        IReadOnlyList<string>? plannedPaths,
        IReadOnlyList<string>? changedPaths,
        IReadOnlyList<string>? executedChecks,
        CancellationToken cancellationToken) =>
        GetTestPlanAsync(
            intent,
            plannedPaths,
            changedPaths,
            executedChecks,
            baseRevision: null,
            headRevision: null,
            cancellationToken);

    public Task<DevelopmentContext> GetDevelopmentContextAsync(
        string intent,
        string query,
        string? plannedPath,
        CancellationToken cancellationToken) =>
        GetDevelopmentContextAsync(
            intent,
            query,
            plannedPath,
            baseRevision: null,
            headRevision: null,
            cancellationToken);

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
        IReadOnlyList<string>? executedChecks,
        string? baseRevision,
        string? headRevision,
        CancellationToken cancellationToken) {
        List<string> arguments = string.IsNullOrWhiteSpace(intent)
            ? ["-Format", "Json"]
            : ["-Format", "Json", "-Objective", intent];
        ChangeSetSnapshot snapshot = await snapshots.GetAsync(cancellationToken).ConfigureAwait(false);
        AddRevisionRange(arguments, snapshot, baseRevision, headRevision);
        bool hasChangeScope = AddPaths(arguments, "-ChangedPath", changedPaths);
        if (!hasChangeScope) {
            hasChangeScope = AddChangeSet(arguments, snapshot);
        }
        AddPaths(arguments, "-ProposedPath", plannedPaths);
        AddPaths(arguments, "-ExecutedCheck", executedChecks);

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
        string? baseRevision,
        string? headRevision,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        ChangeSetSnapshot snapshot = await snapshots.GetAsync(cancellationToken).ConfigureAwait(false);
        List<DevelopmentContextComponentError> errors = [];
        WikiCommandResult? trace = await ExecuteComponentAsync(
            "trace",
            ["-Format", "Json", "-Fast", "-Query", query],
            errors,
            cancellationToken).ConfigureAwait(false);
        await EnsureSnapshotUnchangedAsync(snapshot, cancellationToken).ConfigureAwait(false);
        string[] expandedScopePaths = [.. new[] { plannedPath }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Concat(trace?.GetScopePaths() ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        List<string> briefArguments = ["-Format", "Json", "-Compact", "-Objective", intent];
        AddRevisionRange(briefArguments, snapshot, baseRevision, headRevision);
        AddPaths(briefArguments, "-ProposedPath", expandedScopePaths);
        AddChangeSet(briefArguments, snapshot);

        List<string> testArguments = ["-Format", "Json", "-Fast", "-Objective", intent];
        bool baselineAvailable = AddRevisionRange(testArguments, snapshot, baseRevision, headRevision);
        AddChangeSet(testArguments, snapshot);
        AddPaths(testArguments, "-ProposedPath", expandedScopePaths);

        Task<WikiCommandResult?> briefTask = ExecuteComponentAsync(
            "brief", briefArguments, errors, cancellationToken);
        Task<WikiCommandResult?> testPlanTask = HasScope(testArguments)
            ? ExecuteComponentAsync("test-plan", testArguments, errors, cancellationToken)
            : Task.FromResult<WikiCommandResult?>(null);
        if (!HasScope(testArguments)) {
            errors.Add(new DevelopmentContextComponentError(
                "test-plan",
                DevelopmentMcpErrorCodes.TestPlanScopeRequired,
                "No changed, planned, or traced repository paths were available for test planning."));
        }
        await Task.WhenAll(briefTask, testPlanTask).ConfigureAwait(false);
        await EnsureSnapshotUnchangedAsync(snapshot, cancellationToken).ConfigureAwait(false);

        string[] effectiveLayers = InferLayers(snapshot.ChangedPaths.Concat(expandedScopePaths));
        return new DevelopmentContext(
            snapshot.Fingerprint,
            snapshot.GitHead,
            await briefTask.ConfigureAwait(false),
            trace,
            await testPlanTask.ConfigureAwait(false),
            PartialSuccess: errors.Count > 0,
            ComponentErrors: errors,
            ExpandedScopePaths: expandedScopePaths,
            ScopeMismatch: HasScopeMismatch(plannedPath, trace?.GetScopePaths()),
            EffectiveLayers: effectiveLayers,
            CrossLayerScope: effectiveLayers.Length > 1,
            BaseRevision: baselineAvailable ? baseRevision ?? snapshot.GitHead : null,
            HeadRevision: headRevision ?? snapshot.GitHead,
            BaselineAvailable: baselineAvailable);
    }

    private async Task EnsureSnapshotUnchangedAsync(
        ChangeSetSnapshot expected,
        CancellationToken cancellationToken) {
        ChangeSetSnapshot current = await snapshots.RefreshAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(expected.Fingerprint, current.Fingerprint, StringComparison.Ordinal)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.SnapshotChanged,
                "The Git/worktree snapshot changed while development context was being collected. Retry the request.");
        }
    }

    private async Task<WikiCommandResult?> ExecuteComponentAsync(
        string command,
        IReadOnlyList<string> arguments,
        List<DevelopmentContextComponentError> errors,
        CancellationToken cancellationToken) {
        try {
            return await executor.ExecuteAsync(command, arguments, cancellationToken).ConfigureAwait(false);
        } catch (DevelopmentMcpException exception) {
            lock (errors) {
                errors.Add(new DevelopmentContextComponentError(command, exception.ErrorCode, exception.Message));
            }
            return null;
        }
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

    private static bool AddRevisionRange(
        List<string> arguments,
        ChangeSetSnapshot snapshot,
        string? baseRevision,
        string? headRevision) {
        if (!string.IsNullOrWhiteSpace(baseRevision) &&
            !string.IsNullOrWhiteSpace(headRevision) &&
            string.Equals(baseRevision, headRevision, StringComparison.OrdinalIgnoreCase)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.InvalidRevisionRange,
                "baseRevision and headRevision must identify different revisions.");
        }
        bool baselineAvailable = !string.IsNullOrWhiteSpace(baseRevision) || snapshot.ChangedPaths.Count > 0;
        if (baselineAvailable) {
            arguments.Add("-BaseRef");
            arguments.Add(baseRevision ?? snapshot.GitHead);
        } else {
            arguments.Add("-NoBaseline");
        }
        if (!string.IsNullOrWhiteSpace(headRevision)) {
            arguments.Add("-HeadRef");
            arguments.Add(headRevision);
        }
        return baselineAvailable;
    }

    private static bool HasScopeMismatch(string? plannedPath, IReadOnlyList<string>? tracedPaths) {
        if (string.IsNullOrWhiteSpace(plannedPath)) {
            return false;
        }

        string normalizedPlannedPath = plannedPath.Replace('\\', '/').TrimEnd('/');
        return (tracedPaths ?? []).Any(path => {
            string normalizedPath = path.Replace('\\', '/');
            return !normalizedPath.Equals(normalizedPlannedPath, StringComparison.OrdinalIgnoreCase) &&
                !normalizedPath.StartsWith($"{normalizedPlannedPath}/", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string[] InferLayers(IEnumerable<string> paths) => [.. paths
        .Select(InferLayer)
        .Where(layer => layer is not null)
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)];

    private static string? InferLayer(string path) {
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("FoodDiary.Web.Client/", StringComparison.OrdinalIgnoreCase)) {
            return "Frontend";
        }
        if (normalized.StartsWith("FoodDiary.Presentation.Api/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("FoodDiary.Web.", StringComparison.OrdinalIgnoreCase)) {
            return "Api";
        }
        if (normalized.StartsWith("FoodDiary.Infrastructure/", StringComparison.OrdinalIgnoreCase)) {
            return "Infrastructure";
        }
        if (normalized.StartsWith("FoodDiary.Domain/", StringComparison.OrdinalIgnoreCase)) {
            return "Domain";
        }
        if (normalized.StartsWith("FoodDiary.Application", StringComparison.OrdinalIgnoreCase)) {
            return "Application";
        }
        return null;
    }
}
