using FoodDiary.Application.Cycles.Commands.DeleteCycleProfile;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {
    [Fact]
    public async Task DeleteCycleProfileCommandHandler_WithOwnedProfile_DeletesProfile() {
        var user = User.Create("cycle-delete@example.com", "hash");
        var profile = CycleProfile.Create(
            user.Id,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new DeleteCycleProfileCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteCycleProfileCommand(user.Id.Value, profile.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.WasDeleted);
    }

    [Fact]
    public async Task DeleteCycleProfileCommandHandler_WithForeignProfile_ReturnsNotFound() {
        var owner = User.Create("cycle-delete-owner@example.com", "hash");
        var currentUser = User.Create("cycle-delete-current@example.com", "hash");
        var profile = CycleProfile.Create(
            owner.Id,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new DeleteCycleProfileCommandHandler(repository, CreateCurrentUserAccessService(currentUser));

        Result result = await handler.Handle(
            new DeleteCycleProfileCommand(currentUser.Id.Value, profile.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
        Assert.False(repository.WasDeleted);
    }
}
