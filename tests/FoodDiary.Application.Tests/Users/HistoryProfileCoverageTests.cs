using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Queries.GetWaistGoalHistory;
using FoodDiary.Application.Users.Queries.GetWeightGoalHistory;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class HistoryProfileCoverageTests {
    [Fact]
    public async Task GoalHistoryHandlers_WithInvalidCurrentUser_ReturnAccessFailures() {
        IUserProfileReadService profiles = Substitute.For<IUserProfileReadService>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();

        Result<IReadOnlyList<WeightGoalHistoryModel>> weight = await new GetWeightGoalHistoryQueryHandler(profiles, access)
            .Handle(new GetWeightGoalHistoryQuery(Guid.Empty), CancellationToken.None);
        Result<IReadOnlyList<WaistGoalHistoryModel>> waist = await new GetWaistGoalHistoryQueryHandler(profiles, access)
            .Handle(new GetWaistGoalHistoryQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
    }

    [Fact]
    public async Task UserContextService_HistoryMethods_WhenUserMissing_ReturnFailures() {
        UserContextService service = CreateService(user: null);
        var userId = UserId.New();

        Result<IReadOnlyList<WeightGoalHistoryModel>> weight = await service.GetWeightGoalHistoryAsync(userId, CancellationToken.None);
        Result<IReadOnlyList<WaistGoalHistoryModel>> waist = await service.GetWaistGoalHistoryAsync(userId, CancellationToken.None);
        Result<WeightHistoryProfileModel> weightProfile = await service.GetWeightHistoryProfileAsync(userId, CancellationToken.None);
        Result<WaistHistoryProfileModel> waistProfile = await service.GetWaistHistoryProfileAsync(userId, CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
        ResultAssert.Failure(weightProfile);
        ResultAssert.Failure(waistProfile);
    }

    [Fact]
    public async Task UserContextService_HistoryProfiles_MapGoalsAndHistory() {
        var user = User.Create("history-profile@example.com", "hash");
        DateTime first = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        user.StartWeightGoal(75, 82, first);
        user.StartWeightGoal(72, 80, first.AddDays(1));
        user.StartWaistGoal(80, 90, first);
        user.StartWaistGoal(76, 86, first.AddDays(1));
        UserContextService service = CreateService(user);

        Result<WeightHistoryProfileModel> weight = await service.GetWeightHistoryProfileAsync(user.Id, CancellationToken.None);
        Result<WaistHistoryProfileModel> waist = await service.GetWaistHistoryProfileAsync(user.Id, CancellationToken.None);

        ResultAssert.Success(weight);
        ResultAssert.Success(waist);
        Assert.Multiple(
            () => Assert.Equal(72, weight.Value.Goal.DesiredWeight),
            () => Assert.Equal(80, weight.Value.Goal.StartWeight),
            () => Assert.Equal(2, weight.Value.GoalHistory.Count),
            () => Assert.Equal("Active", weight.Value.GoalHistory[0].Status),
            () => Assert.Equal(76, waist.Value.Goal.DesiredWaist),
            () => Assert.Equal(86, waist.Value.Goal.StartWaist),
            () => Assert.Equal(2, waist.Value.GoalHistory.Count),
            () => Assert.Equal("Active", waist.Value.GoalHistory[0].Status));
    }

    private static UserContextService CreateService(User? user) {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        lookup.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        return new UserContextService(lookup, Substitute.For<IUserWriteRepository>());
    }
}
