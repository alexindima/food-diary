namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminDashboardTrend(DateTime Date, int Registrations, long AiTokens,
    IReadOnlyList<AdminDashboardRevenuePoint> Revenue);
