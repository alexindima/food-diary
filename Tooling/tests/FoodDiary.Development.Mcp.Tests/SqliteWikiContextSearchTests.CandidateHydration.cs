using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task CompactCandidates_PreserveExactPoolsWithDuplicatesAndMissingFeatureRows(int limit) {
        await using SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString());
        await connection.OpenAsync();
        await using SqliteCommand setup = connection.CreateCommand();
        setup.CommandText = """
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            CREATE TABLE context_search_features(context_rowid INTEGER PRIMARY KEY, path TEXT);
            """;
        await setup.ExecuteNonQueryAsync();
        await using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO context_search VALUES ('code',$key,$path,$key,'csharp','Star Orchard',$body);";
        for (int index = 0; index < 160; index++) {
            string path = index < 140 ? "Area/Repeated.cs" : "Area/Item" + index.ToString("000", System.Globalization.CultureInfo.InvariantCulture) + ".cs";
            insert.Parameters.Clear();
            insert.Parameters.AddWithValue("$key", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$path", path);
            insert.Parameters.AddWithValue("$body", "star orchard " + new string('x', 4096));
            await insert.ExecuteNonQueryAsync();
        }
        setup.CommandText = """
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            INSERT INTO context_search_features SELECT rowid,path FROM context_search WHERE rowid != 160;
            UPDATE context_search_features SET path=NULL WHERE context_rowid=159;
            """;
        await setup.ExecuteNonQueryAsync();
        string original = await ReadCandidatePoolAsync(connection, limit, compact: false);
        string optimized = await ReadCandidatePoolAsync(connection, limit, compact: true);
        Assert.Equal(original, optimized);
        string absent = await ReadCandidatePoolAsync(connection, limit, compact: true, query: "absent");
        Assert.Equal("[]", absent);
    }

    [Fact]
    public async Task SearchAsync_CompactProjectionMatchesMinimalProjection() {
        var reader = new SqliteContextSearchReader(_fixtureRoot);
        WikiContextSearchResult before = await reader.SearchAsync("USDA foods search", 20, "Any", module: null, scopePaths: null,
            CancellationToken.None, "fixture-change-set");
        ExecuteBatchFixtureSql("""
            CREATE TABLE context_search_features(context_rowid INTEGER PRIMARY KEY, path TEXT);
            INSERT INTO context_search_features SELECT rowid,path FROM context_search;
            """);
        WikiContextSearchResult after = await reader.SearchAsync("USDA foods search", 20, "Any", module: null, scopePaths: null,
            CancellationToken.None, "fixture-change-set");
        Assert.Equal(JsonSerializer.Serialize(before with { QueryDurationMilliseconds = 0 }),
            JsonSerializer.Serialize(after with { QueryDurationMilliseconds = 0 }));
    }

    private static async Task<string> ReadCandidatePoolAsync(SqliteConnection connection, int limit, bool compact, string query = "star orchard") {
        string[] terms = query.Split(' ');
        MethodInfo method = typeof(SqliteContextSearchReader).GetMethod("ReadCandidatesAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task work = Assert.IsAssignableFrom<Task>(method.Invoke(null,
            [connection, query, terms, terms, terms, limit, limit, CancellationToken.None, compact, null]));
        await work;
        IEnumerable records = Assert.IsAssignableFrom<IEnumerable>(work.GetType().GetProperty("Result")!.GetValue(work));
        return JsonSerializer.Serialize(records.Cast<object>());
    }
}
