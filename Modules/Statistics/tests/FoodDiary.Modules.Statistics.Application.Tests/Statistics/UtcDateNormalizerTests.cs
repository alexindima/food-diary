using FoodDiary.Application.Statistics.Common;

namespace FoodDiary.Application.Tests.Statistics;

[ExcludeFromCodeCoverage]
public sealed class UtcDateNormalizerTests {
    [Fact]
    public void NormalizeInstantPreservingUnspecifiedAsUtc_PreservesUtcAndNormalizesOtherKinds() {
        var utc = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);
        var local = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Unspecified);

        DateTime normalizedUtc = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(utc);
        DateTime normalizedLocal = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local);
        DateTime normalizedUnspecified = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified);

        Assert.Multiple(
            () => Assert.Equal(utc, normalizedUtc),
            () => Assert.Equal(local.ToUniversalTime(), normalizedLocal),
            () => Assert.Equal(unspecified, normalizedUnspecified),
            () => Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind));
    }

    [Fact]
    public void NormalizeDatePreservingUnspecifiedAsUtc_TruncatesAfterUtcNormalization() {
        var value = new DateTime(2026, 8, 14, 23, 30, 0, DateTimeKind.Local);

        DateTime normalized = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(value);

        Assert.Multiple(
            () => Assert.Equal(value.ToUniversalTime().Date, normalized),
            () => Assert.Equal(DateTimeKind.Utc, normalized.Kind));
    }
}
