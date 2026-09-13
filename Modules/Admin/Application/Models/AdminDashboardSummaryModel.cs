namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminDashboardSummaryModel(
    int TotalUsers,
    int ActiveUsers,
    int PremiumUsers,
    int DeletedUsers,
    int PendingReportsCount,
    IReadOnlyList<AdminUserModel> RecentUsers);
