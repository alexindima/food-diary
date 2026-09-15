using FoodDiary.Modules.Export.Application.Internal;

namespace FoodDiary.Modules.Export.Application.Tests.Time;

[ExcludeFromCodeCoverage]
public sealed class UtcDateNormalizerTests {
    [Fact]
    public void Normalizers_CoverLocalAndUnspecifiedInputs() {
        var local = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Unspecified);
        var utc = new DateTime(2026, 8, 14, 12, 30, 0, DateTimeKind.Utc);

        DateTime normalizedLocal = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local);
        DateTime normalizedUnspecified = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified);
        DateTime normalizedUtc = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(utc);

        Assert.Multiple(
            () => Assert.Equal(local.ToUniversalTime(), normalizedLocal),
            () => Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind),
            () => Assert.Equal(utc, normalizedUnspecified),
            () => Assert.Equal(utc, normalizedUtc));
    }
}
