using FoodDiary.Application.Cycles.Commands.UpdateMenstrualEpisode;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {
    [Fact]
    public async Task UpdateMenstrualEpisodeCommandHandler_WithConfirmedEpisode_UpdatesProfile() {
        var user = User.Create("cycle-episode-update@example.com", "hash");
        DateTime start = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, start);
        MenstrualEpisode episode = profile.ConfirmPeriodStart(start);
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateMenstrualEpisodeCommand(
                user.Id.Value,
                profile.Id.Value,
                episode.Id.Value,
                start.AddDays(-1),
                start.AddDays(4)),
            CancellationToken.None);

        CycleModel model = ResultAssert.Success(result);
        MenstrualEpisodeModel updated = Assert.Single(model.MenstrualEpisodes!);
        Assert.Multiple(
            () => Assert.Equal(start.AddDays(-1), updated.StartDate),
            () => Assert.Equal(start.AddDays(4), updated.EndDate),
            () => Assert.True(repository.WasUpdated));
    }

    [Fact]
    public async Task UpdateMenstrualEpisodeCommandHandler_WhenEpisodeDoesNotBelongToProfile_ReturnsValidationFailure() {
        var user = User.Create("cycle-episode-missing@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateMenstrualEpisodeCommand(
                user.Id.Value,
                profile.Id.Value,
                Guid.NewGuid(),
                DateTime.UtcNow,
                EndDate: null),
            CancellationToken.None);

        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }
}
