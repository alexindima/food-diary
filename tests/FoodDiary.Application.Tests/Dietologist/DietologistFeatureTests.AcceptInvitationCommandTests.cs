using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

    [Fact]
    public async Task AcceptInvitation_WithNullUserId_ReturnsFailure() {
        AcceptInvitationCommandHandler handler = CreateAcceptHandler();

        Result result = await handler.Handle(
            new AcceptInvitationCommand(Guid.NewGuid(), "token", UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task AcceptInvitation_WhenNotFound_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(Guid.NewGuid(), "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task AcceptInvitation_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var dietologistId = UserId.New();
        AcceptInvitationCommandHandler handler = CreateAcceptHandler(
            userRepository: CreateAccessCheckedFailingDietologistUserContext(dietologistId));

        Result result = await handler.Handle(
            new AcceptInvitationCommand(Guid.NewGuid(), "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitation_WithEmptyInvitationId_ReturnsValidationFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        AcceptInvitationCommandHandler handler = CreateAcceptHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(Guid.Empty, "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("InvitationId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task AcceptInvitation_WithInvalidToken_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId, tokenHash: "correct-hash");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            passwordHasher: new StubPasswordHasher(verifyResult: false));

        Result result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "wrong-token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task AcceptInvitation_WhenCurrentUserEmailDoesNotMatchInvitation_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId, "other@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(
            invitationRepository: invRepo,
            userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(DietologistInvitationStatus.Pending, invitation.Status);
    }


    [Fact]
    public async Task AcceptInvitation_WithExpiredInvitation_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        var invitation = DietologistInvitation.Create(
            clientId, "diet@example.com", "hash",
            DateTime.UtcNow.AddDays(-1), AllDomainPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task AcceptInvitation_WithValidData_Succeeds() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        userRepo.SeedRoles(new[] { RoleNames.Dietologist });

        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();
        var userRoleMembershipService = new RecordingUserRoleMembershipService();

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            userRoleMembershipService: userRoleMembershipService,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DietologistInvitationStatus.Accepted, invitation.Status);
        Notification notification = Assert.Single(notificationRepo.Added);
        Assert.Equal(NotificationTypes.DietologistInvitationAccepted, notification.Type);
        Assert.Equal(clientId, notification.UserId);
        Assert.Equal([dietologistId], userRoleMembershipService.EnsureRoleUserIds);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }


    [Fact]
    public async Task AcceptInvitation_WhenDietologistDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
        Assert.Equal(DietologistInvitationStatus.Pending, invitation.Status);
    }


    [Fact]
    public async Task AcceptInvitation_WhenInvitationAlreadyHandled_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        invitation.Decline();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationCommandHandler handler = CreateAcceptHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WithMatchingEmail_Succeeds() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        userRepo.SeedRoles(new[] { RoleNames.Dietologist });

        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();
        var userRoleMembershipService = new RecordingUserRoleMembershipService();

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            userRoleMembershipService: userRoleMembershipService,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DietologistInvitationStatus.Accepted, invitation.Status);
        Assert.Contains(notificationRepo.Added, x => string.Equals(x.Type, NotificationTypes.DietologistInvitationAccepted, StringComparison.Ordinal) && x.UserId == clientId);
        Assert.Equal([dietologistId], userRoleMembershipService.EnsureRoleUserIds);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WithNullUserId_ReturnsFailure() {
        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler();

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenUserDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var dietologistId = UserId.New();
        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(
            userRepository: CreateAccessCheckedFailingDietologistUserContext(dietologistId));

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WithEmptyInvitationId_ReturnsValidationFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("InvitationId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenInvitationMissingAfterUserAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new SequenceUserRepository(user, null);

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenInvitationMissing_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenInvitationAlreadyHandled_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        invitation.Decline();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenEmailDoesNotMatch_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "other@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }


    [Fact]
    public async Task AcceptInvitationForCurrentUser_WhenInvitationExpired_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        var invitation = DietologistInvitation.Create(
            UserId.New(), "diet@example.com", "hash", DateTime.UtcNow.AddDays(-1), AllDomainPermissions);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, CreateUser(invitation.ClientUserId, "client@example.com"));
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        AcceptInvitationForCurrentUserCommandHandler handler = CreateAcceptCurrentUserHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationExpired", result.Error.Code);
    }

}
