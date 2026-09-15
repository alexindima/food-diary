using FoodDiary.Modules.Dietologist.Domain.ValueObjects;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistPermissionValueTests {
    [Fact]
    public void DietologistPermissions_AllEnabled_AllFieldsTrue() {
        DietologistPermissions perms = DietologistPermissions.AllEnabled;

        Assert.Multiple(
            () => Assert.True(perms.ShareMeals),
            () => Assert.True(perms.ShareStatistics),
            () => Assert.True(perms.ShareWeight),
            () => Assert.True(perms.ShareWaist),
            () => Assert.True(perms.ShareGoals),
            () => Assert.True(perms.ShareHydration),
            () => Assert.True(perms.ShareProfile),
            () => Assert.True(perms.ShareFasting));
    }

    [Fact]
    public void DietologistPermissions_WithSelectiveDisable_PreservesOthers() {
        var perms = new DietologistPermissions(ShareMeals: false, ShareWeight: false);

        Assert.Multiple(
            () => Assert.False(perms.ShareMeals),
            () => Assert.True(perms.ShareStatistics),
            () => Assert.False(perms.ShareWeight),
            () => Assert.True(perms.ShareWaist),
            () => Assert.True(perms.ShareGoals),
            () => Assert.True(perms.ShareHydration),
            () => Assert.True(perms.ShareProfile),
            () => Assert.True(perms.ShareFasting));
    }
}
