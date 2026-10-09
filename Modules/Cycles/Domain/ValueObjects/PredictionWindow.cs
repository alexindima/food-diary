namespace FoodDiary.Modules.Cycles.Domain.ValueObjects;

public sealed record PredictionWindow {
    public DateOnly? From { get; }
    public DateOnly? To { get; }
    private PredictionWindow(DateOnly? from, DateOnly? to) {
        From = from;
        To = to;
    }

    public static PredictionWindow FromEndpoints(DateOnly? from, DateOnly? to) {
        if (from.HasValue && to.HasValue && from > to) {
            throw new ArgumentException("Prediction start date cannot be after its end date.", nameof(from));
        }
        return new PredictionWindow(from, to);
    }

    public static PredictionWindow FromStoredEndpoints(DateOnly? from, DateOnly? to) => new(from, to);
}
