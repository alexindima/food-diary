using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public interface IWikiContextSearch {
    Task<WikiContextSearchResult> SearchAsync(
        string query,
        int limit,
        string changeType,
        string? module,
        IReadOnlyList<string>? scopePaths,
        CancellationToken cancellationToken,
        string? expectedChangeSetFingerprint = null);
}
