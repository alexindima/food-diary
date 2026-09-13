namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminDashboardMetrics(
    int Registrations, int PayingUsers, long AiTokens,
    IReadOnlyList<AdminDashboardTrend> Trend);
