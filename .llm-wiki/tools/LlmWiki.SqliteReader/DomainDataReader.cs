using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LlmWiki.SqliteReader;

public static class DomainDataReader {
    private const string SourcePath = ".llm-wiki/generated/domain-data-index.json";
    private static readonly HashSet<string> SupportedViews = new(StringComparer.Ordinal) {
        "all",
        "types",
        "invariants",
        "mappings",
        "indexes",
        "relationships",
    };

    static DomainDataReader() {
        SQLitePCL.Batteries_V2.Init();
    }

    public static string Query(
        string repositoryRoot,
        string view,
        string? query,
        int limit,
        bool includeDiagnostics,
        double readerLoadDurationMilliseconds = 0) {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        string normalizedView = SupportedViews.Contains(view) ? view : "all";
        string sourceAbsolutePath = Path.Combine(repositoryRoot, SourcePath.Replace('/', Path.DirectorySeparatorChar));
        string databasePath = Path.Combine(repositoryRoot, ".artifacts", "llm-wiki", "code-graph", "code-graph.sqlite");
        if (!File.Exists(sourceAbsolutePath)) {
            throw new InvalidOperationException("Domain-data source is missing. Run ./.llm-wiki/wiki.ps1 update and retry.");
        }
        if (!File.Exists(databasePath)) {
            throw new InvalidOperationException("SQLite code-graph database is missing. Run ./.llm-wiki/wiki.ps1 graph-build and retry.");
        }

        var stopwatch = Stopwatch.StartNew();
        string sourceText = File.ReadAllText(sourceAbsolutePath);
        string sourceHash = NormalizedSha256(sourceText);
        string connectionString = new SqliteConnectionStringBuilder {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = true,
            DefaultTimeout = 2,
        }.ToString();
        using SqliteConnection connection = new(connectionString);
        connection.Open();
        string? projectedHash = ReadScalarString(
            connection,
            "SELECT value FROM metadata WHERE key = 'query_source:domain';");
        if (string.IsNullOrWhiteSpace(projectedHash)) {
            throw new InvalidOperationException("SQLite domain-data projection is missing. Run ./.llm-wiki/wiki.ps1 graph-build and retry.");
        }
        if (!string.Equals(projectedHash, sourceHash, StringComparison.Ordinal)) {
            throw new InvalidOperationException("SQLite domain-data projection is stale. Run ./.llm-wiki/wiki.ps1 graph-build and retry.");
        }

        GroupSpec[] groups = SelectGroups(normalizedView);
        string queryText = query ?? string.Empty;
        string queryPattern = $"%{EscapeLike(queryText)}%";
        var payloadsByGroup = new List<(string Name, List<string> Payloads)>();
        int candidateRecords = 0;
        int returnedRecords = 0;
        long sourceBytesMaterialized = 0;
        foreach (GroupSpec group in groups) {
            candidateRecords += CountCandidates(connection, group);
            List<string> payloads = ReadPayloads(connection, group, queryText, queryPattern, limit);
            payloadsByGroup.Add((group.Name, payloads));
            returnedRecords += payloads.Count;
            sourceBytesMaterialized += payloads.Sum(payload => Encoding.UTF8.GetByteCount(payload));
        }
        int scannedRecords = Convert.ToInt32(ReadScalarInt64(
            connection,
            "SELECT COUNT(*) FROM query_documents WHERE category = 'domain';"),
            System.Globalization.CultureInfo.InvariantCulture);
        stopwatch.Stop();

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true })) {
            writer.WriteStartObject();
            foreach ((string name, List<string> payloads) in payloadsByGroup) {
                writer.WritePropertyName(name);
                writer.WriteStartArray();
                foreach (string payload in payloads) {
                    using var document = JsonDocument.Parse(payload);
                    document.RootElement.WriteTo(writer);
                }
                writer.WriteEndArray();
            }
            if (includeDiagnostics) {
                writer.WritePropertyName("_diagnostics");
                writer.WriteStartObject();
                writer.WriteString("source", "sqlite-domain-data-in-process");
                writer.WriteString("reader", "microsoft-data-sqlite");
                writer.WriteNumber("readerLoadDurationMs", Round(readerLoadDurationMilliseconds));
                writer.WriteNumber("sqlDurationMs", Round(stopwatch.Elapsed.TotalMilliseconds));
                writer.WriteNumber("completeCommandDurationMs", Round(readerLoadDurationMilliseconds + stopwatch.Elapsed.TotalMilliseconds));
                writer.WriteNumber("scannedRecords", scannedRecords);
                writer.WriteNumber("candidateRecords", candidateRecords);
                writer.WriteNumber("returnedRecords", returnedRecords);
                writer.WriteString("sourceHash", sourceHash);
                writer.WriteNumber("sourceBytesVerified", Encoding.UTF8.GetByteCount(sourceText));
                writer.WriteNumber("sourceBytesMaterialized", sourceBytesMaterialized);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static GroupSpec[] SelectGroups(string view) {
        var groups = new List<GroupSpec>();
        if (view is "all" or "types") {
            groups.Add(new GroupSpec("types", "domainType"));
        }
        if (view is "all" or "invariants") {
            groups.Add(new GroupSpec("invariants", "invariant"));
        }
        if (view is "all" or "mappings") {
            groups.Add(new GroupSpec("mappings", "persistenceMapping"));
        }
        if (string.Equals(view, "indexes", StringComparison.Ordinal)) {
            groups.Add(new GroupSpec("indexes", "persistenceMapping", "json_array_length(payload_json, '$.indexes') > 0"));
        }
        if (string.Equals(view, "relationships", StringComparison.Ordinal)) {
            groups.Add(new GroupSpec("relationships", "persistenceMapping", "json_array_length(payload_json, '$.relationships') > 0"));
        }
        return [.. groups];
    }

    private static int CountCandidates(SqliteConnection connection, GroupSpec group) {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        command.CommandText = $"""
            SELECT COUNT(*)
            FROM query_documents
            WHERE category = 'domain' AND record_kind = $recordKind{AdditionalPredicate(group)};
            """;
        command.Parameters.AddWithValue("$recordKind", group.RecordKind);
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static List<string> ReadPayloads(
        SqliteConnection connection,
        GroupSpec group,
        string query,
        string queryPattern,
        int limit) {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        command.CommandText = $"""
            SELECT payload_json
            FROM query_documents
            WHERE category = 'domain' AND record_kind = $recordKind{AdditionalPredicate(group)}
              AND ($query = '' OR payload_json LIKE $queryPattern ESCAPE '\' COLLATE NOCASE)
            ORDER BY source_ordinal
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$recordKind", group.RecordKind);
        command.Parameters.AddWithValue("$query", query);
        command.Parameters.AddWithValue("$queryPattern", queryPattern);
        command.Parameters.AddWithValue("$limit", limit);
        using SqliteDataReader reader = command.ExecuteReader();
        var payloads = new List<string>();
        while (reader.Read()) {
            payloads.Add(reader.GetString(0));
        }
        return payloads;
    }

    private static string AdditionalPredicate(GroupSpec group) =>
        string.IsNullOrWhiteSpace(group.AdditionalPredicate)
            ? string.Empty
            : $" AND {group.AdditionalPredicate}";

    private static string? ReadScalarString(SqliteConnection connection, string commandText) {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        command.CommandText = commandText;
        return command.ExecuteScalar() as string;
    }

    private static long ReadScalarInt64(SqliteConnection connection, string commandText) {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        command.CommandText = commandText;
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static string NormalizedSha256(string value) {
        string normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record GroupSpec(
        string Name,
        string RecordKind,
        string? AdditionalPredicate = null);
}
