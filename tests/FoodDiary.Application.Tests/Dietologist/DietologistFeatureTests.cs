using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.EventHandlers;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetClientDashboard;
using FoodDiary.Application.Dietologist.Queries.GetClientGoals;
using FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;
using FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Queries.GetMyClients;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;
using FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public class DietologistFeatureTests {
    private static readonly DietologistPermissionsInput AllPermissions = new(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true);
    private static readonly DietologistPermissions AllDomainPermissions = new(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true);

    private static User CreateUser(UserId? id = null, string email = "user@example.com") {
        var user = User.Create(email, "hashed-password");
        if (id is not null) {
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        }

        return user;
    }

    private static User CreateDeletedUser(UserId id, string email = "deleted@example.com") {
        User user = CreateUser(id, email);
        user.MarkDeleted(DateTime.UtcNow);
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
        DietologistInvitation invitation = CreatePendingInvitation(clientId, permissions: permissions);
        invitation.Accept(dietologistId);
        return invitation;
    }

    private static DashboardSnapshotModel CreateDashboardSnapshot() {
        DateTime date = DateTime.UtcNow;
        return new DashboardSnapshotModel(
            date,
            date,
            DailyGoal: 2000,
            WeeklyCalorieGoal: 14000,
            new DashboardStatisticsModel(
                TotalCalories: 1200,
                AverageProteins: 80,
                AverageFats: 40,
                AverageCarbs: 150,
                AverageFiber: 20,
                ProteinGoal: null,
                FatGoal: null,
                CarbGoal: null,
                FiberGoal: null),
            [new DailyCaloriesModel(date, 1200)],
            new DashboardWeightModel(new WeightPointModel(date, 72), Previous: null, 70),
            new DashboardWaistModel(new WaistPointModel(date, 82), Previous: null, 80),
            new DashboardMealsModel([], 2),
            Hydration: null);
    }

    [Fact]
    public async Task DietologistUserContextService_GetUserByIdAsync_ReturnsRepositoryUser() {
        var user = User.Create("dietologist-context@example.com", "hash");
        IDietologistUserLookupService userLookupService = Substitute.For<IDietologistUserLookupService>();
        userLookupService.GetUserByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        var service = new DietologistUserContextService(userContextService, userLookupService);

        User? result = await service.GetUserByIdAsync(user.Id, CancellationToken.None);

        Assert.Same(user, result);
    }

    private static InviteDietologistCommandHandler CreateInviteHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IDietologistUserContextService? userRepository = null,
        IPasswordHasher? passwordHasher = null,
        IDietologistEmailSender? emailSender = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        TimeProvider? dateTimeProvider = null) {
        INotificationRepository resolvedNotificationRepository = notificationRepository ?? new InMemoryNotificationRepository();
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            userRepository ?? new InMemoryUserRepository(),
            passwordHasher ?? new StubPasswordHasher(),
            emailSender ?? new FakeEmailSender(),
            new InMemoryNotificationWriter(resolvedNotificationRepository, new FakeWebPushNotificationSender()),
            resolvedNotificationRepository,
            notificationPusher ?? new FakeNotificationPusher(),
            new ImmediatePostCommitActionQueue(),
            dateTimeProvider ?? new StubDateTimeProvider(),
            NullLogger<InviteDietologistCommandHandler>.Instance);
    }

    private static AcceptInvitationCommandHandler CreateAcceptHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IDietologistUserContextService? userRepository = null,
        IUserRoleMembershipService? userRoleMembershipService = null,
        IPasswordHasher? passwordHasher = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) {
        INotificationRepository resolvedNotificationRepository = notificationRepository ?? new InMemoryNotificationRepository();
        IDietologistUserContextService resolvedUserContext = userRepository ?? new InMemoryUserRepository();
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            resolvedUserContext,
            userRoleMembershipService ?? new RecordingUserRoleMembershipService(),
            passwordHasher ?? new StubPasswordHasher(),
            new InMemoryNotificationWriter(resolvedNotificationRepository, webPushNotificationSender ?? new FakeWebPushNotificationSender()),
            resolvedNotificationRepository,
            notificationPusher ?? new FakeNotificationPusher(),
            new ImmediatePostCommitActionQueue());
    }

    private static AcceptInvitationForCurrentUserCommandHandler CreateAcceptCurrentUserHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IDietologistUserContextService? userRepository = null,
        IUserRoleMembershipService? userRoleMembershipService = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) {
        INotificationRepository resolvedNotificationRepository = notificationRepository ?? new InMemoryNotificationRepository();
        IDietologistUserContextService resolvedUserContext = userRepository ?? new InMemoryUserRepository();
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            resolvedUserContext,
            userRoleMembershipService ?? new RecordingUserRoleMembershipService(),
            new InMemoryNotificationWriter(resolvedNotificationRepository, webPushNotificationSender ?? new FakeWebPushNotificationSender()),
            resolvedNotificationRepository,
            notificationPusher ?? new FakeNotificationPusher(),
            new ImmediatePostCommitActionQueue());
    }

    private static DeclineInvitationCommandHandler CreateDeclineHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IPasswordHasher? passwordHasher = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) {
        INotificationRepository resolvedNotificationRepository = notificationRepository ?? new InMemoryNotificationRepository();
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            passwordHasher ?? new StubPasswordHasher(),
            new InMemoryNotificationWriter(resolvedNotificationRepository, webPushNotificationSender ?? new FakeWebPushNotificationSender()),
            resolvedNotificationRepository,
            notificationPusher ?? new FakeNotificationPusher(),
            new ImmediatePostCommitActionQueue());
    }

    private static DeclineInvitationForCurrentUserCommandHandler CreateDeclineCurrentUserHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IDietologistUserContextService? userRepository = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IWebPushNotificationSender? webPushNotificationSender = null) {
        INotificationRepository resolvedNotificationRepository = notificationRepository ?? new InMemoryNotificationRepository();
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            userRepository ?? new InMemoryUserRepository(),
            new InMemoryNotificationWriter(resolvedNotificationRepository, webPushNotificationSender ?? new FakeWebPushNotificationSender()),
            resolvedNotificationRepository,
            notificationPusher ?? new FakeNotificationPusher(),
            new ImmediatePostCommitActionQueue());
    }

    private static CreateRecommendationCommandHandler CreateRecommendationHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IRecommendationRepository? recommendationRepository = null,
        INotificationRepository? notificationRepository = null,
        INotificationPusher? notificationPusher = null,
        IDietologistUserContextService? userRepository = null) {
        INotificationRepository resolvedNotificationRepository = notificationRepository ?? new InMemoryNotificationRepository();
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            recommendationRepository ?? new InMemoryRecommendationRepository(),
            new InMemoryNotificationWriter(resolvedNotificationRepository, new FakeWebPushNotificationSender()),
            resolvedNotificationRepository,
            notificationPusher ?? new FakeNotificationPusher(),
            userRepository ?? new InMemoryUserRepository(),
            new ImmediatePostCommitActionQueue());
    }

    // â”€â”€ InviteDietologist â”€â”€

    [Fact]
    public async Task InviteDietologist_WithNullUserId_ReturnsFailure() {
        InviteDietologistCommandHandler handler = CreateInviteHandler();

        Result result = await handler.Handle(
            new InviteDietologistCommand(UserId: null, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task InviteDietologist_WhenUserNotFound_ReturnsFailure() {
        InviteDietologistCommandHandler handler = CreateInviteHandler();

        Result result = await handler.Handle(
            new InviteDietologistCommand(Guid.NewGuid(), "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task InviteDietologist_WhenInvitingSelf_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId, "user@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        InviteDietologistCommandHandler handler = CreateInviteHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "user@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task InviteDietologist_WhenAlreadyHasActiveDietologist_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        DietologistInvitation activeInvitation = CreateAcceptedInvitation(userId, UserId.New());
        invRepo.Seed(activeInvitation);

        InviteDietologistCommandHandler handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task InviteDietologist_WhenPendingExists_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        DietologistInvitation pending = CreatePendingInvitation(userId);
        invRepo.Seed(pending);

        InviteDietologistCommandHandler handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task InviteDietologist_WithValidData_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        var emailSender = new FakeEmailSender();

        InviteDietologistCommandHandler handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo, emailSender: emailSender);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(invRepo.Added);
        Assert.True(emailSender.SentCount > 0);
    }

    [Fact]
    public async Task InviteDietologist_WhenRegisteredDietologistExists_CreatesNotificationAndPushesUpdate() {
        var userId = UserId.New();
        User client = CreateUser(userId, "client@example.com");
        User dietologist = CreateUser(UserId.New(), "diet@example.com");

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(client);
        userRepo.Seed(dietologist);

        var invitationRepo = new InMemoryInvitationRepository();
        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();

        InviteDietologistCommandHandler handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(notificationRepo.Added);
        Assert.True(notificationPusher.PushCalled);
        Assert.Equal(dietologist.Id, notificationRepo.Added[0].UserId);
    }

    [Fact]
    public async Task InviteDietologist_WhenEmailDispatchFailsForUnregisteredDietologist_StillCreatesInvitation() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        var invitationRepo = new InMemoryInvitationRepository();

        InviteDietologistCommandHandler handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            emailSender: new ThrowingEmailSender());

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(invitationRepo.Added);
    }

    [Fact]
    public async Task InviteDietologist_WhenEmailDispatchFailsForRegisteredDietologist_UsesNotificationFallback() {
        var userId = UserId.New();
        User client = CreateUser(userId, "client@example.com");
        User dietologist = CreateUser(UserId.New(), "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(client);
        userRepo.Seed(dietologist);
        var invitationRepo = new InMemoryInvitationRepository();
        var notificationRepo = new InMemoryNotificationRepository();

        InviteDietologistCommandHandler handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            emailSender: new ThrowingEmailSender(),
            notificationRepository: notificationRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(invitationRepo.Added);
        Assert.Single(notificationRepo.Added);
    }

    // â”€â”€ AcceptInvitation â”€â”€

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

    [Fact]
    public async Task GetInvitationForCurrentUser_AfterAccepted_ReturnsAcceptedStatus() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(UserId.New(), dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistEmail))!
            .SetValue(invitation, "diet@example.com");

        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetInvitationForCurrentUserQueryHandler(invRepo, userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Accepted", result.Value.Status);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_WithNullUserId_ReturnsFailure() {
        var handler = new GetInvitationForCurrentUserQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_WhenUserDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));
        var handler = new GetInvitationForCurrentUserQueryHandler(new InMemoryInvitationRepository(), userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_WhenInvitationMissingAfterUserAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        var handler = new GetInvitationForCurrentUserQueryHandler(
            new InMemoryInvitationRepository(),
            new SequenceUserRepository(CreateUser(dietologistId, "diet@example.com"), null));

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_WhenInvitationMissing_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetInvitationForCurrentUserQueryHandler(new InMemoryInvitationRepository(), userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_WhenEmailDoesNotMatch_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "other@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);
        var handler = new GetInvitationForCurrentUserQueryHandler(invRepo, userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }

    // â”€â”€ DeclineInvitation â”€â”€

    [Fact]
    public async Task DeclineInvitation_WhenNotFound_ReturnsFailure() {
        DeclineInvitationCommandHandler handler = CreateDeclineHandler();

        Result result = await handler.Handle(
            new DeclineInvitationCommand(Guid.NewGuid(), "token", UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
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

    // â”€â”€ RevokeInvitation â”€â”€

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

    [Fact]
    public async Task RevokeInvitation_WithNullUserId_ReturnsFailure() {
        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new RevokeInvitationCommand(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task RevokeInvitation_WhenNothingToRevoke_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task RevokeInvitation_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RevokeInvitation_WithPendingInvitation_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreatePendingInvitation(userId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new RevokeInvitationCommandHandler(invRepo, userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DietologistInvitationStatus.Revoked, invitation.Status);
    }

    [Fact]
    public async Task RevokeInvitation_WithActiveInvitation_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(userId, UserId.New());
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new RevokeInvitationCommandHandler(invRepo, userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
    }

    // â”€â”€ DisconnectDietologist â”€â”€

    [Fact]
    public async Task DisconnectDietologist_WithNullUserId_ReturnsFailure() {
        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DisconnectDietologist_WhenNoRelationship_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DisconnectDietologist_WhenUserDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId));

        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisconnectDietologist_WithActiveRelationship_Succeeds() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new DisconnectDietologistCommandHandler(invRepo, userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
    }

    // â”€â”€ UpdateDietologistPermissions â”€â”€

    [Fact]
    public async Task UpdatePermissions_WithNullUserId_ReturnsFailure() {
        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(UserId: null, AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UpdatePermissions_WhenNoActiveRelationship_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UpdatePermissions_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdatePermissions_WithActiveRelationship_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(userId, UserId.New());
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new UpdateDietologistPermissionsCommandHandler(invRepo, userRepo);

        var newPermissions = new DietologistPermissionsInput(ShareMeals: false, ShareStatistics: false, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true);
        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, newPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
    }

    // â”€â”€ CreateRecommendation â”€â”€

    [Fact]
    public async Task CreateRecommendation_WithNullUserId_ReturnsFailure() {
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler();

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(UserId: null, Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CreateRecommendation_WhenNoAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CreateRecommendation_WhenDietologistDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRecommendation_WithAccess_Succeeds() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var recRepo = new InMemoryRecommendationRepository();
        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(invRepo, recRepo, notificationRepo, notificationPusher, userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Eat more veggies", result.Value.Text);
        Assert.Single(recRepo.Added);
        Notification notification = Assert.Single(notificationRepo.Added);
        Assert.Equal(NotificationTypes.NewRecommendation, notification.Type);
        Assert.True(notificationPusher.PushCalled);
    }

    [Fact]
    public async Task CreateRecommendation_WhenDietologistMissingDuringNotification_UsesEmptyLabel() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var userRepo = new SequenceUserRepository(CreateUser(dietologistId, "diet@example.com"), null);
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(
            invitationRepository: invRepo,
            notificationRepository: notificationRepo,
            userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Success(result);
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(
            Assert.Single(notificationRepo.Added).PayloadJson);
        Assert.Equal("diet@example.com", payload?.DietologistName);
    }

    [Fact]
    public async Task CreateRecommendation_WhenAnyPermissionIsMissing_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var limitedPermissions = new DietologistPermissions(
            ShareMeals: true,
            ShareStatistics: true,
            ShareWeight: true,
            ShareWaist: true,
            ShareGoals: true,
            ShareHydration: true,
            ShareProfile: false,
            ShareFasting: true);
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId, limitedPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);
        var recRepo = new InMemoryRecommendationRepository();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(
            invitationRepository: invRepo,
            recommendationRepository: recRepo,
            userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("PermissionDenied", result.Error.Code, StringComparison.Ordinal);
        Assert.Empty(recRepo.Added);
    }

    // â”€â”€ MarkRecommendationRead â”€â”€

    [Fact]
    public async Task MarkRecommendationRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkRecommendationReadCommandHandler(
            new InMemoryRecommendationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkRecommendationRead_WhenNotFound_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(userId));
        var handler = new MarkRecommendationReadCommandHandler(new InMemoryRecommendationRepository(), userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(userId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkRecommendationRead_WhenNotOwned_ReturnsFailure() {
        var clientId = UserId.New();
        var otherId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(otherId));
        var handler = new MarkRecommendationReadCommandHandler(recRepo, userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(otherId.Value, recommendation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkRecommendationRead_WithValidOwner_Succeeds() {
        var clientId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(clientId));
        var handler = new MarkRecommendationReadCommandHandler(recRepo, userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(clientId.Value, recommendation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(recommendation.IsRead);
    }

    [Fact]
    public async Task MarkRecommendationRead_WhenUserDeleted_ReturnsFailure() {
        var clientId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(clientId));
        var handler = new MarkRecommendationReadCommandHandler(recRepo, userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(clientId.Value, recommendation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
        Assert.False(recommendation.IsRead);
    }

    // â”€â”€ GetMyDietologist â”€â”€

    [Fact]
    public async Task GetMyDietologist_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyDietologistQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result<DietologistInfoModel?> result = await handler.Handle(
            new GetMyDietologistQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetMyDietologist_WhenNoDietologist_ReturnsNull() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new GetMyDietologistQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result<DietologistInfoModel?> result = await handler.Handle(
            new GetMyDietologistQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetMyDietologist_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new GetMyDietologistQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result<DietologistInfoModel?> result = await handler.Handle(
            new GetMyDietologistQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    // â”€â”€ GetMyClients â”€â”€

    [Fact]
    public async Task GetMyClients_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyClientsQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result<IReadOnlyList<ClientSummaryModel>> result = await handler.Handle(
            new GetMyClientsQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetMyClients_WhenNoClients_ReturnsEmptyList() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new GetMyClientsQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result<IReadOnlyList<ClientSummaryModel>> result = await handler.Handle(
            new GetMyClientsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetMyClients_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new GetMyClientsQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result<IReadOnlyList<ClientSummaryModel>> result = await handler.Handle(
            new GetMyClientsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMyClients_WhenProfileSharingDisabled_HidesProfileFields() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        User dietologist = CreateUser(dietologistId, "diet@example.com");
        User client = CreateUser(clientId, "client@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(client, "Alice");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(client, "Walker");
        typeof(User).GetProperty(nameof(User.ProfileImage))!.SetValue(client, "https://cdn.example.com/avatar.jpg");
        typeof(User).GetProperty(nameof(User.Gender))!.SetValue(client, "F");
        typeof(User).GetProperty(nameof(User.Height))!.SetValue(client, 170d);
        typeof(User).GetProperty(nameof(User.ActivityLevel))!.SetValue(client, ActivityLevel.High);
        typeof(User).GetProperty(nameof(User.BirthDate))!.SetValue(client, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        DietologistInvitation invitation = CreateAcceptedInvitation(
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

        Result<IReadOnlyList<ClientSummaryModel>> result = await handler.Handle(new GetMyClientsQuery(dietologistId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        ClientSummaryModel clientSummary = Assert.Single(result.Value);
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

    // â”€â”€ GetInvitationByToken â”€â”€

    [Fact]
    public async Task GetInvitationByToken_WhenNotFound_ReturnsFailure() {
        var handler = new GetInvitationByTokenQueryHandler(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository());

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetInvitationByToken_WithEmptyUserId_ReturnsFailure() {
        var handler = new GetInvitationByTokenQueryHandler(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository());

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetInvitationByToken_WhenExpired_ReturnsFailure() {
        var clientId = UserId.New();
        var invitation = DietologistInvitation.Create(
            clientId, "diet@example.com", "hash",
            DateTime.UtcNow.AddDays(-1), AllDomainPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);
        var userRepo = new InMemoryUserRepository();
        var dietologistId = UserId.New();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        var handler = new GetInvitationByTokenQueryHandler(invRepo, userRepo);

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(dietologistId.Value, invitation.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetInvitationByToken_WhenNotPending_ReturnsFailure() {
        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId);
        invitation.Decline();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);
        var userRepo = new InMemoryUserRepository();
        var dietologistId = UserId.New();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        var handler = new GetInvitationByTokenQueryHandler(invRepo, userRepo);

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(dietologistId.Value, invitation.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetInvitationByToken_WhenCurrentUserEmailDoesNotMatchInvitation_ReturnsFailure() {
        var clientId = UserId.New();
        DietologistInvitation invitation = CreatePendingInvitation(clientId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "other@example.com"));
        var handler = new GetInvitationByTokenQueryHandler(invRepo, userRepo);

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.InvitationNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetInvitationByToken_WithPendingInvitation_ReturnsModel() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        DietologistInvitation invitation = CreatePendingInvitation(UserId.New(), "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetInvitationByTokenQueryHandler(invRepo, userRepo);

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(invitation.Id.Value, result.Value.InvitationId);
        Assert.Equal("client@example.com", result.Value.ClientEmail);
        Assert.Equal(DietologistInvitationStatus.Pending.ToString(), result.Value.Status);
    }

    // â”€â”€ GetClientDashboard â”€â”€

    [Fact]
    public async Task GetClientDashboard_WithNullUserId_ReturnsFailure() {
        var handler = new GetClientDashboardQueryHandler(
            new InMemoryInvitationRepository(), new ThrowingDashboardSnapshotBuilder(), new InMemoryUserRepository());

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(UserId: null, Guid.NewGuid(), DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientDashboard_WhenNoAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetClientDashboardQueryHandler(
            new InMemoryInvitationRepository(), new ThrowingDashboardSnapshotBuilder(), userRepo);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, Guid.NewGuid(), DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientDashboard_WhenMealsAllowedAndStatisticsDenied_ReturnsMaskedDashboard() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var noStatsPermissions = new DietologistPermissions(
            ShareMeals: true,
            ShareStatistics: false,
            ShareWeight: false,
            ShareWaist: false,
            ShareGoals: false,
            ShareHydration: false,
            ShareProfile: false,
            ShareFasting: false);
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId, noStatsPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var snapshotBuilder = new RecordingDashboardSnapshotBuilder(CreateDashboardSnapshot());
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetClientDashboardQueryHandler(invRepo, snapshotBuilder, userRepo);
        DateTime dateFrom = DateTime.UtcNow.Date;
        DateTime dateTo = dateFrom.AddDays(6);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, clientId.Value, dateFrom, dateTo, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Meals.Total);
        Assert.Equal(0, result.Value.Statistics.TotalCalories);
        Assert.Null(result.Value.Weight.Latest);
        Assert.Null(result.Value.Hydration);
        DashboardSnapshotRequest request = Assert.IsType<DashboardSnapshotRequest>(snapshotBuilder.LastRequest);
        Assert.NotNull(request.Sections);
        Assert.True(request.Sections.IncludeMeals);
        Assert.False(request.Sections.IncludeStatistics);
        Assert.False(request.Sections.IncludeWeight);
        Assert.Equal(dateTo, request.DateTo?.Date);
    }

    [Fact]
    public async Task GetClientDashboard_WhenNoDashboardPermissions_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var noDashboardPermissions = new DietologistPermissions(
            ShareMeals: false,
            ShareStatistics: false,
            ShareWeight: false,
            ShareWaist: false,
            ShareGoals: true,
            ShareHydration: false,
            ShareProfile: true,
            ShareFasting: false);
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId, noDashboardPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetClientDashboardQueryHandler(invRepo, new ThrowingDashboardSnapshotBuilder(), userRepo);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, clientId.Value, DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientDashboard_WhenDietologistDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        var handler = new GetClientDashboardQueryHandler(
            invRepo, new ThrowingDashboardSnapshotBuilder(), userRepo);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, clientId.Value, DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetClientDashboard_WhenDashboardBuilderFails_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        var handler = new GetClientDashboardQueryHandler(
            invRepo, new FailingDashboardSnapshotBuilder(), userRepo);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, clientId.Value, DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }

    // â”€â”€ GetClientGoals â”€â”€

    [Fact]
    public async Task GetClientGoals_WithNullUserId_ReturnsFailure() {
        var handler = new GetClientGoalsQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientGoals_WhenNoAccess_ReturnsFailure() {
        var handler = new GetClientGoalsQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientGoals_WhenDietologistHasNoRelationshipWithClient_ReturnsAccessDenied() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet-no-relationship@example.com"));
        userRepo.Seed(CreateUser(clientId, "client-no-relationship@example.com"));
        var handler = new GetClientGoalsQueryHandler(new InMemoryInvitationRepository(), userRepo);

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task GetClientGoals_WhenGoalsDenied_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var noGoalsPermissions = new DietologistPermissions(ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: false, ShareHydration: true);
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId, noGoalsPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetClientGoalsQueryHandler(invRepo, userRepo);

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientGoals_WithAccess_ReturnsUser() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        User clientUser = CreateUser(clientId, "client@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        userRepo.Seed(clientUser);

        var handler = new GetClientGoalsQueryHandler(invRepo, userRepo);

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        ResultAssert.Success(result);
    }

    [Fact]
    public async Task GetClientGoals_WhenDietologistDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));
        userRepo.Seed(CreateUser(clientId, "client@example.com"));

        var handler = new GetClientGoalsQueryHandler(invRepo, userRepo);

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetClientGoals_WhenClientMissing_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));

        var handler = new GetClientGoalsQueryHandler(invRepo, userRepo);

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }

    // â”€â”€ GetMyRecommendations â”€â”€

    [Fact]
    public async Task GetMyRecommendations_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyRecommendationsQueryHandler(
            new InMemoryRecommendationRepository(), new InMemoryUserRepository());

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetMyRecommendationsQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetMyRecommendations_WithValidUser_ReturnsRecommendations() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var rec1 = Recommendation.Create(UserId.New(), userId, "Rec 1");
        var rec2 = Recommendation.Create(UserId.New(), userId, "Rec 2");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(rec1);
        recRepo.Seed(rec2);

        var handler = new GetMyRecommendationsQueryHandler(recRepo, userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetMyRecommendationsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetMyRecommendations_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new GetMyRecommendationsQueryHandler(
            new InMemoryRecommendationRepository(), userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetMyRecommendationsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    // â”€â”€ GetRecommendationsForClient â”€â”€

    [Fact]
    public async Task GetRecommendationsForClient_WithNullUserId_ReturnsFailure() {
        var handler = new GetRecommendationsForClientQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryRecommendationRepository(), new InMemoryUserRepository());

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetRecommendationsForClientQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetRecommendationsForClient_WhenNoAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetRecommendationsForClientQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryRecommendationRepository(), userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetRecommendationsForClientQuery(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetRecommendationsForClient_WithAccess_ReturnsRecommendations() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var rec = Recommendation.Create(dietologistId, clientId, "Eat well");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(rec);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var handler = new GetRecommendationsForClientQueryHandler(invRepo, recRepo, userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetRecommendationsForClientQuery(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetRecommendationsForClient_WhenDietologistDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        var handler = new GetRecommendationsForClientQueryHandler(
            invRepo, new InMemoryRecommendationRepository(), userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetRecommendationsForClientQuery(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    // â”€â”€ RecommendationCreatedEventHandler â”€â”€

    [Fact]
    public async Task RecommendationCreatedEventHandler_CreatesNotificationAndPushes() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var recId = RecommendationId.New();

        User dietologist = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(dietologist);

        var notifRepo = new InMemoryNotificationRepository();
        var pusher = new FakeNotificationPusher();

        var handler = new RecommendationCreatedEventHandler(
            notifRepo,
            new InMemoryNotificationWriter(notifRepo, new FakeWebPushNotificationSender()),
            pusher,
            userRepo,
            new ImmediatePostCommitActionQueue());
        var domainEvent = new RecommendationCreatedDomainEvent(recId, dietologistId, clientId);

        await handler.Handle(new NotificationEnvelope<RecommendationCreatedDomainEvent>(domainEvent), CancellationToken.None);

        Assert.Single(notifRepo.Added);
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(notifRepo.Added[0].PayloadJson);
        Assert.Equal("diet@example.com", payload?.DietologistName);
        Assert.True(pusher.PushCalled);
    }

    [Fact]
    public async Task RecommendationCreatedEventHandler_WhenDietologistNotFound_UsesEmptyName() {
        var recId = RecommendationId.New();
        var clientId = UserId.New();
        var dietologistId = UserId.New();

        var notifRepo = new InMemoryNotificationRepository();
        var pusher = new FakeNotificationPusher();

        var handler = new RecommendationCreatedEventHandler(
            notifRepo,
            new InMemoryNotificationWriter(notifRepo, new FakeWebPushNotificationSender()),
            pusher,
            new InMemoryUserRepository(),
            new ImmediatePostCommitActionQueue());
        var domainEvent = new RecommendationCreatedDomainEvent(recId, dietologistId, clientId);

        await handler.Handle(new NotificationEnvelope<RecommendationCreatedDomainEvent>(domainEvent), CancellationToken.None);

        Assert.Single(notifRepo.Added);
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(notifRepo.Added[0].PayloadJson);
        Assert.Equal(string.Empty, payload?.DietologistName);
    }

    [Fact]
    public void NotificationTargetUrlResolver_ForRecommendation_ReturnsRecommendationsPage() {
        string recommendationId = Guid.NewGuid().ToString();

        string? targetUrl = NotificationTargetUrlResolver.Resolve(NotificationTypes.NewRecommendation, recommendationId);

        Assert.Equal($"/recommendations?recommendationId={recommendationId}", targetUrl);
    }

    // â”€â”€ Test Doubles â”€â”€

    [Fact]
    public async Task GetMyDietologistRelationship_WithNullUserId_ReturnsFailure() {
        var handler = new GetMyDietologistRelationshipQueryHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result<DietologistRelationshipModel?> result = await handler.Handle(
            new GetMyDietologistRelationshipQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetMyDietologistRelationship_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new GetMyDietologistRelationshipQueryHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result<DietologistRelationshipModel?> result = await handler.Handle(
            new GetMyDietologistRelationshipQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMyDietologistRelationship_WhenPendingInvitationExists_ReturnsRelationship() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(userId));

        DietologistInvitation invitation = CreatePendingInvitation(userId, "diet@example.com");
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new GetMyDietologistRelationshipQueryHandler(invRepo, userRepo);

        Result<DietologistRelationshipModel?> result = await handler.Handle(
            new GetMyDietologistRelationshipQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(invitation.Id.Value, result.Value.InvitationId);
        Assert.Equal(DietologistInvitationStatus.Pending.ToString(), result.Value.Status);
    }

    [Fact]
    public void DietologistMappings_ToDietologistInfoModel_MapsAcceptedInvitation() {
        var dietologistId = UserId.New();
        User dietologist = CreateUser(dietologistId, "diet@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(dietologist, "Dana");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(dietologist, "Smith");

        DietologistInvitation invitation = CreateAcceptedInvitation(UserId.New(), dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistUser))!
            .SetValue(invitation, dietologist);

        var model = invitation.ToDietologistInfoModel();

        Assert.Equal(invitation.Id.Value, model.InvitationId);
        Assert.Equal(dietologistId.Value, model.DietologistUserId);
        Assert.Equal("diet@example.com", model.Email);
        Assert.Equal("Dana", model.FirstName);
        Assert.Equal("Smith", model.LastName);
        Assert.True(model.Permissions.ShareMeals);
    }

    [Fact]
    public void DietologistMappings_ToInvitationModel_MapsClientDetails() {
        var clientId = UserId.New();
        User client = CreateUser(clientId, "client@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(client, "Casey");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(client, "Jones");

        DietologistInvitation invitation = CreatePendingInvitation(clientId, "diet@example.com");
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!
            .SetValue(invitation, client);

        var model = invitation.ToInvitationModel();

        Assert.Equal(invitation.Id.Value, model.InvitationId);
        Assert.Equal("client@example.com", model.ClientEmail);
        Assert.Equal("Casey", model.ClientFirstName);
        Assert.Equal("Jones", model.ClientLastName);
        Assert.Equal(DietologistInvitationStatus.Pending.ToString(), model.Status);
    }

    [Fact]
    public void DietologistModels_CanBeConstructed() {
        var permissions = new DietologistPermissionsModel(ShareMeals: true, ShareStatistics: false, ShareWeight: true, ShareWaist: false, ShareGoals: true, ShareHydration: false, ShareProfile: true, ShareFasting: false);
        DateTime acceptedAt = DateTime.UtcNow;
        DateTime expiresAt = acceptedAt.AddDays(7);

        var info = new DietologistInfoModel(
            Guid.NewGuid(), Guid.NewGuid(), "diet@example.com", "Dana", "Smith", permissions, acceptedAt);
        var invitation = new InvitationModel(
            Guid.NewGuid(), "client@example.com", "Casey", "Jones", "Pending", acceptedAt, expiresAt);

        Assert.Equal("diet@example.com", info.Email);
        Assert.Equal("client@example.com", invitation.ClientEmail);
        Assert.Equal(expiresAt, invitation.ExpiresAtUtc);
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class RecordingUserRoleMembershipService : IUserRoleMembershipService {
        public List<UserId> EnsureRoleUserIds { get; } = [];

        public Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
            if (string.Equals(roleName, RoleNames.Dietologist, StringComparison.Ordinal)) {
                EnsureRoleUserIds.Add(userId);
            }

            return Task.CompletedTask;
        }

        public Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserRepository : IUserRepository, ICurrentUserAccessService, IDietologistUserContextService, IDietologistUserLookupService {
        private readonly List<User> _users = [];
        private readonly List<Role> _roles = [];

        public void Seed(User user) => _users.Add(user);

        public void SeedRoles(IEnumerable<string> roleNames) {
            foreach (string name in roleNames) {
                _roles.Add(Role.Create(name));
            }
        }

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            User? user = _users.FirstOrDefault(u => u.Id == userId);
            Error? error = user switch {
                null => Errors.Authentication.InvalidToken,
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error);
        }

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? user = _users.FirstOrDefault(u => u.Id == userId);
            Error? error = user switch {
                null => Errors.Authentication.InvalidToken,
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error is not null ? Result.Failure<User>(error) : Result.Success(user!));
        }

        public Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken) =>
            GetByEmailAsync(email, cancellationToken);

        public Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken) =>
            GetByIdAsync(userId, cancellationToken);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Role>>(_roles.Where(r => names.Contains(r.Name, StringComparer.Ordinal)).ToList());

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

    [ExcludeFromCodeCoverage]
    private sealed class SequenceUserRepository(params User?[] users) : IUserRepository, IDietologistUserContextService, IDietologistUserLookupService {
        private readonly Queue<User?> _users = new(users);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult(_users.Count > 0 ? _users.Dequeue() : null);

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? user = _users.Count > 0 ? _users.Dequeue() : null;
            Error? error = user switch {
                null => Errors.Authentication.InvalidToken,
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error is not null ? Result.Failure<User>(error) : Result.Success(user!));
        }

        public Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken) =>
            GetByEmailAsync(email, cancellationToken);

        public Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken) =>
            GetByIdAsync(userId, cancellationToken);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Role>>([]);

        public Task<User> AddAsync(User user, CancellationToken ct = default) =>
            Task.FromResult(user);

        public Task UpdateAsync(User user, CancellationToken ct = default) =>
            Task.CompletedTask;

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

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead && string.Equals(n.Type, type, StringComparison.Ordinal)));

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Any(n => n.UserId == userId && string.Equals(n.Type, type, StringComparison.Ordinal) && string.Equals(n.ReferenceId, referenceId, StringComparison.Ordinal)));

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

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryNotificationWriter(
        INotificationRepository notificationRepository,
        IWebPushNotificationSender webPushNotificationSender) : INotificationWriter {
        public async Task AddAsync(
            Notification notification,
            bool sendWebPush = false,
            CancellationToken cancellationToken = default) {
            await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

            if (sendWebPush) {
                await webPushNotificationSender.SendAsync(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubPasswordHasher(bool verifyResult = true) : IPasswordHasher {
        public string Hash(string password) => $"hashed-{password}";
        public bool Verify(string password, string hashedPassword) => verifyResult;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeEmailSender : IDietologistEmailSender {
        public int SentCount { get; private set; }

        public Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken ct = default) {
            SentCount++;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingEmailSender : IDietologistEmailSender {
        public Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken ct = default) =>
            throw new InvalidOperationException("SMTP unavailable");
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(DateTime.UtcNow);
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class ImmediatePostCommitActionQueue : IPostCommitActionQueue {
        public bool HasActions => false;

        public void Enqueue(Func<CancellationToken, Task> action) {
            action(CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeWebPushNotificationSender : IWebPushNotificationSender {
        public bool SendCalled { get; private set; }

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            SendCalled = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingDashboardSnapshotBuilder : IDashboardSnapshotBuilder {
        public Task<Result<DashboardSnapshotModel>> BuildAsync(
            DashboardSnapshotRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingDashboardSnapshotBuilder : IDashboardSnapshotBuilder {
        public Task<Result<DashboardSnapshotModel>> BuildAsync(
            DashboardSnapshotRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<DashboardSnapshotModel>(Errors.Dietologist.AccessDenied));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingDashboardSnapshotBuilder(DashboardSnapshotModel snapshot) : IDashboardSnapshotBuilder {
        public DashboardSnapshotRequest? LastRequest { get; private set; }

        public Task<Result<DashboardSnapshotModel>> BuildAsync(
            DashboardSnapshotRequest request,
            CancellationToken cancellationToken = default) {
            LastRequest = request;
            return Task.FromResult(Result.Success(snapshot));
        }
    }
}
