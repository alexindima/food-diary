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
        foreach (WikiContextSearchCandidate candidate in result.Candidates) {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandTimeout = 2;
            command.CommandText = """
                SELECT s.record_key, s.source_path, s.title, f.layer, f.module, f.role, f.is_test, f.extension
                FROM context_search s JOIN context_search_features f ON f.context_rowid = s.rowid
                WHERE s.path = $path AND s.record_type = $type
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("$path", candidate.Path);
            command.Parameters.AddWithValue("$type", candidate.RecordType);
            using SqliteDataReader rows = command.ExecuteReader();
            if (!rows.Read()) {
                throw new InvalidDataException("Context candidate features are missing.");
            }
            features[candidate.Path] = new {
                candidate.Rank, candidate.Path, candidate.RecordType, candidate.Category,
                candidate.Score, candidate.LexicalRank, candidate.Reasons, candidate.ScoreMargin,
                candidate.Confidence, candidate.Ambiguous, candidate.AmbiguityReason,
                candidate.SameNameCandidateCount,
                recordKey = rows.GetString(0), sourcePath = rows.GetString(1), title = rows.GetString(2),
                layer = rows.GetString(3), module = rows.GetString(4), role = rows.GetString(5),
                isTest = rows.GetInt32(6) != 0, extension = rows.GetString(7),
            };
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
