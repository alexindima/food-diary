using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LlmWiki.SqliteReader;

public static class CompiledIndexReader {
    static CompiledIndexReader() {
        SQLitePCL.Batteries_V2.Init();
    }

    public static string QueryRuntime(
        string repositoryRoot,
        string? query,
        int limit,
        bool includeDiagnostics,
        double readerLoadDurationMilliseconds = 0) =>
        Query(
            repositoryRoot,
            ".llm-wiki/generated/runtime-topology.json",
            "runtime",
            [
                new("composeServices", "composeService"),
                new("hostedServices", "hostedService"),
                new("httpClients", "httpClient"),
                new("webhooks", "webhook"),
                new("recurringJobRegistrations", "recurringJob"),
            ],
            query,
            limit,
            includeDiagnostics,
            readerLoadDurationMilliseconds);

    public static string QueryArchitectureHealth(
        string repositoryRoot,
        string view,
        string? query,
        int limit,
        bool includeDiagnostics,
        double readerLoadDurationMilliseconds = 0) {
        GroupSpec[] groups = view switch {
            "all" or "drift" => [new("dependencyViolations", "dependencyViolation")],
            "allowances" => [new("unusedAllowances", "unusedProjectAllowance")],
            "untracked" => [new("untrackedProjects", "untrackedProject")],
            "cycles" => [new("moduleCycleNodes", "moduleCycle")],
            "ambiguous" => [new("ambiguousContracts", "ambiguousContract")],
            "dead-candidates" => [
                new("unconsumedBackendContracts", "unconsumedBackendContract"),
                new("selectorUnreferencedComponents", "selectorUnreferenced"),
            ],
            "spec-gaps" => [new("componentsWithoutSpecs", "componentWithoutSpec")],
            "test-gaps" => [new("criticalSymbolsWithoutTests", "criticalSymbolWithoutTest")],
            "debt" => [new("debtMarkers", "debtMarker")],
            _ => [new("dependencyViolations", "dependencyViolation")],
        };
        return Query(
            repositoryRoot,
            ".llm-wiki/generated/architecture-health-index.json",
            "architecture-health",
            groups,
            query,
            limit,
            includeDiagnostics,
            readerLoadDurationMilliseconds);
    }

    private static string Query(
        string repositoryRoot,
        string sourcePath,
        string category,
        GroupSpec[] groups,
        string? query,
        int limit,
        bool includeDiagnostics,
        double readerLoadDurationMilliseconds) {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        string sourceAbsolutePath = Path.Combine(repositoryRoot, sourcePath.Replace('/', Path.DirectorySeparatorChar));
        string databasePath = Path.Combine(repositoryRoot, ".artifacts", "llm-wiki", "code-graph", "code-graph.sqlite");
        if (!File.Exists(sourceAbsolutePath)) {
            throw new InvalidOperationException($"Compiled-index source is missing: {sourcePath}. Run ./.llm-wiki/wiki.ps1 update and retry.");
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
            "SELECT value FROM metadata WHERE key = $key;",
            ("$key", $"query_source:{category}"));
        if (string.IsNullOrWhiteSpace(projectedHash) || !string.Equals(projectedHash, sourceHash, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"SQLite {category} projection is missing or stale. Run ./.llm-wiki/wiki.ps1 graph-build and retry.");
        }

        string queryText = query ?? string.Empty;
        string queryPattern = $"%{EscapeLike(queryText)}%";
        string[] recordKinds = [.. groups.Select(group => group.RecordKind)];
        Dictionary<string, List<string>> payloadsByKind = ReadPayloads(
            connection,
            category,
            recordKinds,
            queryText,
            queryPattern,
            limit);
        List<(string Name, List<string> Payloads)> payloadsByGroup = [];
        int candidateRecords = includeDiagnostics ? CountCandidates(connection, category, recordKinds) : 0;
        int returnedRecords = 0;
        long sourceBytesMaterialized = 0;
        foreach (GroupSpec group in groups) {
            List<string> payloads = payloadsByKind[group.RecordKind];
            payloadsByGroup.Add((group.Name, payloads));
            returnedRecords += payloads.Count;
            if (includeDiagnostics) {
                sourceBytesMaterialized += payloads.Sum(payload => Encoding.UTF8.GetByteCount(payload));
            }
        }
        int scannedRecords = includeDiagnostics
            ? Convert.ToInt32(
                ReadScalarInt64(connection, "SELECT COUNT(*) FROM query_documents WHERE category = $category;", ("$category", category)),
                System.Globalization.CultureInfo.InvariantCulture)
            : 0;
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
                writer.WriteString("source", $"sqlite-{category}-in-process");
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

    private static int CountCandidates(SqliteConnection connection, string category, string[] recordKinds) {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        string kindParameters = string.Join(", ", recordKinds.Select((_, index) => "$recordKind" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        command.CommandText = $"SELECT COUNT(*) FROM query_documents WHERE category = $category AND record_kind IN ({kindParameters});";
        command.Parameters.AddWithValue("$category", category);
        for (int index = 0; index < recordKinds.Length; index++) {
            command.Parameters.AddWithValue("$recordKind" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), recordKinds[index]);
        }
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, List<string>> ReadPayloads(
        SqliteConnection connection,
        string category,
        string[] recordKinds,
        string query,
        string queryPattern,
        int limit) {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        string kindParameters = string.Join(", ", recordKinds.Select((_, index) => "$recordKind" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        command.CommandText = $"""
            SELECT record_kind, payload_json
            FROM query_documents
            WHERE category = $category AND record_kind IN ({kindParameters})
              AND ($query = '' OR payload_json LIKE $queryPattern ESCAPE '\' COLLATE NOCASE)
            ORDER BY source_ordinal
            """;
        command.Parameters.AddWithValue("$category", category);
        for (int index = 0; index < recordKinds.Length; index++) {
            command.Parameters.AddWithValue("$recordKind" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), recordKinds[index]);
        }
        command.Parameters.AddWithValue("$query", query);
        command.Parameters.AddWithValue("$queryPattern", queryPattern);
        using SqliteDataReader reader = command.ExecuteReader();
        Dictionary<string, List<string>> payloads = recordKinds.ToDictionary(kind => kind, _ => new List<string>(), StringComparer.Ordinal);
        while (reader.Read()) {
            List<string> groupPayloads = payloads[reader.GetString(0)];
            if (groupPayloads.Count < limit) {
                groupPayloads.Add(reader.GetString(1));
            }
        }
        return payloads;
    }

    private static string? ReadScalarString(SqliteConnection connection, string commandText, params (string Name, object Value)[] parameters) {
        using SqliteCommand command = CreateCommand(connection, commandText, parameters);
        return command.ExecuteScalar() as string;
    }

    private static long ReadScalarInt64(SqliteConnection connection, string commandText, params (string Name, object Value)[] parameters) {
        using SqliteCommand command = CreateCommand(connection, commandText, parameters);
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, string commandText, params (string Name, object Value)[] parameters) {
        SqliteCommand command = connection.CreateCommand();
        command.CommandTimeout = 2;
        command.CommandText = commandText;
        foreach ((string name, object value) in parameters) {
            command.Parameters.AddWithValue(name, value);
        }
        return command;
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

    private sealed record GroupSpec(string Name, string RecordKind);
}
