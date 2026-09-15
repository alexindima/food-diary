using FoodDiary.Modules.Dashboard.Application.Internal;

namespace FoodDiary.Modules.Dashboard.Application.Tests.Time;

[ExcludeFromCodeCoverage]
public sealed class UtcDateNormalizerTests {
    [Fact]
    public void Normalizers_CoverLocalAndUnspecifiedInputs() {
        var local = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Unspecified);
        var utc = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);

        DateTime normalizedLocal = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(local);
        DateTime normalizedUnspecified = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(unspecified);
        DateTime normalizedUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(utc);

        Assert.Multiple(
            () => Assert.Equal(local.ToUniversalTime().Date, normalizedLocal),
            () => Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind),
            () => Assert.Equal(utc.Date, normalizedUnspecified),
            () => Assert.Equal(utc.Date, normalizedUtc));
    }
}
