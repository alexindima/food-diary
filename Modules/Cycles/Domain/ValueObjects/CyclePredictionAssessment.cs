namespace FoodDiary.Modules.Cycles.Domain.ValueObjects;

public sealed record CyclePredictionAssessment {
    public PredictionWindow Window { get; }
    public PredictionConfidence Confidence { get; }
    public PredictionDataSufficiency Sufficiency { get; }
    public PredictionPatternConsistency Consistency { get; }
    public IReadOnlyCollection<PredictionReasonCode> Reasons { get; }

    public CyclePredictionAssessment(PredictionWindow window, PredictionConfidence confidence,
        PredictionDataSufficiency sufficiency, PredictionPatternConsistency consistency,
        IReadOnlyCollection<PredictionReasonCode> reasons) {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(confidence);
        ArgumentNullException.ThrowIfNull(sufficiency);
        ArgumentNullException.ThrowIfNull(consistency);
        ArgumentNullException.ThrowIfNull(reasons);
        if (reasons.Any(reason => reason is null)) {
            throw new ArgumentException("Prediction reasons cannot contain null values.", nameof(reasons));
        }
        Window = window;
        Confidence = confidence;
        Sufficiency = sufficiency;
        Consistency = consistency;
        Reasons = Array.AsReadOnly(reasons.ToArray());
    }
}
