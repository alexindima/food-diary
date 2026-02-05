namespace FoodDiary.Contracts.Admin;

public sealed record AdminDashboardSummaryResponse(
    int TotalUsers,
    int ActiveUsers,
    int PremiumUsers,
    int DeletedUsers,
    IReadOnlyList<AdminUserResponse> RecentUsers);
