using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Fact]
    public void RefreshTokenSessionId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new RefreshTokenSessionId(originalValue);
        RefreshTokenSessionId copied = original with { Value = replacementValue };
        var expected = new RefreshTokenSessionId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied));
    }
}
