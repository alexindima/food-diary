namespace FoodDiary.Development.Mcp;

public sealed record DevelopmentContext(
    string SnapshotFingerprint,
    string GitHead,
    WikiCommandResult ChangeContext,
    WikiCommandResult BackendTrace,
    WikiCommandResult TestPlan) {
    public DevelopmentContext WithoutRawOutput() => this with {
        ChangeContext = ChangeContext.WithoutRawOutput(),
        BackendTrace = BackendTrace.WithoutRawOutput(),
        TestPlan = TestPlan.WithoutRawOutput(),
    };
}
