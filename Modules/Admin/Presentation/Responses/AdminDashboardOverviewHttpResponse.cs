namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminDashboardOverviewHttpResponse(DateTime FromUtc, DateTime ToUtc, string Interval,
    AdminDashboardPeriodHttpResponse Period, AdminDashboardPeriodHttpResponse? Previous,
    int TotalUsersNow, int PremiumUsersNow, int PendingReportsNow);
