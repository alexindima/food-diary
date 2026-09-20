using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Theory]
    [InlineData(0, 0, "high")]
    [InlineData(10000, 0, "medium")]
    [InlineData(10000, 10000, "low")]
    public async Task SearchAsync_CalibratesUnambiguousScoreMargin(int highMinimum, int mediumMinimum, string confidence) {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        JsonNode policy = JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["confidenceCalibration"]!["ambiguityMaximumMargin"] = -1;
        policy["confidenceCalibration"]!["highMinimumMargin"] = highMinimum;
        policy["confidenceCalibration"]!["mediumMinimumMargin"] = mediumMinimum;
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','first','Area/First.cs','first','csharp','First','fruit sample'),
                ('code','second','Area/Second.cs','second','csharp','Second','fruit sample');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "fruit sample", 10, "Any", module: null, scopePaths: null, CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        Assert.True(result.Ready);
        Assert.Equal(2, result.Candidates.Count);
        WikiContextSearchCandidate first = result.Candidates[0];
        Assert.Multiple(() => Assert.False(first.Ambiguous), () => Assert.Equal(confidence, first.Confidence),
            () => Assert.Equal(first.Score - result.Candidates[1].Score, first.ScoreMargin));
    }
}
