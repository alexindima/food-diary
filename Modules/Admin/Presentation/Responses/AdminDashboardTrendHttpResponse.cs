namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminDashboardTrendHttpResponse(DateTime Date, int Registrations, long AiTokens,
    IReadOnlyList<AdminDashboardRevenuePointHttpResponse> Revenue);
