namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record CreateCycleHttpRequest(
    DateTime TrackingStartDate,
    int Mode,
    int? AverageCycleLength,
    int? AveragePeriodLength,
    int? LutealLength,
    bool IsRegular,
    bool IsOnboardingComplete,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications,
    string? Notes);
