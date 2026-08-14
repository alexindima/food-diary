namespace FoodDiary.Development.Mcp;

public sealed record DevelopmentContext(
    string SnapshotFingerprint,
    string GitHead,
    WikiCommandResult ChangeContext,
    WikiCommandResult BackendTrace,
    WikiCommandResult TestPlan);
