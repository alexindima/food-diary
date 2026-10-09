using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Cycles.Domain.ValueObjects;

public sealed record PredictionConfidence {
    public string Code { get; }
    private PredictionConfidence(string code) => Code = code;
    public static PredictionConfidence Learning { get; } = new("Learning");
    public static PredictionConfidence Low { get; } = new("Low");
    public static PredictionConfidence Medium { get; } = new("Medium");
    public static PredictionConfidence High { get; } = new("High");

    public static PredictionConfidence FromCode(string code) => new(DomainGuard.RequiredText(code, 32, nameof(code)));
    public static PredictionConfidence FromStoredCode(string code) => new(code);
}
