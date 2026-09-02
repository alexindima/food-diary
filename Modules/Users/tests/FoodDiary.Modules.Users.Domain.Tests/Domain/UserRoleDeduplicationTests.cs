using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserRoleDeduplicationTests {
    [Fact]
    public void ReplaceRoles_DeduplicatesRolesByIdentifier() {
        var user = User.Create("roles@example.com", "hash");
        var admin = Role.Create("Admin");

        user.ReplaceRoles([admin, admin]);

        UserRole assignedRole = Assert.Single(user.UserRoles);
        Assert.Equal(admin.Id, assignedRole.RoleId);
    }
}
