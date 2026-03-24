namespace FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

public sealed record CreateWaistEntryHttpRequest(
    DateTime Date,
    double Circumference);
