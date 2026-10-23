using FoodDiary.BugTriage.Application.AuditRuns;
using FoodDiary.BugTriage.Application.Reports.Identifiers;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Fact]
    public void AuditRunId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new AuditRunId(originalValue);
        AuditRunId copied = original with { Value = replacementValue };
        var expected = new AuditRunId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied));
    }

    [Fact]
    public void BugReportId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new BugReportId(originalValue);
        BugReportId copied = original with { Value = replacementValue };
        var expected = new BugReportId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied),
            () => Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Assert.Equal(Guid.Empty, BugReportId.Empty.Value));
    }

    [Fact]
    public void LeaseToken_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new LeaseToken(originalValue);
        LeaseToken copied = original with { Value = replacementValue };
        var expected = new LeaseToken(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied),
            () => Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Assert.Equal(Guid.Empty, LeaseToken.Empty.Value));
    }

    [Fact]
    public void SourceMessageId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new SourceMessageId(originalValue);
        SourceMessageId copied = original with { Value = replacementValue };
        var expected = new SourceMessageId(replacementValue);

        Assert.Multiple(
            () => Assert.Equal(originalValue, original.Value),
            () => Assert.Equal(replacementValue, copied.Value),
            () => Assert.Equal(expected, copied),
            () => Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Assert.NotEqual(original, copied),
            () => Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Assert.Equal(Guid.Empty, SourceMessageId.Empty.Value));
    }
}
