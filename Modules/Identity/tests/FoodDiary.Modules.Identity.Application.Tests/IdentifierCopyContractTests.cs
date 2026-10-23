using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Fact]
    public void TelegramLeaseId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new TelegramLeaseId(originalValue);
        TelegramLeaseId copied = original with { Value = replacementValue };
        var expected = new TelegramLeaseId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied),
            () => Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Assert.Equal(Guid.Empty, TelegramLeaseId.Empty.Value));
    }

    [Fact]
    public void TelegramOperationId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new TelegramOperationId(originalValue);
        TelegramOperationId copied = original with { Value = replacementValue };
        var expected = new TelegramOperationId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied),
            () => Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Assert.Equal(Guid.Empty, TelegramOperationId.Empty.Value));
    }
}
