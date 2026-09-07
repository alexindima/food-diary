namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminDashboardTrendHttpResponse(DateTime Date, int Registrations, long AiTokens,
    IReadOnlyList<AdminDashboardRevenuePointHttpResponse> Revenue);
