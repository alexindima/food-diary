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
    bool ScopeMismatch) {
    public DevelopmentContext WithoutRawOutput() => this with {
        ChangeContext = ChangeContext?.WithoutRawOutput(),
        BackendTrace = BackendTrace?.WithoutRawOutput(),
        TestPlan = TestPlan?.WithoutRawOutput(),
    };
}
