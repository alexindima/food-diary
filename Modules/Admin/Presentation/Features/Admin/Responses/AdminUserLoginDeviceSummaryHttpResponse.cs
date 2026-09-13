namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminUserLoginDeviceSummaryHttpResponse(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
