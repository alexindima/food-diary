using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class UserAuditEventInvariantTests {
    [Fact]
    public void UserLoginEvent_Create_WithValidValues_NormalizesFieldsAndTimestamp() {
        var userId = UserId.New();
        var loggedInAtLocal = new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Local);

        var loginEvent = UserLoginEvent.Create(
            userId,
            authProvider: "  Password  ",
            ipAddress: "  127.0.0.1  ",
            userAgent: "  Browser  ",
            browserName: "  Chrome  ",
            browserVersion: "  123  ",
            operatingSystem: "  Windows  ",
            deviceType: "  Desktop  ",
            loggedInAtLocal);

        Assert.NotEqual(Guid.Empty, loginEvent.Id);
        Assert.Equal(userId, loginEvent.UserId);
        Assert.Equal("Password", loginEvent.AuthProvider);
        Assert.Equal("127.0.0.1", loginEvent.IpAddress);
        Assert.Equal("Browser", loginEvent.UserAgent);
        Assert.Equal("Chrome", loginEvent.BrowserName);
        Assert.Equal("123", loginEvent.BrowserVersion);
        Assert.Equal("Windows", loginEvent.OperatingSystem);
        Assert.Equal("Desktop", loginEvent.DeviceType);
        Assert.Equal(loggedInAtLocal.ToUniversalTime(), loginEvent.LoggedInAtUtc);
        Assert.Equal(loggedInAtLocal.ToUniversalTime(), loginEvent.CreatedOnUtc);
    }

    [Fact]
    public void UserLoginEvent_Create_WithBlankOptionalValues_StoresNulls() {
        var loginEvent = UserLoginEvent.Create(
            UserId.New(),
            "Password",
            ipAddress: " ",
            userAgent: null,
            browserName: " ",
            browserVersion: " ",
            operatingSystem: " ",
            deviceType: " ",
            DateTime.UtcNow);

        Assert.Null(loginEvent.IpAddress);
        Assert.Null(loginEvent.UserAgent);
        Assert.Null(loginEvent.BrowserName);
        Assert.Null(loginEvent.BrowserVersion);
        Assert.Null(loginEvent.OperatingSystem);
        Assert.Null(loginEvent.DeviceType);
    }

    [Fact]
    public void UserLoginEvent_Create_WithLongValues_TruncatesTextFields() {
        var loginEvent = UserLoginEvent.Create(
            UserId.New(),
            authProvider: new string('a', 65),
            ipAddress: new string('i', 129),
            userAgent: new string('u', 513),
            browserName: new string('b', 65),
            browserVersion: new string('v', 65),
            operatingSystem: new string('o', 65),
            deviceType: new string('d', 33),
            DateTime.UtcNow);

        Assert.Equal(64, loginEvent.AuthProvider.Length);
        Assert.Equal(128, loginEvent.IpAddress!.Length);
        Assert.Equal(512, loginEvent.UserAgent!.Length);
        Assert.Equal(64, loginEvent.BrowserName!.Length);
        Assert.Equal(64, loginEvent.BrowserVersion!.Length);
        Assert.Equal(64, loginEvent.OperatingSystem!.Length);
        Assert.Equal(32, loginEvent.DeviceType!.Length);
    }

    [Fact]
    public void UserLoginEvent_Create_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentException>(() =>
            UserLoginEvent.Create(UserId.Empty, "Password", ipAddress: null, userAgent: null, browserName: null, browserVersion: null, operatingSystem: null, deviceType: null, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            UserLoginEvent.Create(UserId.New(), " ", ipAddress: null, userAgent: null, browserName: null, browserVersion: null, operatingSystem: null, deviceType: null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserLoginEvent.Create(UserId.New(), "Password", ipAddress: null, userAgent: null, browserName: null, browserVersion: null, operatingSystem: null, deviceType: null, new DateTime(2026, 5, 1)));
    }

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

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(userId, auditEvent.UserId);
        Assert.Equal(role.Id, auditEvent.RoleId);
        Assert.Equal("Premium", auditEvent.RoleName);
        Assert.Equal(UserRoleAuditAction.Added, auditEvent.Action);
        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Equal("Admin", auditEvent.Source);
        Assert.Equal(occurredAtLocal.ToUniversalTime(), auditEvent.OccurredAtUtc);
        Assert.Equal(occurredAtLocal.ToUniversalTime(), auditEvent.CreatedOnUtc);
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
            UserRoleAuditEvent.Create(UserId.New(), Role.Create("Premium"), UserRoleAuditAction.Added, actorUserId: null, "Admin", new DateTime(2026, 5, 1)));
    }
}
