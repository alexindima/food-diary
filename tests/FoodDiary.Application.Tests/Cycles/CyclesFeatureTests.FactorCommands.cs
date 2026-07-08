using FoodDiary.Results;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithInvalidType_ReturnsValidationFailure() {
        var user = User.Create("cycle-factor-invalid@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpsertCycleFactorCommandHandler(new InMemoryCycleRepository(profile), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                Type: 999,
                StartDate: DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpsertCycleFactorCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-factor-empty-user@example.com", "hash")));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                Guid.Empty,
                Guid.NewGuid(),
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithEmptyProfileId_ReturnsValidationFailure() {
        var user = User.Create("cycle-factor-empty-profile@example.com", "hash");
        var handler = new UpsertCycleFactorCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                Guid.Empty,
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-factor-deleted-user@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WhenProfileMissing_ReturnsNotFound() {
        var user = User.Create("cycle-factor-missing-profile@example.com", "hash");
        var handler = new UpsertCycleFactorCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                Guid.NewGuid(),
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithValidCommand_UpdatesProfileAndReturnsCycle() {
        var user = User.Create("cycle-factor-success@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleFactorType.HormonalContraception,
                new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                EndDate: null,
                Notes: "pill",
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value.Factors);
        Assert.Equal(CycleFactorType.HormonalContraception, result.Value.Factors.Single().Type);
        Assert.True(repository.WasUpdated);
    }
}
