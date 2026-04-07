using FluentValidation.TestHelper;
using FoodDiary.Application.Export.Queries.ExportDiary;

namespace FoodDiary.Application.Tests.Export;

public class ExportValidatorTests {
    private readonly ExportDiaryQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithNullUserId_HasError() {
        var query = new ExportDiaryQuery(null, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task Validate_WithDateFromAfterDateTo_HasError() {
        var query = new ExportDiaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.DateFrom);
    }

    [Fact]
    public async Task Validate_WithRangeOverOneYear_HasError() {
        var query = new ExportDiaryQuery(
            Guid.NewGuid(),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var result = await _validator.TestValidateAsync(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithValidQuery_NoErrors() {
        var query = new ExportDiaryQuery(
            Guid.NewGuid(),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc));
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithSameDate_NoErrors() {
        var date = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new ExportDiaryQuery(Guid.NewGuid(), date, date);
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
