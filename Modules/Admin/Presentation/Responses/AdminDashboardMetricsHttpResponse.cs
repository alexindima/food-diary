namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminDashboardMetricsHttpResponse(int Registrations, int PayingUsers, long AiTokens,
    IReadOnlyList<AdminDashboardTrendHttpResponse> Trend);
