using FluentValidation.TestHelper;
using FoodDiary.Application.Export.Queries.ExportCycle;
using FoodDiary.Application.Export.Queries.ExportDiary;

namespace FoodDiary.Application.Tests.Export;

[ExcludeFromCodeCoverage]
public class ExportValidatorTests {
    private readonly ExportDiaryQueryValidator _validator = new();
    private readonly ExportCycleQueryValidator _cycleValidator = new();

    [Fact]
    public async Task Validate_WithNullUserId_HasError() {
        var query = new ExportDiaryQuery(UserId: null, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task Validate_WithDateFromAfterDateTo_HasError() {
        var query = new ExportDiaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldHaveValidationErrorFor(q => q.DateFrom);
    }

    [Fact]
    public async Task Validate_WithRangeOverOneYear_HasError() {
        var query = new ExportDiaryQuery(
            Guid.NewGuid(),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithValidQuery_NoErrors() {
        var query = new ExportDiaryQuery(
            Guid.NewGuid(),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc));
        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithSameDate_NoErrors() {
        var date = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new ExportDiaryQuery(Guid.NewGuid(), date, date);
        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(840)]
    [InlineData(null)]
    public async Task Validate_WithMaximumDateThatOverflowsDisplayOffset_HasError(int? timeZoneOffsetMinutes) {
        var query = new ExportDiaryQuery(
            Guid.NewGuid(),
            DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
            DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
            TimeZoneOffsetMinutes: timeZoneOffsetMinutes);

        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.DateFrom);
    }

    [Fact]
    public async Task Validate_WithMinimumDateAndNegativeOffset_HasError() {
        var query = new ExportDiaryQuery(
            Guid.NewGuid(),
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            TimeZoneOffsetMinutes: -840);

        TestValidationResult<ExportDiaryQuery> result = await _validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.DateFrom);
    }

    [Fact]
    public async Task ValidateCycle_WithNullUserId_HasError() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new ExportCycleQuery(UserId: null, today, today.AddDays(7));

        TestValidationResult<ExportCycleQuery> result = await _cycleValidator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task ValidateCycle_WithDateFromAfterDateTo_HasError() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new ExportCycleQuery(Guid.NewGuid(), today.AddDays(7), today);

        TestValidationResult<ExportCycleQuery> result = await _cycleValidator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.DateFrom);
    }

    [Fact]
    public async Task ValidateCycle_WithRangeOverOneYear_HasError() {
        var query = new ExportCycleQuery(
            Guid.NewGuid(),
            new DateOnly(2025, 1, 1),
            new DateOnly(2026, 2, 1));

        TestValidationResult<ExportCycleQuery> result = await _cycleValidator.TestValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateCycle_WithValidQuery_NoErrors() {
        var query = new ExportCycleQuery(
            Guid.NewGuid(),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 7));

        TestValidationResult<ExportCycleQuery> result = await _cycleValidator.TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
