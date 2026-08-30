using FluentValidation.TestHelper;
using FoodDiary.Application.Cycles.Commands.DeleteMenstrualEpisode;
using FoodDiary.Application.Cycles.Commands.UpdateMenstrualEpisode;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EpisodeCommandHandlers_WithEmptyEpisodeId_ReturnValidationFailure(bool update) {
        var user = User.Create($"episode-empty-{update}@example.com", "hash");
        Result<CycleModel> result = update
            ? await new UpdateMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user))
                .Handle(new UpdateMenstrualEpisodeCommand(UserId: user.Id.Value, CycleProfileId: Guid.NewGuid(), MenstrualEpisodeId: Guid.Empty, StartDate: new DateOnly(2026, 4, 1), EndDate: null), CancellationToken.None)
            : await new DeleteMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user))
                .Handle(new DeleteMenstrualEpisodeCommand(user.Id.Value, Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EpisodeCommandHandlers_WhenProfileIsMissing_ReturnNotFound(bool update) {
        var user = User.Create($"episode-profile-{update}@example.com", "hash");
        Result<CycleModel> result = update
            ? await new UpdateMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user))
                .Handle(new UpdateMenstrualEpisodeCommand(UserId: user.Id.Value, CycleProfileId: Guid.NewGuid(), MenstrualEpisodeId: Guid.NewGuid(), StartDate: new DateOnly(2026, 4, 1), EndDate: null), CancellationToken.None)
            : await new DeleteMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user))
                .Handle(new DeleteMenstrualEpisodeCommand(user.Id.Value, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EpisodeCommandHandlers_WithEmptyProfileId_ReturnValidationFailure(bool update) {
        var user = User.Create($"episode-empty-profile-{update}@example.com", "hash");
        Result<CycleModel> result = update
            ? await new UpdateMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user))
                .Handle(new UpdateMenstrualEpisodeCommand(UserId: user.Id.Value, CycleProfileId: Guid.Empty, MenstrualEpisodeId: Guid.NewGuid(), StartDate: new DateOnly(2026, 4, 1), EndDate: null), CancellationToken.None)
            : await new DeleteMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user))
                .Handle(new DeleteMenstrualEpisodeCommand(user.Id.Value, Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EpisodeCommandHandlers_WithInvalidUser_ReturnAuthenticationFailure(bool update) {
        Result<CycleModel> result = update
            ? await new UpdateMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user: null))
                .Handle(new UpdateMenstrualEpisodeCommand(UserId: Guid.NewGuid(), CycleProfileId: Guid.NewGuid(), MenstrualEpisodeId: Guid.NewGuid(), StartDate: new DateOnly(2026, 4, 1), EndDate: null), CancellationToken.None)
            : await new DeleteMenstrualEpisodeCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user: null))
                .Handle(new DeleteMenstrualEpisodeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateMenstrualEpisodeCommandValidator_WithInvalidValues_HasErrors() {
        TestValidationResult<UpdateMenstrualEpisodeCommand> result = await new UpdateMenstrualEpisodeCommandValidator()
            .TestValidateAsync(new UpdateMenstrualEpisodeCommand(UserId: null, CycleProfileId: Guid.Empty, MenstrualEpisodeId: Guid.Empty, StartDate: default, EndDate: default));

        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public async Task DeleteMenstrualEpisodeCommandValidator_WithInvalidValues_HasErrors() {
        TestValidationResult<DeleteMenstrualEpisodeCommand> result = await new DeleteMenstrualEpisodeCommandValidator()
            .TestValidateAsync(new DeleteMenstrualEpisodeCommand(UserId: null, CycleProfileId: Guid.Empty, MenstrualEpisodeId: Guid.Empty));

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task UpdateMenstrualEpisodeCommandHandler_WhenEndPrecedesStart_ReturnsValidationFailure() {
        var user = User.Create("episode-invalid-range@example.com", "hash");
        DateOnly start = new(2026, 4, 10);
        var profile = FoodDiary.Domain.Entities.Tracking.CycleProfile.Create(user.Id, start);
        FoodDiary.Domain.Entities.Tracking.MenstrualEpisode episode = profile.ConfirmPeriodStart(start);
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateMenstrualEpisodeCommand(
                UserId: user.Id.Value,
                CycleProfileId: profile.Id.Value,
                MenstrualEpisodeId: episode.Id.Value,
                StartDate: start,
                EndDate: start.AddDays(-1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task UpdateMenstrualEpisodeCommandHandler_WhenRangesOverlap_ReturnsValidationFailure() {
        var user = User.Create("episode-overlap@example.com", "hash");
        DateOnly start = new(2026, 4, 1);
        var profile = FoodDiary.Domain.Entities.Tracking.CycleProfile.Create(user.Id, start);
        FoodDiary.Domain.Entities.Tracking.MenstrualEpisode first = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(first.Id, start, start.AddDays(4));
        FoodDiary.Domain.Entities.Tracking.MenstrualEpisode second = profile.ConfirmPeriodStart(start.AddDays(20));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateMenstrualEpisodeCommand(
                UserId: user.Id.Value,
                CycleProfileId: profile.Id.Value,
                MenstrualEpisodeId: second.Id.Value,
                StartDate: start.AddDays(3),
                EndDate: start.AddDays(8)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task DeleteMenstrualEpisodeCommandHandler_WithInferredEpisode_ReturnsValidationFailure() {
        var user = User.Create("episode-delete-inferred@example.com", "hash");
        DateOnly date = new(2026, 4, 1);
        var profile = FoodDiary.Domain.Entities.Tracking.CycleProfile.Create(user.Id, date);
        profile.UpsertBleedingEntry(
            date,
            FoodDiary.Domain.Enums.BleedingType.Bleeding,
            FoodDiary.Domain.Enums.CycleFlowLevel.Light,
            painImpact: null,
            notes: null);
        FoodDiary.Domain.Entities.Tracking.MenstrualEpisode episode = Assert.Single(profile.MenstrualEpisodes);
        var repository = new InMemoryCycleRepository(profile);
        var handler = new DeleteMenstrualEpisodeCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new DeleteMenstrualEpisodeCommand(user.Id.Value, profile.Id.Value, episode.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }
}
