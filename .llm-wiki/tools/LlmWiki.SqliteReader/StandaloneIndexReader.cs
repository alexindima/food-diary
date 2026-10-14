using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace LlmWiki.SqliteReader;

public static class StandaloneIndexReader {
    private static readonly Dictionary<string, string> Sources = new(StringComparer.Ordinal) {
        ["catalog"] = "repository-catalog.json",
        ["symbols"] = "csharp-symbol-index.json",
        ["frontend"] = "frontend-index.json",
        ["frontend-contract"] = "frontend-contract-index.json",
        ["backend-contract"] = "backend-contract-index.json",
        ["architecture-health"] = "architecture-health-index.json",
        ["domain-data"] = "domain-data-index.json",
        ["configuration"] = "configuration-index.json",
        ["quality"] = "quality-index.json",
        ["runtime"] = "runtime-topology.json",
        ["sensitive-data"] = "sensitive-data-index.json",
    };

    static StandaloneIndexReader() {
        SQLitePCL.Batteries_V2.Init();
    }

    public static string Query(string repositoryRoot, string index, string? query, int limit, string? databasePathOverride = null) {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 50);
        string queryText = query ?? string.Empty;
        // ECMAScript Unicode lowercasing and module-list serialization belong to
        // the existing Node implementation; do not approximate their semantics.
        if (string.Equals(index, "modules", StringComparison.Ordinal) || queryText.Any(character => character > 127)) {
            return Unavailable(index, "node-query-semantics-required");
        }
        if (!Sources.TryGetValue(index, out string? file)) {
            throw new ArgumentException($"Unsupported compiled index: {index}", nameof(index));
        }
        string sourcePath = $".llm-wiki/generated/{file}";
        string absoluteSource = Path.Combine(repositoryRoot, sourcePath);
        if (!File.Exists(absoluteSource)) { return Unavailable(index, "compiled-source-missing"); }
        string databasePath = databasePathOverride ?? Path.Combine(repositoryRoot, ".artifacts/llm-wiki/code-graph/code-graph.sqlite");
        if (!File.Exists(databasePath)) { return Unavailable(index, "standalone-index-projection-missing"); }
        if (OperatingSystem.IsWindows()) {
            databasePath = Path.GetFullPath(databasePath);
            if (!databasePath.StartsWith(@"\\?\", StringComparison.Ordinal)) {
                databasePath = databasePath.StartsWith(@"\\", StringComparison.Ordinal)
                    ? @"\\?\UNC\" + databasePath[2..]
                    : @"\\?\" + databasePath;
            }
        }
        string sourceHash = SourceHash(absoluteSource);
        using SqliteConnection connection = new(new SqliteConnectionStringBuilder {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            DefaultTimeout = 2,
        }.ToString());
        connection.Open();
        using SqliteTransaction transaction = connection.BeginTransaction(deferred: true);
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE type='table' AND name IN ('metadata','standalone_indexes','standalone_index_sections','standalone_index_records')";
        if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 4) { return Unavailable(index, "standalone-index-projection-missing"); }
        command.CommandText = "SELECT value FROM metadata WHERE key='standalone_index_schema_version'";
        if (!string.Equals(Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture), "1", StringComparison.Ordinal)) { return Unavailable(index, "standalone-index-schema-stale"); }
        command.CommandText = "SELECT content_hash,header_json FROM standalone_indexes WHERE index_name=$index";
        command.Parameters.AddWithValue("$index", index);
        string headerJson;
        using (SqliteDataReader reader = command.ExecuteReader()) {
            if (!reader.Read() || !string.Equals(reader.GetString(0), sourceHash, StringComparison.Ordinal)) { return Unavailable(index, "standalone-index-projection-stale"); }
            headerJson = reader.GetString(1);
        }
        using var header = JsonDocument.Parse(headerJson);
        List<string> sections = [];
        if (string.Equals(index, "configuration", StringComparison.Ordinal) && queryText.Trim().Length > 0) {
            sections.AddRange(["optionTypes", "configurationKeys", "environmentVariables"]);
        } else {
            command.CommandText = "SELECT section_name FROM standalone_index_sections WHERE index_name=$index AND query_only=0 ORDER BY ordinal";
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read()) { sections.Add(reader.GetString(0)); }
        }
        string[] terms = [.. Regex.Matches(queryText.ToLowerInvariant(), "[a-z0-9_-]+", RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1))
            .Select(match => match.Value).Where(term => term.Length > 1).Distinct(StringComparer.Ordinal)];
        string where = "index_name=$index AND section_name=$section" +
            string.Concat(terms.Select((_, position) => " AND instr(search_text,$term" + position.ToString(CultureInfo.InvariantCulture) + ")>0"));
        command.Parameters.AddWithValue("$section", string.Empty);
        for (int position = 0; position < terms.Length; position++) {
            command.Parameters.AddWithValue("$term" + position.ToString(CultureInfo.InvariantCulture), terms[position]);
        }
        command.Parameters.AddWithValue("$limit", limit);
        List<object> results = [];
        foreach (string section in sections) {
            command.Parameters["$section"].Value = section;
            command.CommandText = $"SELECT COUNT(*) FROM standalone_index_records WHERE {where}";
            long count = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            command.CommandText = $"SELECT payload_json FROM standalone_index_records WHERE {where} ORDER BY ordinal LIMIT $limit";
            List<JsonElement> items = [];
            using (SqliteDataReader reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    using var payload = JsonDocument.Parse(reader.GetString(0));
                    items.Add(payload.RootElement.Clone());
                }
            }
            results.Add(new { name = section, count, items });
        }
        if (!File.Exists(absoluteSource) || !string.Equals(SourceHash(absoluteSource), sourceHash, StringComparison.Ordinal)) {
            return Unavailable(index, "standalone-index-source-changed");
        }
        return JsonSerializer.Serialize(new {
            ready = true,
            index = new {
                schemaVersion = 1,
                index,
                sourcePath,
                readOnly = true,
                query = queryText,
                freshness = "not-verified; use -Check or wiki.ps1 verify",
                generatedAtUtc = header.RootElement.GetProperty("generatedAtUtc"),
                summary = header.RootElement.GetProperty("summary"),
                sections = results,
                compiledIndex = new { source = "sqlite-standalone-index", fresh = true, sourceHash },
            },
        });
    }

    private static string SourceHash(string path) {
        string text = Encoding.UTF8.GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string Unavailable(string index, string reason) =>
        JsonSerializer.Serialize(new { ready = false, unavailableReason = reason, index });
}
