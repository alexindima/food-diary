using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationPersistenceShapeTests {
    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        var ownerId = UserId.New();
        var invitation = DietologistInvitation.Create(
            ownerId,
            dietologistEmail: "dietologist@example.com",
            tokenHash: "token",
            expiresAtUtc: DateTime.UtcNow.AddDays(1),
            new DietologistPermissions(
                ShareMeals: true,
                ShareStatistics: true,
                ShareWeight: true,
                ShareWaist: true,
                ShareGoals: true,
                ShareHydration: true,
                ShareProfile: true,
                ShareFasting: true));
        ReadPublicProperties(invitation);
        Assert.Multiple(
            () => Assert.Null(invitation.DietologistUserId));
    }
}
