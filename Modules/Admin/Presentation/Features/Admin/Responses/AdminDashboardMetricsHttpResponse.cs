namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminDashboardMetricsHttpResponse(int Registrations, int PayingUsers, long AiTokens,
    IReadOnlyList<AdminDashboardTrendHttpResponse> Trend);
