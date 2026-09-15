using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FluentValidation.TestHelper;
using FoodDiary.Modules.Cycles.Application.Commands.ConfirmPeriodStart;
using FoodDiary.Modules.Cycles.Application.Commands.UpdateCycleConsent;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Tests;

public partial class CyclesFeatureTests {
    [Fact]
    public async Task ConfirmPeriodStartCommandHandler_WithOverlappingEpisode_ReturnsValidationFailureWithoutMutation() {
        var user = User.Create("confirm-overlap@example.com", "hash");
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, start);
        MenstrualEpisode episode = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(episode.Id, start, start.AddDays(5));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new ConfirmPeriodStartCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new ConfirmPeriodStartCommand(user.Id.Value, profile.Id.Value, start.AddDays(3)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        MenstrualEpisode unchanged = Assert.Single(profile.MenstrualEpisodes);
        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", result.Error.Code),
            () => Assert.Equal(episode.Id, unchanged.Id),
            () => Assert.Equal(start, unchanged.StartDate),
            () => Assert.Equal(start.AddDays(5), unchanged.EndDate),
            () => Assert.Empty(profile.PredictionRevisions),
            () => Assert.False(repository.WasUpdated));
    }

    [Fact]
    public async Task ConfirmPeriodStartCommandHandler_WithOwnedProfile_ConfirmsEpisode() {
        var user = User.Create("confirm-period@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new ConfirmPeriodStartCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new ConfirmPeriodStartCommand(user.Id.Value, profile.Id.Value, new DateOnly(2026, 4, 3)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.WasUpdated);
        Assert.Contains(result.Value.MenstrualEpisodes!, episode => episode.StartDate == new DateOnly(2026, 4, 3));
    }

    [Fact]
    public async Task ConfirmPeriodStartCommandHandler_WithEmptyProfileId_ReturnsValidationFailure() {
        var user = User.Create("confirm-invalid@example.com", "hash");
        var handler = new ConfirmPeriodStartCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new ConfirmPeriodStartCommand(user.Id.Value, Guid.Empty, new DateOnly(2026, 4, 3)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPeriodStartCommandHandler_WithInvalidUser_ReturnsAuthenticationFailure() {
        var handler = new ConfirmPeriodStartCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user: null));

        Result<CycleModel> result = await handler.Handle(
            new ConfirmPeriodStartCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 3)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPeriodStartCommandHandler_WhenProfileIsMissing_ReturnsNotFound() {
        var user = User.Create("confirm-missing@example.com", "hash");
        var handler = new ConfirmPeriodStartCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new ConfirmPeriodStartCommand(user.Id.Value, Guid.NewGuid(), new DateOnly(2026, 4, 3)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateCycleConsentCommandHandler_UpdatesOwnedProfile(bool granted) {
        var user = User.Create($"consent-{granted}@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        profile.GrantConsent(CycleConsentPurpose.CycleTracking, DateTime.UtcNow.AddDays(-1));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateCycleConsentCommandHandler(
            repository,
            CreateCurrentUserAccessService(user),
            TimeProvider.System);

        Result<CycleModel> result = await handler.Handle(
            new UpdateCycleConsentCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleConsentPurpose.CycleTracking,
                granted),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.WasUpdated);
        Assert.Equal(granted, result.Value.Consents!.Any(consent =>
            consent.Purpose == CycleConsentPurpose.CycleTracking && consent.RevokedAtUtc is null));
    }

    [Fact]
    public async Task UpdateCycleConsentCommandHandler_WithEmptyProfileId_ReturnsValidationFailure() {
        var user = User.Create("consent-invalid@example.com", "hash");
        var handler = new UpdateCycleConsentCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user),
            TimeProvider.System);

        Result<CycleModel> result = await handler.Handle(
            new UpdateCycleConsentCommand(
                UserId: user.Id.Value,
                CycleProfileId: Guid.Empty,
                Purpose: (int)CycleConsentPurpose.CycleTracking,
                Granted: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UpdateCycleConsentCommandHandler_WithInvalidUser_ReturnsAuthenticationFailure() {
        var handler = new UpdateCycleConsentCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user: null),
            TimeProvider.System);

        Result<CycleModel> result = await handler.Handle(
            new UpdateCycleConsentCommand(
                UserId: Guid.NewGuid(),
                CycleProfileId: Guid.NewGuid(),
                Purpose: (int)CycleConsentPurpose.CycleTracking,
                Granted: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCycleConsentCommandHandler_WhenProfileIsMissing_ReturnsNotFound() {
        var user = User.Create("consent-missing@example.com", "hash");
        var handler = new UpdateCycleConsentCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user),
            TimeProvider.System);

        Result<CycleModel> result = await handler.Handle(
            new UpdateCycleConsentCommand(
                UserId: user.Id.Value,
                CycleProfileId: Guid.NewGuid(),
                Purpose: (int)CycleConsentPurpose.CycleTracking,
                Granted: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPeriodStartCommandValidator_WithInvalidValues_HasErrors() {
        TestValidationResult<ConfirmPeriodStartCommand> result = await new ConfirmPeriodStartCommandValidator()
            .TestValidateAsync(new ConfirmPeriodStartCommand(UserId: null, Guid.Empty, default));

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task UpdateCycleConsentCommandValidator_WithInvalidValues_HasErrors() {
        TestValidationResult<UpdateCycleConsentCommand> result = await new UpdateCycleConsentCommandValidator()
            .TestValidateAsync(new UpdateCycleConsentCommand(UserId: null, Guid.Empty, Purpose: 999, Granted: true));

        Assert.Equal(4, result.Errors.Count);
    }
}
