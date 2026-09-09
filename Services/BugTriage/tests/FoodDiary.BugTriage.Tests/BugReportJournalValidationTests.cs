using System.ComponentModel.DataAnnotations;
using FoodDiary.BugTriage.Presentation.Features.Reports;

namespace FoodDiary.BugTriage.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class BugReportJournalValidationTests {
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(0, null, true)]
    [InlineData(null, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    public void DateRange_RequiresStartBeforeExclusiveEnd(int? start, int? end, bool valid) {
        var query = new BugReportJournalHttpQuery(FromUtc: start.HasValue ? DateTimeOffset.UnixEpoch.AddDays(start.Value) : null,
            ToUtc: end.HasValue ? DateTimeOffset.UnixEpoch.AddDays(end.Value) : null);
        var errors = new List<ValidationResult>();
        Assert.Equal(valid, Validator.TryValidateObject(query, new ValidationContext(query), errors, validateAllProperties: true));
        if (!valid) {
            Assert.Equal(new[] { nameof(query.FromUtc), nameof(query.ToUtc) }, Assert.Single(errors).MemberNames, StringComparer.Ordinal);
        }
    }
}
