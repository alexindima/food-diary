using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FoodDiary.Development.Mcp.Protocol;
using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Wiki;

// This reader follows type identity, not arbitrary method-name coincidences.
// The Node graph manager remains the sole writer of this projection.
internal sealed class SqliteFlowScopeReader(string repositoryRoot) {
    internal static bool RequestsFlow(string query) => Regex.IsMatch(query,
        @"\b(flow|trace|chain|end.to.end)\b|цепоч|прослед|сквоз|как проходит|(?:клиент[\s\S]*сервер|frontend[\s\S]*backend|client[\s\S]*server)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));

    internal static bool RequestsTests(string query) => Regex.IsMatch(query,
        @"\b(tests?|testing|specs?)\b|тест", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));

    internal static string Stage(string path) {
        string value = path.Replace('\\', '/').ToLowerInvariant();
        if (Regex.IsMatch(value, @"(^|/)(tests?|[^/]+\.tests?)/|\.(spec|test)\.[^/]+$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100))) { return "tests"; }
        if (value.StartsWith("fooddiary.web.client/", StringComparison.Ordinal)) { return "frontend"; }
        string fileName = Path.GetFileName(path.Replace('\\', '/'));
        if (fileName.Length > 1 && fileName[0] == 'I' && char.IsUpper(fileName[1])) { return "contract"; }
        if (value.Contains("worker", StringComparison.Ordinal) || value.StartsWith("fooddiary.jobmanager/", StringComparison.Ordinal)) { return "worker"; }
        if (value.EndsWith("controller.cs", StringComparison.Ordinal) || value.EndsWith("endpoints.cs", StringComparison.Ordinal)) { return "endpoint"; }
        if (value.Contains("/infrastructure/", StringComparison.Ordinal) || value.StartsWith("fooddiary.infrastructure/", StringComparison.Ordinal) || value.StartsWith("fooddiary.integrations/", StringComparison.Ordinal) || value.StartsWith("fooddiary.readmodel.composition/", StringComparison.Ordinal)) { return "infrastructure"; }
        if (value.EndsWith("handler.cs", StringComparison.Ordinal)) { return "handler"; }
        if (value.EndsWith("service.cs", StringComparison.Ordinal)) { return "service"; }
        return "contract";
    }

    public async Task<DevelopmentFlowScope> ExpandAsync(
        IReadOnlyList<string> seedPaths,
        bool includeTests,
        string expectedFingerprint,
        CancellationToken cancellationToken) {
        var stopwatch = Stopwatch.StartNew();
        string databasePath = Path.Combine(repositoryRoot, ".artifacts", "llm-wiki", "code-graph", "code-graph.sqlite");
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(databasePath)) { return Unavailable("database-missing"); }
        try {
            SqliteConnection connection = new(new SqliteConnectionStringBuilder {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
                DefaultTimeout = 2,
            }.ToString());
            await using ConfiguredAsyncDisposable connectionDisposal = connection.ConfigureAwait(false);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            SqliteTransaction transaction = connection.BeginTransaction(deferred: true);
            await using ConfiguredAsyncDisposable transactionDisposal = transaction.ConfigureAwait(false);
            SqliteCommand metadata = connection.CreateCommand();
            await using ConfiguredAsyncDisposable metadataDisposal = metadata.ConfigureAwait(false);
            metadata.Transaction = transaction;
            metadata.CommandText = "SELECT value FROM metadata WHERE key = 'change_set_fingerprint';";
            if (!string.Equals(await metadata.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string,
                expectedFingerprint, StringComparison.Ordinal)) { return Unavailable("snapshot-mismatch"); }
            HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
            List<DevelopmentFlowLink> links = [];
            string[] frontier = [.. seedPaths.Distinct(StringComparer.OrdinalIgnoreCase).Take(12)];
            bool truncated = seedPaths.Count > 12;
            for (int depth = 0; depth < 2 && frontier.Length > 0; depth++) {
                List<string> next = [];
                foreach (string source in frontier) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!visited.Add(source)) { continue; }
                    SqliteCommand command = connection.CreateCommand();
                    await using ConfiguredAsyncDisposable commandDisposal = command.ConfigureAwait(false);
                    command.Transaction = transaction;
                    command.CommandTimeout = 2;
                    command.CommandText = """
                        WITH source AS (SELECT id FROM files WHERE path = $path),
                        declarations AS (
                            SELECT name FROM symbols WHERE file_id IN source
                              AND kind IN ('class','interface','record','record struct','struct') AND length(name) >= 12
                        ), neighbours AS (
                            SELECT dst.path, 'type-reference' kind, s.name evidence
                            FROM source src JOIN file_tokens t ON t.file_id = src.id
                            JOIN symbols s ON s.name = t.token COLLATE NOCASE JOIN files dst ON dst.id = s.file_id
                            WHERE s.kind IN ('class','interface','record','record struct','struct') AND length(s.name) >= 12
                              AND (SELECT count(*) FROM symbols same WHERE same.name = s.name COLLATE NOCASE) <= 3
                              AND (SELECT count(*) FROM file_tokens common WHERE common.token = s.name COLLATE NOCASE) <= 40
                            UNION
                            SELECT dst.path, 'type-consumer', d.name FROM declarations d
                            JOIN file_tokens t ON t.token = d.name COLLATE NOCASE JOIN files dst ON dst.id = t.file_id
                            WHERE (SELECT count(*) FROM file_tokens common WHERE common.token = d.name COLLATE NOCASE) <= 40
                            UNION
                            SELECT dst.path, 'type-implementation', e.target FROM declarations d
                            JOIN typed_edges e ON e.target = d.name COLLATE NOCASE JOIN files dst ON dst.id = e.file_id
                            WHERE e.kind IN ('type-inheritance','type-implementation')
                            UNION
                            SELECT dst.path, 'interface-implementation', e.target FROM source src
                            JOIN file_tokens t ON t.file_id = src.id
                            JOIN typed_edges e ON e.target = t.token COLLATE NOCASE JOIN files dst ON dst.id = e.file_id
                            WHERE e.kind IN ('type-inheritance','type-implementation') AND length(e.target) >= 12
                              AND EXISTS(SELECT 1 FROM symbols declaration WHERE declaration.name = e.target COLLATE NOCASE AND declaration.kind = 'interface')
                              AND (SELECT count(*) FROM file_tokens common WHERE common.token = e.target COLLATE NOCASE) <= 40
                            UNION
                            SELECT dst.path, 'interface-consumer', e.target FROM source src
                            JOIN typed_edges e ON e.file_id = src.id
                            JOIN file_tokens t ON t.token = e.target COLLATE NOCASE JOIN files dst ON dst.id = t.file_id
                            WHERE e.kind IN ('type-inheritance','type-implementation') AND length(e.target) >= 12
                              AND EXISTS(SELECT 1 FROM symbols declaration WHERE declaration.name = e.target COLLATE NOCASE AND declaration.kind = 'interface')
                              AND (SELECT count(*) FROM file_tokens common WHERE common.token = e.target COLLATE NOCASE) <= 40
                            UNION
                            SELECT dst.path, 'mediator-dispatch', e.target FROM source src
                            JOIN typed_edges e ON e.file_id = src.id
                            JOIN typed_edges other ON other.target = e.target COLLATE NOCASE
                            JOIN files dst ON dst.id = other.file_id
                            WHERE e.kind IN ('mediator-handler','mediator-dispatch')
                              AND other.kind IN ('mediator-handler','mediator-dispatch') AND other.kind <> e.kind
                        )
                        SELECT DISTINCT path, kind, evidence FROM neighbours WHERE path <> $path
                        ORDER BY CASE WHEN path LIKE '%Worker.cs' THEN 0 WHEN path LIKE '%Handler.cs' THEN 1
                            WHEN path LIKE '%Service.cs' THEN 2 ELSE 3 END, path, kind, evidence LIMIT 41;
                        """;
                    command.Parameters.AddWithValue("$path", source);
                    SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    await using ConfiguredAsyncDisposable readerDisposal = reader.ConfigureAwait(false);
                    int count = 0;
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                        if (++count > 40 || links.Count >= 80) { truncated = true; break; }
                        string target = reader.GetString(0);
                        if (!IsRelevantPath(target, includeTests)) { continue; }
                        links.Add(new DevelopmentFlowLink(source, target, reader.GetString(1), reader.GetString(2)));
                        if (!visited.Contains(target) && visited.Count + next.Count < 16) { next.Add(target); } else if (!visited.Contains(target)) { truncated = true; }
                    }
                    if (links.Count >= 80) { break; }
                }
                frontier = [.. next.Distinct(StringComparer.OrdinalIgnoreCase)];
                if (depth == 1 && frontier.Length > 0) { truncated = true; }
            }
            return new DevelopmentFlowScope(Ready: true,
                [.. links.Select(link => link.TargetPath).Distinct(StringComparer.OrdinalIgnoreCase)],
                links, [], truncated, stopwatch.Elapsed.TotalMilliseconds);
        } catch (SqliteException exception) {
            return Unavailable(FormattableString.Invariant($"sqlite-error-{exception.SqliteErrorCode}"));
        }

        DevelopmentFlowScope Unavailable(string reason) => new(Ready: false, [], [], [], Truncated: false,
            stopwatch.Elapsed.TotalMilliseconds, reason);
    }

    private static bool IsRelevantPath(string path, bool includeTests) =>
        path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
            ? !path.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains("/Generated/", StringComparison.OrdinalIgnoreCase) &&
                (includeTests || !string.Equals(Stage(path), "tests", StringComparison.Ordinal))
            : false;
}
