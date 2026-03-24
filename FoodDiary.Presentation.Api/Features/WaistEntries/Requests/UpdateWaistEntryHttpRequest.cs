namespace FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

public sealed record UpdateWaistEntryHttpRequest(
    DateTime Date,
    double Circumference);
