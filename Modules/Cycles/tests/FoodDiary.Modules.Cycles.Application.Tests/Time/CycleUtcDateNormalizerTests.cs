using FoodDiary.Application.Cycles.Internal;

namespace FoodDiary.Application.Tests.Time;

[ExcludeFromCodeCoverage]
public sealed class CycleUtcDateNormalizerTests {
    [Fact]
    public void NormalizeInstantPreservingUnspecifiedAsUtc_HandlesEveryDateTimeKind() {
        var local = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Unspecified);
        var utc = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);

        DateTime normalizedLocal = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local);
        DateTime normalizedUnspecified = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified);
        DateTime normalizedUtc = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(utc);

        Assert.Multiple(
            () => Assert.Equal(local.ToUniversalTime(), normalizedLocal),
            () => Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind),
            () => Assert.Equal(unspecified, normalizedUnspecified),
            () => Assert.Equal(utc, normalizedUtc));
    }
}
