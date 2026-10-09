using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Cycles.Domain.ValueObjects;

public sealed record PredictionReasonCode {
    public string Code { get; }
    private PredictionReasonCode(string code) => Code = code;
    public static PredictionReasonCode PredictionPausedByState { get; } = new("prediction_paused_by_state");
    public static PredictionReasonCode AmbiguousEpisodeHistory { get; } = new("ambiguous_episode_history");
    public static PredictionReasonCode InsufficientCompletedCycles { get; } = new("insufficient_completed_cycles");
    public static PredictionReasonCode EstimatedFromCompletedCycles { get; } = new("estimated_from_completed_cycles");
    public static PredictionReasonCode FertilityEstimateNotAvailableInV2 { get; } = new("fertility_estimate_not_available_in_v2");

    public static PredictionReasonCode FromCode(string code) => new(DomainGuard.RequiredText(code, 512, nameof(code)));
    public static PredictionReasonCode FromStoredCode(string code) => new(code);
}
