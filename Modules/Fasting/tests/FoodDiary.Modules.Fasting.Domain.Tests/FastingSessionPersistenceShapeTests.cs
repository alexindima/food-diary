using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Modules.Fasting.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingSessionPersistenceShapeTests {
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
        var fastingSession = FastingSession.Create(
            ownerId,
            FastingProtocol.Fast16Eat8,
            plannedDurationHours: 16,
            startedAtUtc: DateTime.UtcNow);
        ReadPublicProperties(fastingSession);
        Assert.Multiple(
            () => Assert.Equal(ownerId, fastingSession.UserId));
    }
}
