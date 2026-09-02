using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class MiscDomainInvariantTests {
    [Fact]
    public void Role_Create_WithBlankName_Throws() {
        Assert.Throws<ArgumentException>(() => Role.Create("   "));
    }

    [Fact]
    public void Role_UserRoles_AreExposedAsReadOnly() {
        var role = Role.Create("admin");
        ICollection<UserRole> userRoles = Assert.IsAssignableFrom<ICollection<UserRole>>(role.UserRoles);

        Assert.True(userRoles.IsReadOnly);
    }

}
