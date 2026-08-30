using FoodDiary.Application.Cycles.Commands.UpdateCycleSettings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {
    [Fact]
    public async Task UpdateCycleSettingsCommandValidator_WithInvalidLengths_Fails() {
        var command = new UpdateCycleSettingsCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 17,
            AveragePeriodLength: 15,
            LutealLength: 7,
            IsRegular: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true);

        FluentValidation.Results.ValidationResult result = await new UpdateCycleSettingsCommandValidator().ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public async Task UpdateCycleSettingsCommandHandler_WithValidCommand_UpdatesOwnedProfile() {
        var user = User.Create("cycle-settings@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpdateCycleSettingsCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpdateCycleSettingsCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleTrackingMode.Perimenopause,
                AverageCycleLength: 31,
                AveragePeriodLength: 6,
                LutealLength: 12,
                IsRegular: true,
                ShowFertilityEstimates: true,
                DiscreetNotifications: false),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.WasUpdated);
        Assert.Equal(CycleTrackingMode.Perimenopause, result.Value.Mode);
        Assert.Equal(31, result.Value.AverageCycleLength);
        Assert.Equal(6, result.Value.AveragePeriodLength);
        Assert.Equal(12, result.Value.LutealLength);
        Assert.True(result.Value.IsRegular);
        Assert.True(result.Value.ShowFertilityEstimates);
        Assert.False(result.Value.DiscreetNotifications);
    }

    [Fact]
    public async Task UpdateCycleSettingsCommandHandler_WhenProfileIsNotOwned_ReturnsNotFound() {
        var owner = User.Create("cycle-owner@example.com", "hash");
        var requester = User.Create("cycle-requester@example.com", "hash");
        var profile = CycleProfile.Create(owner.Id, DateOnly.FromDateTime(DateTime.UtcNow));
        var handler = new UpdateCycleSettingsCommandHandler(
            new InMemoryCycleRepository(profile),
            CreateCurrentUserAccessService(requester));

        Result<CycleModel> result = await handler.Handle(
            new UpdateCycleSettingsCommand(
                requester.Id.Value,
                profile.Id.Value,
                (int)CycleTrackingMode.PeriodTracking,
                AverageCycleLength: 28,
                AveragePeriodLength: 5,
                LutealLength: 14,
                IsRegular: false,
                ShowFertilityEstimates: false,
                DiscreetNotifications: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateCycleSettingsCommandHandler_WithInvalidIdentity_ReturnsFailure(bool emptyProfileId) {
        var user = User.Create($"cycle-settings-invalid-{emptyProfileId}@example.com", "hash");
        var handler = new UpdateCycleSettingsCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(emptyProfileId ? user : null));
        var command = new UpdateCycleSettingsCommand(
            emptyProfileId ? user.Id.Value : Guid.NewGuid(),
            emptyProfileId ? Guid.Empty : Guid.NewGuid(),
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: false);

        Result<CycleModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(emptyProfileId ? "Validation.Invalid" : "Authentication.InvalidToken", result.Error.Code);
    }
}
