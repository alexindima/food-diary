using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Domain.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Xunit.Fact]
    public void WaistEntryId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new WaistEntryId(originalValue);
        WaistEntryId copied = original with { Value = replacementValue };
        var expected = new WaistEntryId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied));
    }

    [Xunit.Fact]
    public void WeightEntryId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new WeightEntryId(originalValue);
        WeightEntryId copied = original with { Value = replacementValue };
        var expected = new WeightEntryId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied));
    }
}
