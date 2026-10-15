using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public interface IWikiFlowScopeSearch {
    Task<DevelopmentFlowScope> ExpandFlowScopeAsync(
        string query,
        IReadOnlyList<string> seedPaths,
        IReadOnlyList<string> queryTerms,
        bool includeTests,
        string expectedChangeSetFingerprint,
        CancellationToken cancellationToken);
}
