namespace FoodDiary.Presentation.Api.Features.WaistEntries.Responses;

public sealed record WaistEntryHttpResponse(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double Circumference);
