using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserRoleCreationTests {
    [Fact]
    public void MiscDomainMethods_CoverRemainingBranches() {
        var userRole = new UserRole(UserId.New(), RoleId.New());
        Assert.Multiple(
            () => Assert.NotEqual(UserId.Empty, userRole.UserId));
    }
}
