using FluentValidation.TestHelper;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

namespace FoodDiary.Application.Tests.Dashboard;

[ExcludeFromCodeCoverage]
public class DashboardValidatorTests {
    [Fact]
    public async Task GetDashboardSnapshot_WithNullUserId_HasError() {
        TestValidationResult<GetDashboardSnapshotQuery> result = await new GetDashboardSnapshotQueryValidator().TestValidateAsync(
            new GetDashboardSnapshotQuery(UserId: null, DateTime.UtcNow, 1, 10, "en", 7));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Theory]
    [InlineData(-841)]
    [InlineData(841)]
    public async Task GetDashboardSnapshot_WithInvalidTimeZoneOffset_HasError(int offsetMinutes) {
        TestValidationResult<GetDashboardSnapshotQuery> result = await new GetDashboardSnapshotQueryValidator().TestValidateAsync(
            new GetDashboardSnapshotQuery(Guid.NewGuid(), DateTime.UtcNow, 1, 10, "en", 7, offsetMinutes));

        result.ShouldHaveValidationErrorFor(query => query.TimeZoneOffsetMinutes);
    }

}
