using DashboardUtcDateNormalizer = FoodDiary.Application.Dashboard.Internal.UtcDateNormalizer;
using ExportUtcDateNormalizer = FoodDiary.Application.Export.Internal.UtcDateNormalizer;
using HydrationUtcDateNormalizer = FoodDiary.Application.Hydration.Internal.UtcDateNormalizer;

namespace FoodDiary.Application.Tests.Time;

[ExcludeFromCodeCoverage]
public sealed class AdditionalUtcDateNormalizerTests {
    [Fact]
    public void Normalizers_CoverLocalAndUnspecifiedInputs() {
        var local = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Unspecified);
        var utc = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);

        DateTime exportLocal = ExportUtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local);
        DateTime exportUnspecified = ExportUtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified);
        DateTime hydration = HydrationUtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified);
        DateTime dashboard = DashboardUtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(unspecified);
        DateTime hydrationUtc = HydrationUtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(utc);
        DateTime dashboardUtc = DashboardUtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(utc);
        DateTime hydrationLocal = HydrationUtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local);
        DateTime dashboardLocal = DashboardUtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(local);

        Assert.Multiple(
            () => Assert.Equal(local.ToUniversalTime(), exportLocal),
            () => Assert.Equal(DateTimeKind.Utc, exportUnspecified.Kind),
            () => Assert.Equal(DateTimeKind.Utc, hydration.Kind),
            () => Assert.Equal(DateTimeKind.Utc, dashboard.Kind),
            () => Assert.Equal(utc, hydrationUtc),
            () => Assert.Equal(utc.Date, dashboardUtc),
            () => Assert.Equal(local.ToUniversalTime(), hydrationLocal),
            () => Assert.Equal(local.ToUniversalTime().Date, dashboardLocal));
    }
}
