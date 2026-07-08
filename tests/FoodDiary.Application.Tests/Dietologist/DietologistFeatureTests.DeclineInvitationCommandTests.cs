using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

    [Fact]
    public async Task DeclineInvitation_WhenNotFound_ReturnsFailure() {
        DeclineInvitationCommandHandler handler = CreateDeclineHandler();

        Result result = await handler.Handle(
            new DeclineInvitationCommand(Guid.NewGuid(), "token", UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task DeclineInvitation_WithEmptyInvitationId_ReturnsValidationFailure() {
        DeclineInvitationCommandHandler handler = CreateDeclineHandler();

        Result result = await handler.Handle(
            new DeclineInvitationCommand(Guid.Empty, "token", UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("InvitationId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DeclineInvitation_WithInvalidToken_ReturnsFailure() {
        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        DeclineInvitationCommandHandler handler = CreateDeclineHandler(
            invitationRepository: invRepo,
            passwordHasher: new StubPasswordHasher(verifyResult: false));

        Result result = await handler.Handle(
            new DeclineInvitationCommand(invitation.Id.Value, "wrong", UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task DeclineInvitation_WithValidToken_Succeeds() {
        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();

        DeclineInvitationCommandHandler handler = CreateDeclineHandler(
            invitationRepository: invRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        Result result = await handler.Handle(
            new DeclineInvitationCommand(invitation.Id.Value, "token", UserId: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DietologistInvitationStatus.Declined, invitation.Status);
        Notification notification = Assert.Single(notificationRepo.Added);
        Assert.Equal(NotificationTypes.DietologistInvitationDeclined, notification.Type);
        Assert.Equal(clientId, notification.UserId);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WithMatchingEmail_Succeeds() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();

        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DietologistInvitationStatus.Declined, invitation.Status);
        Assert.Contains(notificationRepo.Added, x => string.Equals(x.Type, NotificationTypes.DietologistInvitationDeclined, StringComparison.Ordinal) && x.UserId == clientId);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WithNullUserId_ReturnsFailure() {
        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler();

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenUserDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var dietologistId = UserId.New();
        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(
            userRepository: CreateAccessCheckedFailingDietologistUserContext(dietologistId));

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WithEmptyInvitationId_ReturnsValidationFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("InvitationId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenInvitationMissingAfterUserAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(
            userRepository: new SequenceUserRepository(CreateUser(dietologistId, "diet@example.com"), null));

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenInvitationMissing_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenInvitationAlreadyHandled_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        invitation.Decline();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenEmailDoesNotMatch_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "other@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }


    [Fact]
    public async Task DeclineInvitationForCurrentUser_WhenInvitationExpired_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        var invitation = DietologistInvitation.Create(
            UserId.New(), "diet@example.com", "hash", DateTime.UtcNow.AddDays(-1), AllDomainPermissions);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, CreateUser(invitation.ClientUserId, "client@example.com"));
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        DeclineInvitationForCurrentUserCommandHandler handler = CreateDeclineCurrentUserHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationExpired", result.Error.Code);
    }

}
