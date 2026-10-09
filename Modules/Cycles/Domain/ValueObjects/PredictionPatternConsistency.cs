using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Cycles.Domain.ValueObjects;

public sealed record PredictionPatternConsistency {
    public string Code { get; }
    private PredictionPatternConsistency(string code) => Code = code;
    public static PredictionPatternConsistency Unavailable { get; } = new("Unavailable");
    public static PredictionPatternConsistency Limited { get; } = new("Limited");
    public static PredictionPatternConsistency Consistent { get; } = new("Consistent");

    public static PredictionPatternConsistency FromCode(string code) => new(DomainGuard.RequiredText(code, 32, nameof(code)));
    public static PredictionPatternConsistency FromStoredCode(string code) => new(code);
}
