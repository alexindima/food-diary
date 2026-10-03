using System.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class SqliteWikiContextSearch : IWikiContextSearch {
    private readonly SqliteContextSearchReader _reader;
    private readonly WikiRuntimeTelemetry _telemetry;

    public SqliteWikiContextSearch(WikiRuntimeTelemetry telemetry)
        : this(RepositoryRootResolver.Resolve(), telemetry) {
    }

    internal SqliteWikiContextSearch(string repositoryRoot, WikiRuntimeTelemetry telemetry) {
        _reader = new SqliteContextSearchReader(repositoryRoot);
        _telemetry = telemetry;
    }

    public async Task<WikiContextSearchResult> SearchAsync(
        string query,
        int limit,
        string changeType,
        string? module,
        IReadOnlyList<string>? scopePaths,
        CancellationToken cancellationToken,
        string? expectedChangeSetFingerprint = null) {
        var stopwatch = Stopwatch.StartNew();
        try {
            return await _reader.SearchAsync(query, limit, changeType, module, scopePaths,
                cancellationToken, expectedChangeSetFingerprint).ConfigureAwait(false);
        } finally {
            _telemetry.RecordCommandStage("context-search", "in-process-sqlite", stopwatch.Elapsed);
        }
    }
}
