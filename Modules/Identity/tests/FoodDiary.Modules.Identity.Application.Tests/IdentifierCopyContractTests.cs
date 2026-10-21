using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Xunit.Fact]
    public void TelegramLeaseId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new TelegramLeaseId(originalValue);
        TelegramLeaseId copied = original with { Value = replacementValue };
        var expected = new TelegramLeaseId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied),
            () => Xunit.Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Xunit.Assert.Equal(Guid.Empty, TelegramLeaseId.Empty.Value));
    }

    [Xunit.Fact]
    public void TelegramOperationId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new TelegramOperationId(originalValue);
        TelegramOperationId copied = original with { Value = replacementValue };
        var expected = new TelegramOperationId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied),
            () => Xunit.Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Xunit.Assert.Equal(Guid.Empty, TelegramOperationId.Empty.Value));
    }
}
