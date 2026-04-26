using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;
using FoodDiary.Application.Dietologist.Commands.RevokeInvitation;
using FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;
using FoodDiary.Application.Dietologist.EventHandlers;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetClientDashboard;
using FoodDiary.Application.Dietologist.Queries.GetClientGoals;
using FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;
using FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Queries.GetMyClients;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;

namespace FoodDiary.Application.Tests.Dietologist;

public class DietologistFeatureTests {
    private static readonly DietologistPermissionsInput AllPermissions = new(true, true, true, true, true, true);
    private static readonly DietologistPermissions AllDomainPermissions = new(true, true, true, true, true, true);

    private static User CreateUser(UserId? id = null, string email = "user@example.com") {
        var user = User.Create(email, "hashed-password");
        if (id is not null) {
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        }

        return user;
    }

    private static DietologistInvitation CreatePendingInvitation(
        UserId clientId, string email = "diet@example.com", string tokenHash = "hash",
        DietologistPermissions? permissions = null) {
        var invitation = DietologistInvitation.Create(
            clientId, email, tokenHash,
            DateTime.UtcNow.AddDays(7),
            permissions ?? AllDomainPermissions);

        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, CreateUser(clientId, "client@example.com"));

        return invitation;
    }

    private static DietologistInvitation CreateAcceptedInvitation(
        UserId clientId, UserId dietologistId,
        DietologistPermissions? permissions = null) {
        var invitation = CreatePendingInvitation(clientId, permissions: permissions);
        invitation.Accept(dietologistId);
        return invitation;
    }

    private static InviteDietologistCommandHandler CreateInviteHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IUserRepository? userRepository = null,
        IPasswordHasher? passwordHasher = null,
        IDietologistEmailSender? emailSender = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IDateTimeProvider? dateTimeProvider = null) =>
        new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            userRepository ?? new InMemoryUserRepository(),
            passwordHasher ?? new StubPasswordHasher(),
            emailSender ?? new FakeEmailSender(),
            notificationRepository ?? new InMemoryNotificationRepository(),
            notificationPusher ?? new FakeNotificationPusher(),
            dateTimeProvider ?? new StubDateTimeProvider(),
            NullLogger<InviteDietologistCommandHandler>.Instance);

    private static AcceptInvitationCommandHandler CreateAcceptHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IUserRepository? userRepository = null,
        IPasswordHasher? passwordHasher = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) =>
        new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            userRepository ?? new InMemoryUserRepository(),
            passwordHasher ?? new StubPasswordHasher(),
            notificationRepository ?? new InMemoryNotificationRepository(),
            notificationPusher ?? new FakeNotificationPusher(),
            webPushNotificationSender ?? new FakeWebPushNotificationSender());

    private static AcceptInvitationForCurrentUserCommandHandler CreateAcceptCurrentUserHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IUserRepository? userRepository = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) =>
        new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            userRepository ?? new InMemoryUserRepository(),
            notificationRepository ?? new InMemoryNotificationRepository(),
            notificationPusher ?? new FakeNotificationPusher(),
            webPushNotificationSender ?? new FakeWebPushNotificationSender());

    private static DeclineInvitationCommandHandler CreateDeclineHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IPasswordHasher? passwordHasher = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) =>
        new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            passwordHasher ?? new StubPasswordHasher(),
            notificationRepository ?? new InMemoryNotificationRepository(),
            notificationPusher ?? new FakeNotificationPusher(),
            webPushNotificationSender ?? new FakeWebPushNotificationSender());

    private static DeclineInvitationForCurrentUserCommandHandler CreateDeclineCurrentUserHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IUserRepository? userRepository = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) =>
        new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            userRepository ?? new InMemoryUserRepository(),
            notificationRepository ?? new InMemoryNotificationRepository(),
            notificationPusher ?? new FakeNotificationPusher(),
            webPushNotificationSender ?? new FakeWebPushNotificationSender());

    // ── InviteDietologist ──

    [Fact]
    public async Task InviteDietologist_WithNullUserId_ReturnsFailure() {
        var handler = CreateInviteHandler();

        var result = await handler.Handle(
            new InviteDietologistCommand(null, "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task InviteDietologist_WhenUserNotFound_ReturnsFailure() {
        var handler = CreateInviteHandler();

        var result = await handler.Handle(
            new InviteDietologistCommand(Guid.NewGuid(), "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task InviteDietologist_WhenInvitingSelf_ReturnsFailure() {
        var userId = UserId.New();
        var user = CreateUser(userId, "user@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = CreateInviteHandler(userRepository: userRepo);

        var result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "user@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task InviteDietologist_WhenAlreadyHasActiveDietologist_ReturnsFailure() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        var activeInvitation = CreateAcceptedInvitation(userId, UserId.New());
        invRepo.Seed(activeInvitation);

        var handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo);

        var result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task InviteDietologist_WhenPendingExists_ReturnsFailure() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        var pending = CreatePendingInvitation(userId);
        invRepo.Seed(pending);

        var handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo);

        var result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task InviteDietologist_WithValidData_Succeeds() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        var emailSender = new FakeEmailSender();

        var handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo, emailSender: emailSender);

        var result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(invRepo.Added);
        Assert.True(emailSender.SentCount > 0);
    }

    [Fact]
    public async Task InviteDietologist_WhenRegisteredDietologistExists_CreatesNotificationAndPushesUpdate() {
        var userId = UserId.New();
        var client = CreateUser(userId, "client@example.com");
        var dietologist = CreateUser(UserId.New(), "diet@example.com");

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(client);
        userRepo.Seed(dietologist);

        var invitationRepo = new InMemoryInvitationRepository();
        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();

        var handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher);

        var result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(notificationRepo.Added);
        Assert.True(notificationPusher.PushCalled);
        Assert.Equal(dietologist.Id, notificationRepo.Added[0].UserId);
    }

    [Fact]
    public async Task InviteDietologist_WhenEmailDispatchFails_StillSucceeds() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = CreateInviteHandler(
            userRepository: userRepo,
            emailSender: new ThrowingEmailSender());

        var result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── AcceptInvitation ──

    [Fact]
    public async Task AcceptInvitation_WithNullUserId_ReturnsFailure() {
        var handler = CreateAcceptHandler();

        var result = await handler.Handle(
            new AcceptInvitationCommand(Guid.NewGuid(), "token", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AcceptInvitation_WhenNotFound_ReturnsFailure() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = CreateAcceptHandler(userRepository: userRepo);

        var result = await handler.Handle(
            new AcceptInvitationCommand(Guid.NewGuid(), "token", dietologistId.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AcceptInvitation_WithInvalidToken_ReturnsFailure() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId, tokenHash: "correct-hash");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = CreateAcceptHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            passwordHasher: new StubPasswordHasher(verifyResult: false));

        var result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "wrong-token", dietologistId.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AcceptInvitation_WithExpiredInvitation_ReturnsFailure() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        var invitation = DietologistInvitation.Create(
            clientId, "diet@example.com", "hash",
            DateTime.UtcNow.AddDays(-1), AllDomainPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = CreateAcceptHandler(invitationRepository: invRepo, userRepository: userRepo);

        var result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AcceptInvitation_WithValidData_Succeeds() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        userRepo.SeedRoles(new[] { RoleNames.Dietologist });

        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();

        var handler = CreateAcceptHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        var result = await handler.Handle(
            new AcceptInvitationCommand(invitation.Id.Value, "token", dietologistId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DietologistInvitationStatus.Accepted, invitation.Status);
        var notification = Assert.Single(notificationRepo.Added);
        Assert.Equal(NotificationTypes.DietologistInvitationAccepted, notification.Type);
        Assert.Equal(clientId, notification.UserId);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }

    [Fact]
    public async Task AcceptInvitationForCurrentUser_WithMatchingEmail_Succeeds() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        userRepo.SeedRoles(new[] { RoleNames.Dietologist });

        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();

        var handler = CreateAcceptCurrentUserHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        var result = await handler.Handle(
            new AcceptInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DietologistInvitationStatus.Accepted, invitation.Status);
        Assert.Contains(notificationRepo.Added, x => x.Type == NotificationTypes.DietologistInvitationAccepted && x.UserId == clientId);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_AfterAccepted_ReturnsAcceptedStatus() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invitation = CreateAcceptedInvitation(UserId.New(), dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistEmail))!
            .SetValue(invitation, "diet@example.com");

        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetInvitationForCurrentUserQueryHandler(invRepo, userRepo);

        var result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Accepted", result.Value.Status);
    }

    // ── DeclineInvitation ──

    [Fact]
    public async Task DeclineInvitation_WhenNotFound_ReturnsFailure() {
        var handler = CreateDeclineHandler();

        var result = await handler.Handle(
            new DeclineInvitationCommand(Guid.NewGuid(), "token", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeclineInvitation_WithInvalidToken_ReturnsFailure() {
        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = CreateDeclineHandler(
            invitationRepository: invRepo,
            passwordHasher: new StubPasswordHasher(verifyResult: false));

        var result = await handler.Handle(
            new DeclineInvitationCommand(invitation.Id.Value, "wrong", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeclineInvitation_WithValidToken_Succeeds() {
        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();

        var handler = CreateDeclineHandler(
            invitationRepository: invRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        var result = await handler.Handle(
            new DeclineInvitationCommand(invitation.Id.Value, "token", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DietologistInvitationStatus.Declined, invitation.Status);
        var notification = Assert.Single(notificationRepo.Added);
        Assert.Equal(NotificationTypes.DietologistInvitationDeclined, notification.Type);
        Assert.Equal(clientId, notification.UserId);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }

    [Fact]
    public async Task DeclineInvitationForCurrentUser_WithMatchingEmail_Succeeds() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var webPushSender = new FakeWebPushNotificationSender();

        var handler = CreateDeclineCurrentUserHandler(
            invitationRepository: invRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher,
            webPushNotificationSender: webPushSender);

        var result = await handler.Handle(
            new DeclineInvitationForCurrentUserCommand(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DietologistInvitationStatus.Declined, invitation.Status);
        Assert.Contains(notificationRepo.Added, x => x.Type == NotificationTypes.DietologistInvitationDeclined && x.UserId == clientId);
        Assert.True(notificationPusher.PushCalled);
        Assert.True(webPushSender.SendCalled);
    }

    // ── RevokeInvitation ──

    [Fact]
    public async Task RevokeInvitation_WithNullUserId_ReturnsFailure() {
        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new RevokeInvitationCommand(null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RevokeInvitation_WhenNothingToRevoke_ReturnsFailure() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        var result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RevokeInvitation_WithPendingInvitation_Succeeds() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invitation = CreatePendingInvitation(userId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new RevokeInvitationCommandHandler(invRepo, userRepo);

        var result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DietologistInvitationStatus.Revoked, invitation.Status);
    }

    [Fact]
    public async Task RevokeInvitation_WithActiveInvitation_Succeeds() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invitation = CreateAcceptedInvitation(userId, UserId.New());
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new RevokeInvitationCommandHandler(invRepo, userRepo);

        var result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── DisconnectDietologist ──

    [Fact]
    public async Task DisconnectDietologist_WithNullUserId_ReturnsFailure() {
        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new DisconnectDietologistCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DisconnectDietologist_WhenNoRelationship_ReturnsFailure() {
        var dietologistId = UserId.New();
        var user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        var result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DisconnectDietologist_WithActiveRelationship_Succeeds() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new DisconnectDietologistCommandHandler(invRepo, userRepo);

        var result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── UpdateDietologistPermissions ──

    [Fact]
    public async Task UpdatePermissions_WithNullUserId_ReturnsFailure() {
        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(null, AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdatePermissions_WhenNoActiveRelationship_ReturnsFailure() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        var result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, AllPermissions),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdatePermissions_WithActiveRelationship_Succeeds() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invitation = CreateAcceptedInvitation(userId, UserId.New());
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new UpdateDietologistPermissionsCommandHandler(invRepo, userRepo);

        var newPermissions = new DietologistPermissionsInput(false, false, true, true, true, true);
        var result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, newPermissions),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── CreateRecommendation ──

    [Fact]
    public async Task CreateRecommendation_WithNullUserId_ReturnsFailure() {
        var handler = new CreateRecommendationCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryRecommendationRepository());

        var result = await handler.Handle(
            new CreateRecommendationCommand(null, Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateRecommendation_WhenNoAccess_ReturnsFailure() {
        var handler = new CreateRecommendationCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryRecommendationRepository());

        var result = await handler.Handle(
            new CreateRecommendationCommand(Guid.NewGuid(), Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateRecommendation_WithAccess_Succeeds() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var recRepo = new InMemoryRecommendationRepository();
        var handler = new CreateRecommendationCommandHandler(invRepo, recRepo);

        var result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Eat more veggies", result.Value.Text);
        Assert.Single(recRepo.Added);
    }

    // ── MarkRecommendationRead ──

    [Fact]
    public async Task MarkRecommendationRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkRecommendationReadCommandHandler(new InMemoryRecommendationRepository());

        var result = await handler.Handle(
            new MarkRecommendationReadCommand(null, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MarkRecommendationRead_WhenNotFound_ReturnsFailure() {
        var handler = new MarkRecommendationReadCommandHandler(new InMemoryRecommendationRepository());

        var result = await handler.Handle(
            new MarkRecommendationReadCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MarkRecommendationRead_WhenNotOwned_ReturnsFailure() {
        var clientId = UserId.New();
        var otherId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var handler = new MarkRecommendationReadCommandHandler(recRepo);

        var result = await handler.Handle(
            new MarkRecommendationReadCommand(otherId.Value, recommendation.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MarkRecommendationRead_WithValidOwner_Succeeds() {
        var clientId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var handler = new MarkRecommendationReadCommandHandler(recRepo);

        var result = await handler.Handle(
            new MarkRecommendationReadCommand(clientId.Value, recommendation.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(recommendation.IsRead);
    }

    // ── GetMyDietologist ──

    [Fact]
    public async Task GetMyDietologist_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyDietologistQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new GetMyDietologistQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetMyDietologist_WhenNoDietologist_ReturnsNull() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new GetMyDietologistQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        var result = await handler.Handle(
            new GetMyDietologistQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    // ── GetMyClients ──

    [Fact]
    public async Task GetMyClients_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyClientsQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new GetMyClientsQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetMyClients_WhenNoClients_ReturnsEmptyList() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new GetMyClientsQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        var result = await handler.Handle(
            new GetMyClientsQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetMyClients_WhenProfileSharingDisabled_HidesProfileFields() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var dietologist = CreateUser(dietologistId, "diet@example.com");
        var client = CreateUser(clientId, "client@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(client, "Alice");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(client, "Walker");
        typeof(User).GetProperty(nameof(User.ProfileImage))!.SetValue(client, "https://cdn.example.com/avatar.jpg");
        typeof(User).GetProperty(nameof(User.Gender))!.SetValue(client, "F");
        typeof(User).GetProperty(nameof(User.Height))!.SetValue(client, 170d);
        typeof(User).GetProperty(nameof(User.ActivityLevel))!.SetValue(client, ActivityLevel.High);
        typeof(User).GetProperty(nameof(User.BirthDate))!.SetValue(client, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var invitation = CreateAcceptedInvitation(
            clientId,
            dietologistId,
            new DietologistPermissions(
                ShareMeals: true,
                ShareStatistics: true,
                ShareWeight: true,
                ShareWaist: true,
                ShareGoals: true,
                ShareHydration: true,
                ShareProfile: false,
                ShareFasting: true));

        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!.SetValue(invitation, client);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistUser))!.SetValue(invitation, dietologist);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(dietologist);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetMyClientsQueryHandler(invRepo, userRepo);

        var result = await handler.Handle(new GetMyClientsQuery(dietologistId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var clientSummary = Assert.Single(result.Value);
        Assert.Equal("client@example.com", clientSummary.Email);
        Assert.Null(clientSummary.FirstName);
        Assert.Null(clientSummary.LastName);
        Assert.Null(clientSummary.ProfileImage);
        Assert.Null(clientSummary.BirthDate);
        Assert.Null(clientSummary.Gender);
        Assert.Null(clientSummary.Height);
        Assert.Null(clientSummary.ActivityLevel);
        Assert.False(clientSummary.Permissions.ShareProfile);
    }

    // ── GetInvitationByToken ──

    [Fact]
    public async Task GetInvitationByToken_WhenNotFound_ReturnsFailure() {
        var handler = new GetInvitationByTokenQueryHandler(new InMemoryInvitationRepository());

        var result = await handler.Handle(
            new GetInvitationByTokenQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetInvitationByToken_WhenExpired_ReturnsFailure() {
        var clientId = UserId.New();
        var invitation = DietologistInvitation.Create(
            clientId, "diet@example.com", "hash",
            DateTime.UtcNow.AddDays(-1), AllDomainPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetInvitationByTokenQueryHandler(invRepo);

        var result = await handler.Handle(
            new GetInvitationByTokenQuery(invitation.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetInvitationByToken_WhenNotPending_ReturnsFailure() {
        var clientId = UserId.New();
        var invitation = CreatePendingInvitation(clientId);
        invitation.Decline();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetInvitationByTokenQueryHandler(invRepo);

        var result = await handler.Handle(
            new GetInvitationByTokenQuery(invitation.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    // ── GetClientDashboard ──

    [Fact]
    public async Task GetClientDashboard_WithNullUserId_ReturnsFailure() {
        var handler = new GetClientDashboardQueryHandler(
            new InMemoryInvitationRepository(), new StubMediator());

        var result = await handler.Handle(
            new GetClientDashboardQuery(null, Guid.NewGuid(), DateTime.UtcNow, 1, 10, "en", 7),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetClientDashboard_WhenNoAccess_ReturnsFailure() {
        var handler = new GetClientDashboardQueryHandler(
            new InMemoryInvitationRepository(), new StubMediator());

        var result = await handler.Handle(
            new GetClientDashboardQuery(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 1, 10, "en", 7),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetClientDashboard_WhenStatisticsDenied_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var noStatsPermissions = new DietologistPermissions(true, false, true, true, true, true);
        var invitation = CreateAcceptedInvitation(clientId, dietologistId, noStatsPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetClientDashboardQueryHandler(invRepo, new StubMediator());

        var result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, clientId.Value, DateTime.UtcNow, 1, 10, "en", 7),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    // ── GetClientGoals ──

    [Fact]
    public async Task GetClientGoals_WithNullUserId_ReturnsFailure() {
        var handler = new GetClientGoalsQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new GetClientGoalsQuery(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetClientGoals_WhenNoAccess_ReturnsFailure() {
        var handler = new GetClientGoalsQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new GetClientGoalsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetClientGoals_WhenGoalsDenied_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var noGoalsPermissions = new DietologistPermissions(true, true, true, true, false, true);
        var invitation = CreateAcceptedInvitation(clientId, dietologistId, noGoalsPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetClientGoalsQueryHandler(invRepo, new InMemoryUserRepository());

        var result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetClientGoals_WithAccess_ReturnsUser() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var clientUser = CreateUser(clientId, "client@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(clientUser);

        var handler = new GetClientGoalsQueryHandler(invRepo, userRepo);

        var result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── GetMyRecommendations ──

    [Fact]
    public async Task GetMyRecommendations_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyRecommendationsQueryHandler(
            new InMemoryRecommendationRepository(), new InMemoryUserRepository());

        var result = await handler.Handle(
            new GetMyRecommendationsQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetMyRecommendations_WithValidUser_ReturnsRecommendations() {
        var userId = UserId.New();
        var user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var rec1 = Recommendation.Create(UserId.New(), userId, "Rec 1");
        var rec2 = Recommendation.Create(UserId.New(), userId, "Rec 2");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(rec1);
        recRepo.Seed(rec2);

        var handler = new GetMyRecommendationsQueryHandler(recRepo, userRepo);

        var result = await handler.Handle(
            new GetMyRecommendationsQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    // ── GetRecommendationsForClient ──

    [Fact]
    public async Task GetRecommendationsForClient_WithNullUserId_ReturnsFailure() {
        var handler = new GetRecommendationsForClientQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryRecommendationRepository());

        var result = await handler.Handle(
            new GetRecommendationsForClientQuery(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetRecommendationsForClient_WhenNoAccess_ReturnsFailure() {
        var handler = new GetRecommendationsForClientQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryRecommendationRepository());

        var result = await handler.Handle(
            new GetRecommendationsForClientQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetRecommendationsForClient_WithAccess_ReturnsRecommendations() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var rec = Recommendation.Create(dietologistId, clientId, "Eat well");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(rec);

        var handler = new GetRecommendationsForClientQueryHandler(invRepo, recRepo);

        var result = await handler.Handle(
            new GetRecommendationsForClientQuery(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    // ── RecommendationCreatedEventHandler ──

    [Fact]
    public async Task RecommendationCreatedEventHandler_CreatesNotificationAndPushes() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var recId = RecommendationId.New();

        var dietologist = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(dietologist);

        var notifRepo = new InMemoryNotificationRepository();
        var pusher = new FakeNotificationPusher();

        var handler = new RecommendationCreatedEventHandler(
            notifRepo,
            pusher,
            userRepo);
        var domainEvent = new RecommendationCreatedDomainEvent(recId, dietologistId, clientId);

        await handler.Handle(
            new RecommendationCreatedNotification(domainEvent), CancellationToken.None);

        Assert.Single(notifRepo.Added);
        Assert.True(pusher.PushCalled);
    }

    [Fact]
    public async Task RecommendationCreatedEventHandler_WhenDietologistNotFound_UsesDefaultName() {
        var recId = RecommendationId.New();
        var clientId = UserId.New();
        var dietologistId = UserId.New();

        var notifRepo = new InMemoryNotificationRepository();
        var pusher = new FakeNotificationPusher();

        var handler = new RecommendationCreatedEventHandler(
            notifRepo, pusher, new InMemoryUserRepository());
        var domainEvent = new RecommendationCreatedDomainEvent(recId, dietologistId, clientId);

        await handler.Handle(
            new RecommendationCreatedNotification(domainEvent), CancellationToken.None);

        Assert.Single(notifRepo.Added);
    }

    // ── Test Doubles ──

    private sealed class InMemoryInvitationRepository : IDietologistInvitationRepository {
        private readonly List<DietologistInvitation> _invitations = [];
        public List<DietologistInvitation> Added { get; } = [];

        public void Seed(DietologistInvitation invitation) => _invitations.Add(invitation);

        public Task<DietologistInvitation?> GetByIdAsync(
            DietologistInvitationId id,
            bool asTracking = false,
            CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(i => i.Id == id));

        public Task<DietologistInvitation?> GetByClientAndStatusAsync(
            UserId clientUserId, DietologistInvitationStatus status, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(i => i.ClientUserId == clientUserId && i.Status == status));

        public Task<DietologistInvitation?> GetActiveByClientAsync(
            UserId clientUserId, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(
                i => i.ClientUserId == clientUserId && i.Status == DietologistInvitationStatus.Accepted));

        public Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
            UserId clientUserId, UserId dietologistUserId, CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(
                i => i.ClientUserId == clientUserId && i.DietologistUserId == dietologistUserId
                     && i.Status == DietologistInvitationStatus.Accepted));

        public Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
            UserId dietologistUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DietologistInvitation>>(
                _invitations.Where(i => i.DietologistUserId == dietologistUserId
                                        && i.Status == DietologistInvitationStatus.Accepted).ToList());

        public Task<bool> HasActiveRelationshipAsync(
            UserId clientUserId, UserId dietologistUserId, CancellationToken ct = default) =>
            Task.FromResult(_invitations.Any(
                i => i.ClientUserId == clientUserId && i.DietologistUserId == dietologistUserId
                     && i.Status == DietologistInvitationStatus.Accepted));

        public Task<DietologistInvitation> AddAsync(DietologistInvitation invitation, CancellationToken ct = default) {
            _invitations.Add(invitation);
            Added.Add(invitation);
            return Task.FromResult(invitation);
        }

        public Task UpdateAsync(DietologistInvitation invitation, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryUserRepository : IUserRepository {
        private readonly List<User> _users = [];
        private readonly List<Role> _roles = [];

        public void Seed(User user) => _users.Add(user);

        public void SeedRoles(IEnumerable<string> roleNames) {
            foreach (var name in roleNames) {
                _roles.Add(Role.Create(name));
            }
        }

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Role>>(_roles.Where(r => names.Contains(r.Name)).ToList());

        public Task<User> AddAsync(User user, CancellationToken ct = default) {
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryRecommendationRepository : IRecommendationRepository {
        private readonly List<Recommendation> _recommendations = [];
        public List<Recommendation> Added { get; } = [];

        public void Seed(Recommendation recommendation) => _recommendations.Add(recommendation);

        public Task<IReadOnlyList<Recommendation>> GetByClientAsync(
            UserId clientUserId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Recommendation>>(
                _recommendations.Where(r => r.ClientUserId == clientUserId).Take(limit).ToList());

        public Task<IReadOnlyList<Recommendation>> GetByDietologistAndClientAsync(
            UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Recommendation>>(
                _recommendations.Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
                    .Take(limit).ToList());

        public Task<Recommendation?> GetByIdAsync(
            RecommendationId id, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_recommendations.FirstOrDefault(r => r.Id == id));

        public Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken ct = default) {
            _recommendations.Add(recommendation);
            Added.Add(recommendation);
            return Task.FromResult(recommendation);
        }

        public Task UpdateAsync(Recommendation recommendation, CancellationToken ct = default) => Task.CompletedTask;

        public Task<int> GetUnreadCountAsync(UserId clientUserId, CancellationToken ct = default) =>
            Task.FromResult(_recommendations.Count(r => r.ClientUserId == clientUserId && !r.IsRead));
    }

    private sealed class InMemoryNotificationRepository : INotificationRepository {
        private readonly List<Notification> _notifications = [];
        public List<Notification> Added { get; } = [];

        public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default) {
            _notifications.Add(notification);
            Added.Add(notification);
            return Task.FromResult(notification);
        }

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead && n.Type == type));

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Any(n => n.UserId == userId && n.Type == type && n.ReferenceId == referenceId));

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task UpdateAsync(Notification notification, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task MarkAllReadAsync(UserId userId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubPasswordHasher(bool verifyResult = true) : IPasswordHasher {
        public string Hash(string password) => $"hashed-{password}";
        public bool Verify(string password, string hashedPassword) => verifyResult;
    }

    private sealed class FakeEmailSender : IDietologistEmailSender {
        public int SentCount { get; private set; }

        public Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken ct = default) {
            SentCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailSender : IDietologistEmailSender {
        public Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken ct = default) =>
            throw new InvalidOperationException("SMTP unavailable");
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    private sealed class FakeNotificationPusher : INotificationPusher {
        public bool PushCalled { get; private set; }

        public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken ct = default) {
            PushCalled = true;
            return Task.CompletedTask;
        }

        public Task PushNotificationsChangedAsync(Guid userId, CancellationToken ct = default) {
            PushCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWebPushNotificationSender : IWebPushNotificationSender {
        public bool SendCalled { get; private set; }

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            SendCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubMediator : ISender {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
