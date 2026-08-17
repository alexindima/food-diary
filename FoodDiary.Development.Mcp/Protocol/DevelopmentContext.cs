using FoodDiary.Development.Mcp.Wiki;

namespace FoodDiary.Development.Mcp.Protocol;

public sealed record DevelopmentContext(
    string SnapshotFingerprint,
    string GitHead,
    WikiCommandResult? ChangeContext,
    WikiCommandResult? BackendTrace,
    WikiCommandResult? TestPlan,
    bool PartialSuccess,
    IReadOnlyList<DevelopmentContextComponentError> ComponentErrors,
    IReadOnlyList<string> ExpandedScopePaths,
    bool ScopeMismatch,
    IReadOnlyList<string> EffectiveLayers,
    bool CrossLayerScope,
    string? BaseRevision = null,
    string? HeadRevision = null,
    bool BaselineAvailable = false) {
    public DevelopmentContext WithoutRawOutput() => this with {
        ChangeContext = ChangeContext?.WithoutRawOutput(),
        BackendTrace = BackendTrace?.WithoutRawOutput(),
        TestPlan = TestPlan?.WithoutRawOutput(),
    };

    public DevelopmentContext ToCompact(bool includeRawOutput = false) => this with {
        ChangeContext = ChangeContext?.ToCompactChangeContext(includeRawOutput: includeRawOutput),
        BackendTrace = BackendTrace?.ToCompactTrace(includeRawOutput: includeRawOutput),
        TestPlan = TestPlan?.ToCompactTestPlan(includeRawOutput: includeRawOutput),
        ExpandedScopePaths = ExpandedScopePaths.Take(20).ToArray(),
    };
}
