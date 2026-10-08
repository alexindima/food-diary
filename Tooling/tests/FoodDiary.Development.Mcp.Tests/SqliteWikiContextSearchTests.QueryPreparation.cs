using System.Text.Json.Nodes;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Fact]
    public async Task SearchBatchAsync_PreparesBoostsSeparatelyForEachQueryAndChangeType() {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        JsonNode policy = JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["identityBoosts"] = JsonNode.Parse("""
            [{"id":"stock-identity","queryTerms":["stock","service"],"minimumMatches":2,
              "identityTerms":["stock"],"minimumIdentityMatches":1,"score":300,"identityScope":"file",
              "directOnly":true,"changeTypes":["Backend"],"excludedQueryTerms":["excluded"]}]
            """);
        policy["pathBoosts"] = JsonNode.Parse("""
            [{"id":"stock-path","queryTerms":["stock","service"],"minimumMatches":2,
              "pathPrefixes":["Tooling/"],"score":300,"directOnly":true,"excludedQueryTerms":["excluded"]}]
            """);
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        ExecuteBatchFixtureSql("""
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            INSERT INTO context_search VALUES
                ('code','stock','Tooling/Stock.cs','stock','csharp','Stock','stock service excluded');
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """);
        (string Query, int Limit, string ChangeType)[] requests = [
            ("stock service", 10, "Any"),
            ("stock", 10, "Backend"),
            ("stock service", 10, "Backend"),
            ("stock service excluded", 10, "Backend"),
            ("stock service", 10, "Frontend"),
            ("stock", 10, "Backend"),
        ];
        var reader = new SqliteContextSearchReader(_fixtureRoot);
        IReadOnlyList<WikiContextSearchResult> results = await reader.SearchBatchAsync(
            requests, CancellationToken.None, "fixture-change-set");
        (bool Identity, bool Path)[] expected = [(false, true), (false, false), (true, true), (false, false), (false, true), (false, false)];
        Assert.Equal(expected.Length, results.Count);
        for (int index = 0; index < results.Count; index++) {
            WikiContextSearchResult result = results[index];
            Assert.True(result.Ready);
            WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
            Assert.Multiple(
                () => Assert.Equal(expected[index].Identity, candidate.Reasons.Contains("ranking policy stock-identity", StringComparer.Ordinal)),
                () => Assert.Equal(expected[index].Path, candidate.Reasons.Contains("ranking policy stock-path", StringComparer.Ordinal)));
        }
    }
}
