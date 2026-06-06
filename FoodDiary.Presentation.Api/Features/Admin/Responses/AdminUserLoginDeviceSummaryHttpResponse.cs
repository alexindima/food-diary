namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminUserLoginDeviceSummaryHttpResponse(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
