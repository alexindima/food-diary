using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

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
        List<DevelopmentContextComponentError> errors = [];
        WikiCommandResult? trace = await ExecuteComponentAsync(
            "trace",
            ["-Format", "Json", "-Fast", "-Query", query],
            errors,
            cancellationToken).ConfigureAwait(false);
        string[] expandedScopePaths = [.. new[] { plannedPath }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Concat(trace?.ReferencedPaths ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        List<string> briefArguments = ["-Format", "Json", "-Compact", "-Objective", intent];
        AddPaths(briefArguments, "-ProposedPath", expandedScopePaths);
        AddChangeSet(briefArguments, snapshot);

        List<string> testArguments = ["-Format", "Json", "-Fast", "-Objective", intent];
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
            ScopeMismatch: HasScopeMismatch(plannedPath, trace?.ReferencedPaths),
            EffectiveLayers: effectiveLayers,
            CrossLayerScope: effectiveLayers.Length > 1);
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
            normalized.StartsWith("FoodDiary.Web.Api/", StringComparison.OrdinalIgnoreCase)) {
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
