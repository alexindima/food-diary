namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminUserLoginDeviceSummaryHttpResponse(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
