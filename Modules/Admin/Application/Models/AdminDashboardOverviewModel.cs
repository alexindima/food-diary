namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminDashboardOverviewModel(DateTime FromUtc, DateTime ToUtc, string Interval,
    AdminDashboardPeriodModel Period, AdminDashboardPeriodModel? Previous,
    int TotalUsersNow, int PremiumUsersNow, int PendingReportsNow);
