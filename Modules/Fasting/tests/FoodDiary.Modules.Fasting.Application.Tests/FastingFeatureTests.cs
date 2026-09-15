using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Modules.Fasting.Application.Tests;

[ExcludeFromCodeCoverage]
public partial class FastingFeatureTests {
    private static readonly DateTime FixedNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    private static ICurrentUserAccessService CreateCurrentUserAccessService(UserId userId) =>
        new StubCurrentUserAccessService(CreateUser(userId));

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user) =>
        new StubCurrentUserAccessService(user);

    private static User CreateUser(UserId userId) {
        var user = User.Create($"fasting-{userId.Value:N}@example.com", "hash");
        SetPrivateProperty(user, nameof(User.Id), userId);
        return user;
    }

    private static IUnitOfWork CreateUnitOfWork() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return unitOfWork;
    }

    private static void AttachNavigation(FastingOccurrence occurrence, FastingPlan plan, User user) {
        SetPrivateProperty(occurrence, nameof(FastingOccurrence.Plan), plan);
    }

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        PropertyInfo? property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }
}
