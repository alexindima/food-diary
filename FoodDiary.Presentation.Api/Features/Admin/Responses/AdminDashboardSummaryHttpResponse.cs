namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminDashboardSummaryHttpResponse(
    int TotalUsers,
    int ActiveUsers,
    int PremiumUsers,
    int DeletedUsers,
    IReadOnlyList<AdminUserHttpResponse> RecentUsers);
