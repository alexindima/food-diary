namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminDashboardMetrics(
    int Registrations, int PayingUsers, long AiTokens,
    IReadOnlyList<AdminDashboardTrend> Trend);
