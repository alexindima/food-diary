namespace FoodDiary.Application.Admin.Models;

public sealed record AdminDashboardSummaryModel(
    int TotalUsers,
    int ActiveUsers,
    int PremiumUsers,
    int DeletedUsers,
    IReadOnlyList<AdminUserModel> RecentUsers);
