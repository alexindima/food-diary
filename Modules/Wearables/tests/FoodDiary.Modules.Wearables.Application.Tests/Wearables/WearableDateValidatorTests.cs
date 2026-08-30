using FluentValidation.TestHelper;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;

namespace FoodDiary.Application.Tests.Wearables;

[ExcludeFromCodeCoverage]
public sealed class WearableDateValidatorTests {
    private static readonly DateTime CurrentUtc = new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider CurrentTime = new StubTimeProvider(CurrentUtc);

    [Theory]
    [InlineData(1970, 1, 1)]
    [InlineData(2026, 8, 18)]
    public void SyncValidator_WithSupportedDate_HasNoErrors(int year, int month, int day) {
        var validator = new SyncWearableDataCommandValidator(CurrentTime);
        var command = new SyncWearableDataCommand(Guid.NewGuid(), "fitbit", new DateTime(year, month, day));

        TestValidationResult<SyncWearableDataCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(candidate => candidate.Date);
    }

    [Theory]
    [MemberData(nameof(UnsupportedDates))]
    public void SyncValidator_WithUnsupportedDate_ReturnsValidationError(DateTime date) {
        var validator = new SyncWearableDataCommandValidator(CurrentTime);
        var command = new SyncWearableDataCommand(Guid.NewGuid(), "fitbit", date);

        TestValidationResult<SyncWearableDataCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(candidate => candidate.Date)
            .WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [MemberData(nameof(UnsupportedDates))]
    public void DailySummaryValidator_WithUnsupportedDate_ReturnsValidationError(DateTime date) {
        var validator = new GetWearableDailySummaryQueryValidator(CurrentTime);
        var query = new GetWearableDailySummaryQuery(Guid.NewGuid(), date);

        TestValidationResult<GetWearableDailySummaryQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(candidate => candidate.Date)
            .WithErrorCode("Validation.Invalid");
    }

    public static TheoryData<DateTime> UnsupportedDates => [
        new DateTime(1969, 12, 31),
        CurrentUtc.Date.AddDays(1),
        DateTime.MaxValue,
    ];

    [ExcludeFromCodeCoverage]
    private sealed class StubTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
