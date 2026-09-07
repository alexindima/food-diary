namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminDashboardTrend(DateTime Date, int Registrations, long AiTokens,
    IReadOnlyList<AdminDashboardRevenuePoint> Revenue);
