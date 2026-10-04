using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Theory]
    [InlineData("astral orchard session", "Backend", true)]
    [InlineData("refresh astral orchard session", "Backend", false)]
    [InlineData("calculate astral orchard progress", "Backend", false)]
    [InlineData("send astral orchard session reminder", "Backend", false)]
    [InlineData("отправить напоминание о прогрессе astral orchard", "Backend", false)]
    [InlineData("astral orchard session", "Any", false)]
    public async Task SearchAsync_MovedDomainEntitiesRetainApplicableStateRolesAsync(string query, string changeType, bool stateRole) {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','entity','Modules/Astral/Domain/Entities/AstralOrchardSession.cs','entity','csharp',
                    'AstralOrchardSession','astral orchard session progress'),
                ('code','migration','FoodDiary.Infrastructure/Migrations/AddAstralOrchardSession.cs','migration','csharp',
                    'AddAstralOrchardSession','astral orchard session progress');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 10, changeType, module: null, scopePaths: null, CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        Assert.True(result.Ready);
        WikiContextSearchCandidate entity = Assert.Single(result.Candidates, candidate => candidate.Path.StartsWith("Modules/Astral/", StringComparison.Ordinal));
        Assert.Equal(stateRole, entity.Reasons.Any(reason => reason.StartsWith("structural role domain-state-entity-role", StringComparison.Ordinal)));
        if (stateRole) {
            Assert.Equal(entity.Path, result.Candidates[0].Path);
        }
    }

    [Fact]
    public async Task SearchAsync_RecallsCompoundSubjectsWithDistinctPathsDespiteDuplicateRows() {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        JsonNode policy = JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["candidatePoolLimit"] = 1;
        policy["identityCandidatePoolLimit"] = 2;
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','subject','Modules/Astral/Application/Queries/GetAstralOrchard/GetAstralOrchardQueryHandler.cs',
                    'subject','csharp','Get Astral Orchard Query Handler',$body);
            WITH RECURSIVE rows(value) AS (SELECT 1 UNION ALL SELECT value + 1 FROM rows WHERE value < 20)
            INSERT INTO context_search SELECT 'query-document', 'noise-' || value, 'Area/SavedDetails.cs',
                'noise','csharp','read saved details','read saved details astral' FROM rows;
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        command.Parameters.AddWithValue("$body", string.Concat(Enumerable.Repeat("unrelated navigation ", 1000)));
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "read saved astral orchard details", 10, "Backend", module: null, scopePaths: null,
            CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        Assert.True(result.Ready);
        Assert.Contains(result.Candidates, candidate => candidate.Path.EndsWith("GetAstralOrchardQueryHandler.cs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_RecallsLongGuidanceForNamedSubjectOutsideBroadPool() {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        JsonNode policy = JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["candidatePoolLimit"] = 1;
        policy["identityCandidatePoolLimit"] = 1;
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','policy','Area/ArtemisAccessPolicy.cs','policy','csharp','ArtemisAccessPolicy','Artemis access rules policy'),
                ('agent-guide','guide','AGENTS.md','guide','instructions','Repository instructions',$body),
                ('agent-guide','other','Other/AGENTS.md','other','instructions','Repository instructions','Selene access rules policy');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        command.Parameters.AddWithValue("$body", $"{string.Concat(Enumerable.Repeat("unrelated navigation ", 1000))} Artemis access rules policy");
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "где правила доступа к Artemis только для чтения", 10, "Any", module: null, scopePaths: null,
            CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        Assert.True(result.Ready);
        WikiContextSearchCandidate guide = Assert.Single(result.Candidates, candidate => string.Equals(candidate.Path, "AGENTS.md", StringComparison.Ordinal));
        Assert.Contains("requested guidance with literal subject", guide.Reasons, StringComparer.Ordinal);
        Assert.DoesNotContain(result.Candidates, candidate => string.Equals(candidate.Path, "Other/AGENTS.md", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Find frontend and backend water requests")]
    [InlineData("Найди клиентскую и серверную части запроса воды")]
    [InlineData("Find frontend water requests\nand backend water queries")]
    public async Task SearchAsync_MarksMultipleRequestedLayersAsAmbiguous(string query) {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','web','FoodDiary.Web.Client/src/app/water.service.ts','web','typescript','WaterService','water frontend backend api requests query'),
                ('code','backend','Modules/Hydration/Application/WaterQueryHandler.cs','backend','csharp','WaterQueryHandler','water frontend backend query');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 10, "Any", module: null, scopePaths: null, CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        Assert.True(result.Ready);
        Assert.NotEmpty(result.Candidates);
        Assert.All(result.Candidates, candidate => {
            Assert.True(candidate.Ambiguous);
            Assert.Equal("low", candidate.Confidence);
            Assert.Equal("multi-layer-request", candidate.AmbiguityReason);
        });
    }

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
