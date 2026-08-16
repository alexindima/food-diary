namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record UpdateCycleSettingsHttpRequest(
    int Mode,
    int AverageCycleLength,
    int AveragePeriodLength,
    int LutealLength,
    bool IsRegular,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications);
