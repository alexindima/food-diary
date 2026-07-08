using FoodDiary.Results;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {

    [Fact]
    public async Task CreateCycleCommandValidator_WithInvalidLength_Fails() {
        var validator = new CreateCycleCommandValidator();
        var command = new CreateCycleCommand(
            Guid.NewGuid(),
            DateTime.UtcNow,
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 10,
            AveragePeriodLength: 20,
            LutealLength: 20,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-cycle@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var handler = new CreateCycleCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(CreateCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new CreateCycleCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-create-empty-user@example.com", "hash")));

        Result<CycleModel> result = await handler.Handle(CreateCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WhenCurrentProfileExists_UpdatesExistingProfile() {
        var user = User.Create("cycle-create-existing@example.com", "hash");
        var profile = CycleProfile.Create(
            user.Id,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            notes: "old");
        var repository = new InMemoryCycleRepository(profile);
        var handler = new CreateCycleCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new CreateCycleCommand(
                user.Id.Value,
                new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                (int)CycleTrackingMode.TryingToConceive,
                AverageCycleLength: 30,
                AveragePeriodLength: 4,
                LutealLength: 13,
                IsRegular: true,
                IsOnboardingComplete: true,
                ShowFertilityEstimates: true,
                DiscreetNotifications: false,
                Notes: " updated "),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(profile.Id.Value, result.Value.Id);
        Assert.Equal(CycleTrackingMode.TryingToConceive, result.Value.Mode);
        Assert.Equal(30, result.Value.AverageCycleLength);
        Assert.Equal("updated", result.Value.Notes);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-current-deleted@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        GetCurrentCycleQueryHandler handler = CreateCurrentCycleHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user));

        Result<CycleModel?> result = await handler.Handle(new GetCurrentCycleQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        GetCurrentCycleQueryHandler handler = CreateCurrentCycleHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-current-empty@example.com", "hash")));

        Result<CycleModel?> result = await handler.Handle(new GetCurrentCycleQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }
}
