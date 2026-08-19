namespace FoodDiary.Presentation.Api.Policies;

public static class PresentationQueryLimits {
    public const int MinimumPage = 1;
    public const int MaximumPage = 10_000;
    public const int MinimumPageSize = 1;
    public const int MaximumPageSize = 100;
    public const int MaximumCollectionSize = 1_000;
    public const int MaximumRecentItems = 50;
    public const int MaximumQuantizationDays = 366;
    public const int MaximumHistoryEntries = 500;
    public const int MaximumDashboardTrendDays = 31;
    public const int MinimumTimeZoneOffsetMinutes = -840;
    public const int MaximumTimeZoneOffsetMinutes = 840;
    public const int MaximumAdminDashboardRecentItems = 20;
    public const int MaximumAdminMailInboxMessages = 200;
    public const int MaximumAdminUserRoleAuditEntries = 50;
    public const int MaximumCollaborationAuditEntries = 500;
    public const int MaximumFastingTelemetryHours = 168;
    public const int MaximumMarketingAttributionHours = 2_160;
    public const int MaximumSearchLength = 128;
    public const int MaximumCategoryLength = 64;
    public const int MaximumFilterLength = 64;
    public const int MaximumCsvFilterLength = 256;
    public const int MaximumLocaleLength = 10;
    public const int MaximumSortLength = 32;
}
