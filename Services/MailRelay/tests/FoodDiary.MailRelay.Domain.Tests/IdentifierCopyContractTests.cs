using FoodDiary.MailRelay.Domain.Emails;

namespace FoodDiary.MailRelay.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Fact]
    public void MailRelayOutboxId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new MailRelayOutboxId(originalValue);
        MailRelayOutboxId copied = original with { Value = replacementValue };
        var expected = new MailRelayOutboxId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied),
            () => Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()));
    }

    [Fact]
    public void QueuedEmailId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new QueuedEmailId(originalValue);
        QueuedEmailId copied = original with { Value = replacementValue };
        var expected = new QueuedEmailId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied));
    }
}
