using FoodDiary.Application.Meals.Common.Time;

namespace FoodDiary.Application.Tests.Meals;

[ExcludeFromCodeCoverage]
public sealed class UtcDateNormalizerTests {
    [Fact]
    public void NormalizeInstantPreservingUnspecifiedAsUtc_CoversAllDateTimeKinds() {
        var utc = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);
        var local = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Unspecified);

        DateTime normalizedUtc = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(utc);
        DateTime normalizedLocal = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local);
        DateTime normalizedUnspecified = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified);

        Assert.Multiple(
            () => Assert.Equal(utc, normalizedUtc),
            () => Assert.Equal(local.ToUniversalTime(), normalizedLocal),
            () => Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind),
            () => Assert.Equal(unspecified.Ticks, normalizedUnspecified.Ticks));
    }

    [Fact]
    public void NormalizeDateUsingLocalFallback_ReturnsStartOfUtcDate() {
        var value = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);

        DateTime normalized = UtcDateNormalizer.NormalizeDateUsingLocalFallback(value);

        Assert.Equal(new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc), normalized);
    }

    [Fact]
    public void NormalizeDateEndUsingLocalFallback_ReturnsEndOfUtcDate() {
        var value = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);

        DateTime normalized = UtcDateNormalizer.NormalizeDateEndUsingLocalFallback(value);

        Assert.Equal(new DateTime(2026, 8, 14, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), normalized);
    }
}
