using FoodDiary.Development.Mcp.Wiki;

namespace FoodDiary.Development.Mcp.Diagnostics;

public sealed record ServerStatus(
    string Version,
    ServerRuntimeIdentity RuntimeIdentity,
    string RepositoryRoot,
    string GitHead,
    bool RunningCodeMatchesRepositoryHead,
    bool WorktreeDirty,
    string WorktreeFingerprint,
    string SourceFingerprint,
    bool IndexesMatchWorktree,
    bool RunningCodeIncludesWorktreeChanges,
    IReadOnlyList<string> SourceChangedPaths,
    IReadOnlyList<string> DerivedWikiPaths,
    IReadOnlyList<string> ReviewMetadataPaths,
    bool WikiAvailable,
    bool IndexFilesPresent,
    string DeepFreshness,
    string? LastVerifiedCommit,
    DateTimeOffset? LastVerifiedAtUtc,
    string? IndexFingerprint,
    string IndexStatusCode,
    string IndexCheckSummary,
    IReadOnlyList<WikiIndexStatus> Indexes,
    WikiRuntimeMetrics RuntimeMetrics,
    DateTimeOffset CheckedAtUtc,
    bool ReadOnly = true);
