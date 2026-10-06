using System.Collections;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Fact]
    public async Task SearchBatchAsync_MatchesOrderedIndividualSearches() {
        (string Query, int Limit, string ChangeType)[] requests = [
            ("web API telemetry logs", 20, "Any"),
            ("USDA foods search", 10, "Backend"),
            ("!!!", 10, "Any"),
            ("web API telemetry logs", 20, "Any"),
        ];
        var reader = new SqliteContextSearchReader(_fixtureRoot);
        List<WikiContextSearchResult> expected = [];
        foreach ((string query, int limit, string changeType) in requests) {
            expected.Add(await reader.SearchAsync(query, limit, changeType, module: null, scopePaths: null,
                CancellationToken.None, "fixture-change-set"));
        }
        IReadOnlyList<WikiContextSearchResult> actual = await reader.SearchBatchAsync(requests, CancellationToken.None, "fixture-change-set");
        Assert.Equal(JsonSerializer.Serialize(expected.Select(result => result with { QueryDurationMilliseconds = 0 })),
            JsonSerializer.Serialize(actual.Select(result => result with { QueryDurationMilliseconds = 0 })));
    }

    [Theory]
    [InlineData("stale", "snapshot-mismatch")]
    [InlineData("missing", "database-missing")]
    [InlineData("empty", "fts-projection-not-ready")]
    public async Task SearchBatchAsync_PreservesUnavailableStates(string mode, string reason) {
        if (string.Equals(mode, "missing", StringComparison.Ordinal)) {
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
        if (string.Equals(mode, "empty", StringComparison.Ordinal)) { ExecuteBatchFixtureSql("DELETE FROM context_search;"); }
        var reader = new SqliteContextSearchReader(_fixtureRoot);
        IReadOnlyList<WikiContextSearchResult> results = await reader.SearchBatchAsync(
            [("USDA foods search", 10, "Any"), ("telemetry logs", 10, "Any")], CancellationToken.None,
            string.Equals(mode, "stale", StringComparison.Ordinal) ? "different-snapshot" : "fixture-change-set");
        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.Multiple(() => Assert.False(result.Ready),
            () => Assert.Equal(reason, result.UnavailableReason), () => Assert.Empty(result.Candidates)));
    }

    [Fact]
    public async Task SearchBatchAsync_KeepsOneSnapshotAndRevalidatesTheNextBatch() {
        ExecuteBatchFixtureSql("PRAGMA journal_mode=WAL;");
        var reader = new SqliteContextSearchReader(_fixtureRoot);
        var requests = new PreparedBatchRequests([("USDA foods search", 10, "Any"), ("telemetry logs", 10, "Any")],
            () => ExecuteBatchFixtureSql("UPDATE metadata SET value='new-snapshot' WHERE key='change_set_fingerprint'; DELETE FROM context_search;"));
        IReadOnlyList<WikiContextSearchResult> pinned = await reader.SearchBatchAsync(requests, CancellationToken.None, "fixture-change-set");
        Assert.All(pinned, result => Assert.Multiple(() => Assert.True(result.Ready),
            () => Assert.Equal("fixture-change-set", result.ChangeSetFingerprint), () => Assert.NotEmpty(result.Candidates)));
        IReadOnlyList<WikiContextSearchResult> refreshed = await reader.SearchBatchAsync([("USDA foods search", 10, "Any")],
            CancellationToken.None, "fixture-change-set");
        WikiContextSearchResult unavailable = Assert.Single(refreshed);
        Assert.False(unavailable.Ready);
        Assert.Equal("new-snapshot", unavailable.ChangeSetFingerprint);
        Assert.Empty(unavailable.Candidates);
    }

    [Fact]
    public async Task SearchBatchAsync_CancellationReleasesTheOwnedConnection() {
        // Release the fixture constructor's pooled writer before testing the
        // batch's own non-pooled connection lifetime.
        SqliteConnection.ClearAllPools();
        using var cancellation = new CancellationTokenSource();
        var requests = new PreparedBatchRequests([("telemetry logs", 10, "Any")], cancellation.Cancel);
        var reader = new SqliteContextSearchReader(_fixtureRoot);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.SearchBatchAsync(requests, cancellation.Token, "fixture-change-set"));
        string renamed = _databasePath + ".closed";
        File.Move(_databasePath, renamed);
        File.Move(renamed, _databasePath);
        Assert.Empty(await reader.SearchBatchAsync([], CancellationToken.None));
    }

    private void ExecuteBatchFixtureSql(string sql) {
        using SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [ExcludeFromCodeCoverage]
    private sealed class PreparedBatchRequests((string Query, int Limit, string ChangeType)[] requests, Action afterPreparation)
        : IReadOnlyList<(string Query, int Limit, string ChangeType)> {
        private int _enumerations;
        public int Count => requests.Length;
        public (string Query, int Limit, string ChangeType) this[int index] => requests[index];
        public IEnumerator<(string Query, int Limit, string ChangeType)> GetEnumerator() {
            if (++_enumerations == 2) { afterPreparation(); }
            return ((IEnumerable<(string Query, int Limit, string ChangeType)>)requests).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
