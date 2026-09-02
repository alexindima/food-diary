using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserRoleAuditInvariantTests {
    [Fact]
    public void UserRoleAuditEvent_Create_WithValidValues_NormalizesFieldsAndTimestamp() {
        var userId = UserId.New();
        var actorUserId = UserId.New();
        var role = Role.Create("  Premium  ");
        var occurredAtLocal = new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Local);

        var auditEvent = UserRoleAuditEvent.Create(
            userId,
            role,
            UserRoleAuditAction.Added,
            actorUserId,
            source: "  Admin  ",
            occurredAtLocal);

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, auditEvent.Id),
            () => Assert.Equal(userId, auditEvent.UserId),
            () => Assert.Equal(role.Id, auditEvent.RoleId),
            () => Assert.Equal("Premium", auditEvent.RoleName),
            () => Assert.Equal(UserRoleAuditAction.Added, auditEvent.Action),
            () => Assert.Equal(actorUserId, auditEvent.ActorUserId),
            () => Assert.Equal("Admin", auditEvent.Source),
            () => Assert.Equal(occurredAtLocal.ToUniversalTime(), auditEvent.OccurredAtUtc),
            () => Assert.Equal(occurredAtLocal.ToUniversalTime(), auditEvent.CreatedOnUtc));
    }

    [Fact]
    public void UserRoleAuditEvent_Create_WithNullActor_StoresNullActor() {
        var auditEvent = UserRoleAuditEvent.Create(
            UserId.New(),
            Role.Create("Premium"),
            UserRoleAuditAction.Removed,
            actorUserId: null,
            source: "Billing",
            DateTime.UtcNow);

        Assert.Null(auditEvent.ActorUserId);
    }

    [Fact]
    public void UserRoleAuditEvent_Create_WithLongValues_TruncatesTextFields() {
        var auditEvent = UserRoleAuditEvent.Create(
            UserId.New(),
            Role.Create(new string('r', 64)),
            UserRoleAuditAction.Added,
            actorUserId: null,
            source: new string('s', 65),
            DateTime.UtcNow);

        Assert.Equal(64, auditEvent.RoleName.Length);
        Assert.Equal(64, auditEvent.Source.Length);
    }

    [Fact]
    public void UserRoleAuditEvent_Create_WithEmptyRoleId_Throws() {
        var role = (Role)Activator.CreateInstance(typeof(Role), nonPublic: true)!;

        Assert.Throws<ArgumentException>(() =>
            UserRoleAuditEvent.Create(UserId.New(), role, UserRoleAuditAction.Added, actorUserId: null, "Admin", DateTime.UtcNow));
    }

    [Fact]
    public void UserRoleAuditEvent_Create_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentNullException>(() =>
            UserRoleAuditEvent.Create(UserId.New(), null!, UserRoleAuditAction.Added, actorUserId: null, "Admin", DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            UserRoleAuditEvent.Create(UserId.Empty, Role.Create("Premium"), UserRoleAuditAction.Added, actorUserId: null, "Admin", DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            UserRoleAuditEvent.Create(UserId.New(), Role.Create("Premium"), UserRoleAuditAction.Added, UserId.Empty, "Admin", DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            UserRoleAuditEvent.Create(UserId.New(), Role.Create("Premium"), UserRoleAuditAction.Added, actorUserId: null, " ", DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserRoleAuditEvent.Create(UserId.New(), Role.Create("Premium"), (UserRoleAuditAction)999, actorUserId: null, "Admin", DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserRoleAuditEvent.Create(UserId.New(), Role.Create("Premium"), UserRoleAuditAction.Added, actorUserId: null, "Admin", new DateTime(2026, 5, 1)));
    }
}
