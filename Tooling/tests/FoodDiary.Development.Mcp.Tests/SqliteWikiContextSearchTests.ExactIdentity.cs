using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Fact]
    public async Task SearchAsync_ExactDeclaredFunctionOutranksRelatedFileNamesAsync() {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','function','.llm-wiki/tools/code-graph-maintenance.mjs','function','typescript','repairWikiReferences repair Wiki References','repair wiki references'),
                ('code','related','.llm-wiki/tools/Manage-LlmWikiRepairLearning.ps1','related','powershell','','repairWikiReferences repair wiki references');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "repairWikiReferences", 5, "Any", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        Assert.Multiple(
            () => Assert.Equal(".llm-wiki/tools/code-graph-maintenance.mjs", result.Candidates[0].Path),
            () => Assert.Contains("exact declared symbol identity", result.Candidates[0].Reasons, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("RefreshTokenCommandHandlerTests")]
    [InlineData("RefreshTokenCommandHandlerTests.cs")]
    [InlineData("Modules/Identity/tests/RefreshTokenCommandHandlerTests.cs")]
    public async Task SearchAsync_ExactTestIdentityWinsWithoutChangeTypeAsync(string query) {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','handler','Modules/Identity/Application/RefreshTokenCommandHandler.cs','handler','csharp','RefreshTokenCommandHandler','refresh token command handler'),
                ('code','test','Modules/Identity/tests/RefreshTokenCommandHandlerTests.cs','test','csharp','RefreshTokenCommandHandlerTests','refresh token command handler tests');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 5, "Any", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        Assert.Multiple(
            () => Assert.Equal("Modules/Identity/tests/RefreshTokenCommandHandlerTests.cs", result.Candidates[0].Path),
            () => Assert.Equal("high", result.Candidates[0].Confidence),
            () => Assert.Contains("exact file identity", result.Candidates[0].Reasons, StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_SameNamedExactFilesRemainAmbiguousAsync() {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','first','Modules/First/Domain/TokenSession.cs','first','csharp','TokenSession','token session'),
                ('code','second','Modules/Second/Domain/TokenSession.cs','second','csharp','TokenSession','token session');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "TokenSession", 5, "Any", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        Assert.Multiple(
            () => Assert.Equal(2, result.Candidates.Count),
            () => Assert.All(result.Candidates, candidate => Assert.Equal("multiple-exact-identities", candidate.AmbiguityReason)),
            () => Assert.All(result.Candidates, candidate => Assert.Equal("low", candidate.Confidence)));
    }
}
