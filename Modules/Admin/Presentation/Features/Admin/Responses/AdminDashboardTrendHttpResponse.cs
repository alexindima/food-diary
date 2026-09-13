namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminDashboardTrendHttpResponse(DateTime Date, int Registrations, long AiTokens,
    IReadOnlyList<AdminDashboardRevenuePointHttpResponse> Revenue);
