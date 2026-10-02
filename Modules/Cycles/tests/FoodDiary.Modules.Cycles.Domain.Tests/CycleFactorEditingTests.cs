using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public class CycleFactorEditingTests {
    [Fact]
    public void UpdateFactor_ChangingIdentity_PreservesIdAndCount() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        CycleFactor factor = profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: "note");
        CycleFactor updated = profile.UpdateFactor(factor.Id, CycleFactorType.NonHormonalContraception, new DateOnly(2026, 4, 3), endDate: null, notes: null);
        Assert.Same(factor, updated);
        Assert.Single(profile.Factors);
        Assert.Equal(new DateOnly(2026, 4, 3), factor.StartDate);
        Assert.Equal(CycleFactorType.NonHormonalContraception, factor.Type);
        Assert.Equal("note", factor.Notes);
    }

    [Fact]
    public void UpdateFactor_WithCollision_PreservesBothFactors() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        CycleFactor first = profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: "first");
        profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 3), endDate: null, notes: "second");
        Assert.Throws<InvalidOperationException>(() => profile.UpdateFactor(first.Id, first.Type, new DateOnly(2026, 4, 3), endDate: null, notes: "changed"));
        Assert.Equal(2, profile.Factors.Count);
        Assert.Equal(new DateOnly(2026, 4, 2), first.StartDate);
        Assert.Equal("first", first.Notes);
    }

    [Fact]
    public void UpdateFactor_WithInvalidRange_DoesNotPartiallyChangeIdentity() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        CycleFactor factor = profile.UpsertFactor(CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: "note");
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.UpdateFactor(factor.Id, CycleFactorType.NonHormonalContraception, new DateOnly(2026, 4, 4), new DateOnly(2026, 4, 3), "changed"));
        Assert.Equal(new DateOnly(2026, 4, 2), factor.StartDate);
        Assert.Equal(CycleFactorType.HormonalContraception, factor.Type);
        Assert.Equal("note", factor.Notes);
    }

    [Fact]
    public void UpdateFactor_WithUnknownId_DoesNotCreateFactor() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        Assert.Throws<KeyNotFoundException>(() => profile.UpdateFactor(CycleFactorId.New(), CycleFactorType.HormonalContraception, new DateOnly(2026, 4, 2), endDate: null, notes: null));
        Assert.Empty(profile.Factors);
    }
}
