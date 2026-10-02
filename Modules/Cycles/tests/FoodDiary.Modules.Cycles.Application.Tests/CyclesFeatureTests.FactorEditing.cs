using FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleFactor;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Tests;

public partial class CyclesFeatureTests {
    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithForeignProfile_DoesNotUpdateFactor() {
        var owner = User.Create("factor-owner@example.com", "hash");
        var caller = User.Create("factor-caller@example.com", "hash");
        var profile = CycleProfile.Create(owner.Id, new DateOnly(2026, 4, 1));
        CycleFactor factor = profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: "private");
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(caller));
        Result<CycleModel> result = await handler.Handle(new UpsertCycleFactorCommand(caller.Id.Value, profile.Id.Value,
            (int)factor.Type, new DateOnly(2026, 4, 3), EndDate: null, Notes: null, ClearNotes: true, FactorId: factor.Id.Value), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
        Assert.Equal("private", factor.Notes);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithFactorId_UpdatesSelectedFactor() {
        var user = User.Create("factor-edit@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        CycleFactor factor = profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: "note");
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));
        Result<CycleModel> result = await handler.Handle(new UpsertCycleFactorCommand(user.Id.Value, profile.Id.Value,
            (int)CycleFactorType.NonHormonalContraception, new DateOnly(2026, 4, 3), EndDate: null, Notes: null, ClearNotes: false, FactorId: factor.Id.Value), CancellationToken.None);
        ResultAssert.Success(result);
        Assert.Single(profile.Factors);
        Assert.Equal(new DateOnly(2026, 4, 3), factor.StartDate);
        Assert.Equal(CycleFactorType.NonHormonalContraception, factor.Type);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithCollision_ReturnsConflictWithoutSaving() {
        var user = User.Create("factor-conflict@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        CycleFactor factor = profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: "first");
        profile.UpsertFactor(factor.Type, new DateOnly(2026, 4, 3), endDate: null, notes: "second");
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));
        Result<CycleModel> result = await handler.Handle(new UpsertCycleFactorCommand(user.Id.Value, profile.Id.Value,
            (int)factor.Type, new DateOnly(2026, 4, 3), EndDate: null, Notes: "changed", ClearNotes: false, FactorId: factor.Id.Value), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Cycle.FactorIdentityConflict", result.Error.Code);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.False(repository.WasUpdated);
        Assert.Equal("first", factor.Notes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpsertCycleFactorCommandHandler_WithInvalidFactorId_DoesNotCreateRecord(bool empty) {
        var user = User.Create("factor-missing@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));
        Result<CycleModel> result = await handler.Handle(new UpsertCycleFactorCommand(user.Id.Value, profile.Id.Value,
            (int)CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), EndDate: null, Notes: null, ClearNotes: false, FactorId:
            empty ? Guid.Empty : Guid.NewGuid()), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(profile.Factors);
        Assert.False(repository.WasUpdated);
    }
}
