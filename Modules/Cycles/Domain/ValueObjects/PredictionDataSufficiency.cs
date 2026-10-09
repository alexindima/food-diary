using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Cycles.Domain.ValueObjects;

public sealed record PredictionDataSufficiency {
    public string Code { get; }
    private PredictionDataSufficiency(string code) => Code = code;
    public static PredictionDataSufficiency Unavailable { get; } = new("Unavailable");
    public static PredictionDataSufficiency Insufficient { get; } = new("Insufficient");
    public static PredictionDataSufficiency Limited { get; } = new("Limited");
    public static PredictionDataSufficiency Established { get; } = new("Established");

    public static PredictionDataSufficiency FromCode(string code) => new(DomainGuard.RequiredText(code, 32, nameof(code)));
    public static PredictionDataSufficiency FromStoredCode(string code) => new(code);
}
