using FoodDiary.Application.Cycles.Commands.DeleteMenstrualEpisode;
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
        DateOnly start = new(2026, 4, 1);
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
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateMenstrualEpisodeCommand(
                user.Id.Value,
                profile.Id.Value,
                Guid.NewGuid(),
                DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate: null),
            CancellationToken.None);

        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task UpdateMenstrualEpisodeCommandHandler_WithPredictionExclusion_UpdatesPredictionEligibility() {
        var user = User.Create("cycle-episode-exclusion@example.com", "hash");
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, start);
        MenstrualEpisode episode = profile.ConfirmPeriodStart(start);
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateMenstrualEpisodeCommand(
                user.Id.Value,
                profile.Id.Value,
                episode.Id.Value,
                start,
                start.AddDays(3),
                ExcludedFromPredictions: true),
            CancellationToken.None);

        CycleModel model = ResultAssert.Success(result);
        Assert.True(Assert.Single(model.MenstrualEpisodes!).ExcludedFromPredictions);
    }

    [Fact]
    public async Task DeleteMenstrualEpisodeCommandHandler_WithConfirmedEpisode_PreservesDailyFacts() {
        var user = User.Create("cycle-episode-delete@example.com", "hash");
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, start);
        profile.UpsertBleedingEntry(start, FoodDiary.Domain.Enums.BleedingType.Bleeding, FoodDiary.Domain.Enums.CycleFlowLevel.Light, 1, "kept");
        MenstrualEpisode episode = profile.ConfirmPeriodStart(start);
        var repository = new InMemoryCycleRepository(profile);
        var handler = new DeleteMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new DeleteMenstrualEpisodeCommand(user.Id.Value, profile.Id.Value, episode.Id.Value),
            CancellationToken.None);

        CycleModel model = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Single(model.BleedingEntries),
            () => Assert.Equal(FoodDiary.Domain.Enums.MenstrualEpisodeStatus.Inferred, Assert.Single(model.MenstrualEpisodes!).Status),
            () => Assert.True(repository.WasUpdated));
    }
}
