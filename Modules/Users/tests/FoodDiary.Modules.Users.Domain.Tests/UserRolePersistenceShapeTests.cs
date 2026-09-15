using FoodDiary.Modules.Users.Domain.Entities;
using System.Reflection;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserRolePersistenceShapeTests {
    private static T CreatePrivate<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        ReadPublicProperties(CreatePrivate<UserRole>());
    }
}
