using FoodDiary.BugTriage.Application.AuditRuns;
using FoodDiary.BugTriage.Application.Reports.Identifiers;

namespace FoodDiary.BugTriage.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class IdentifierCopyContractTests {
    [Xunit.Fact]
    public void AuditRunId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new AuditRunId(originalValue);
        AuditRunId copied = original with { Value = replacementValue };
        var expected = new AuditRunId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied));
    }

    [Xunit.Fact]
    public void BugReportId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new BugReportId(originalValue);
        BugReportId copied = original with { Value = replacementValue };
        var expected = new BugReportId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied),
            () => Xunit.Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Xunit.Assert.Equal(Guid.Empty, BugReportId.Empty.Value));
    }

    [Xunit.Fact]
    public void LeaseToken_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new LeaseToken(originalValue);
        LeaseToken copied = original with { Value = replacementValue };
        var expected = new LeaseToken(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied),
            () => Xunit.Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Xunit.Assert.Equal(Guid.Empty, LeaseToken.Empty.Value));
    }

    [Xunit.Fact]
    public void SourceMessageId_CopyPreservesOriginalAndRebindsValue() {
        var originalValue = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacementValue = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        var original = new SourceMessageId(originalValue);
        SourceMessageId copied = original with { Value = replacementValue };
        var expected = new SourceMessageId(replacementValue);

        Xunit.Assert.Multiple(
            () => Xunit.Assert.Equal(originalValue, original.Value),
            () => Xunit.Assert.Equal(replacementValue, copied.Value),
            () => Xunit.Assert.Equal(expected, copied),
            () => Xunit.Assert.Equal(expected.GetHashCode(), copied.GetHashCode()),
            () => Xunit.Assert.NotEqual(original, copied),
            () => Xunit.Assert.Equal("87654321-4321-8765-cba9-876543210fed", copied.ToString()),
            () => Xunit.Assert.Equal(Guid.Empty, SourceMessageId.Empty.Value));
    }
}
