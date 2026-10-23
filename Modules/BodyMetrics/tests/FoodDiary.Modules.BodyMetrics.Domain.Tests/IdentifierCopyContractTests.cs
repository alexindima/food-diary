using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Fact]
    public void WaistEntryId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new WaistEntryId(originalValue);
        WaistEntryId copied = original with { Value = replacementValue };
        var expected = new WaistEntryId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied));
    }

    [Fact]
    public void WeightEntryId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new WeightEntryId(originalValue);
        WeightEntryId copied = original with { Value = replacementValue };
        var expected = new WeightEntryId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied));
    }
}
