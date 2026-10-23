using FoodDiary.Modules.DailyAdvices.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.DailyAdvices.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Fact]
    public void DailyAdviceId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new DailyAdviceId(originalValue);
        DailyAdviceId copied = original with { Value = replacementValue };
        var expected = new DailyAdviceId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied));
    }
}
