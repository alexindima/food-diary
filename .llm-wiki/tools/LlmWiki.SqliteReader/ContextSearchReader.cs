using System.Globalization;
using System.Text.Json;
using FoodDiary.Development.Mcp.Protocol;
using FoodDiary.Development.Mcp.Wiki;
using Microsoft.Data.Sqlite;

namespace LlmWiki.SqliteReader;

public static class ContextSearchReader {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Search(
        string repositoryRoot, string query, int limit, string changeType,
        string? module, string[] scopePaths, string expectedFingerprint) {
        var reader = new SqliteContextSearchReader(repositoryRoot);
        WikiContextSearchResult result = reader.SearchAsync(query, limit, changeType, module, scopePaths,
            CancellationToken.None, expectedFingerprint).GetAwaiter().GetResult();
        WikiContextSearchCandidate? first = result.Candidates.Count > 0 ? result.Candidates[0] : null;
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = Path.Combine(repositoryRoot, ".artifacts", "llm-wiki", "code-graph", "code-graph.sqlite"),
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        if (result.Ready) {
            connection.Open();
        }
        var features = new Dictionary<string, object>(StringComparer.Ordinal);
        if (result.Candidates.Count > 0) {
            var candidates = result.Candidates.ToDictionary(
                candidate => (candidate.Path, candidate.RecordType));
            using SqliteCommand command = connection.CreateCommand();
            command.CommandTimeout = 2;
            string[] paths = [.. result.Candidates.Select(candidate => candidate.Path).Distinct(StringComparer.Ordinal)];
            string[] parameters = [.. paths.Select((_, index) => "$path" + index.ToString(CultureInfo.InvariantCulture))];
            command.CommandText = $"""
                SELECT f.path, f.record_type, s.record_key, s.source_path, s.title,
                    f.layer, f.module, f.role, f.is_test, f.extension
                FROM context_search_features f JOIN context_search s ON s.rowid = f.context_rowid
                WHERE f.path IN ({string.Join(", ", parameters)})
                ORDER BY f.context_rowid;
                """;
            for (int index = 0; index < paths.Length; index++) {
                command.Parameters.AddWithValue(parameters[index], paths[index]);
            }
            using SqliteDataReader rows = command.ExecuteReader();
            while (rows.Read()) {
                string path = rows.GetString(0);
                if (features.ContainsKey(path) || !candidates.TryGetValue((path, rows.GetString(1)), out WikiContextSearchCandidate? candidate)) {
                    continue;
                }
                features[path] = new {
                    candidate.Rank, candidate.Path, candidate.RecordType, candidate.Category,
                    candidate.Score, candidate.LexicalRank, candidate.Reasons, candidate.ScoreMargin,
                    candidate.Confidence, candidate.Ambiguous, candidate.AmbiguityReason,
                    candidate.SameNameCandidateCount,
                    recordKey = rows.GetString(2), sourcePath = rows.GetString(3), title = rows.GetString(4),
                    layer = rows.GetString(5), module = rows.GetString(6), role = rows.GetString(7),
                    isTest = rows.GetInt32(8) != 0, extension = rows.GetString(9),
                };
            }
            if (features.Count != result.Candidates.Count) {
                throw new InvalidDataException("Context candidate features are missing.");
            }
        }
        return JsonSerializer.Serialize(new {
            result.Ready, result.IndexedDocuments, result.Fingerprint, result.UpdatedAtUtc,
            result.ChangeSetFingerprint, result.GitHead, result.Fresh, result.QueryTerms,
            result.UnavailableReason,
            reader = result.Reader,
            durationMs = result.QueryDurationMilliseconds,
            records = result.Candidates.Select(candidate => features[candidate.Path]),
            rankingSummary = new {
                confidence = first?.Confidence ?? "none",
                ambiguous = first?.Ambiguous ?? false,
                ambiguityReason = first?.AmbiguityReason,
                topScoreMargin = first?.ScoreMargin,
                sameNameCandidateCount = first?.SameNameCandidateCount ?? 0,
            },
        }, JsonOptions);
    }
}
