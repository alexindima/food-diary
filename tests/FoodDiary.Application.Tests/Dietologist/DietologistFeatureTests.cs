using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Dietologist.Common;
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
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public partial class DietologistFeatureTests {
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

    [Fact]
    public async Task DietologistUserContextService_GetUserEmailAndModelById_ReturnsLookupResults() {
        var user = User.Create("dietologist-context-model@example.com", "hash");
        IDietologistUserLookupService userLookupService = Substitute.For<IDietologistUserLookupService>();
        userLookupService.GetUserByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        userLookupService.GetUserByIdAsync(Arg.Is<UserId>(id => id != user.Id), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        var service = new DietologistUserContextService(userContextService, userLookupService);

        string? email = await service.GetUserEmailByIdAsync(user.Id, CancellationToken.None);
        Result<UserModel> model = await service.GetUserModelByIdAsync(user.Id, CancellationToken.None);
        Result<UserModel> missing = await service.GetUserModelByIdAsync(UserId.New(), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(user.Email, email),
            () => ResultAssert.Success(model),
            () => Assert.Equal(user.Email, model.Value.Email),
            () => ResultAssert.Failure(missing, "Dietologist.AccessDenied"));
    }

    [Fact]
    public async Task DietologistUserLookupService_GetUserByIdAsync_ReturnsRepositoryUser() {
        User user = CreateUser(UserId.New(), "lookup@example.com");
        IUserLookupRepository userRepository = Substitute.For<IUserLookupRepository>();
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        var service = new DietologistUserLookupService(userRepository);

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
            new NotificationClientRefreshService(
                resolvedNotificationRepository,
                notificationPusher ?? new FakeNotificationPusher()),
            new ImmediatePostCommitActionQueue(),
            dateTimeProvider ?? new StubDateTimeProvider());
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
            new NotificationClientRefreshService(
                resolvedNotificationRepository,
                notificationPusher ?? new FakeNotificationPusher()),
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
            new NotificationClientRefreshService(
                resolvedNotificationRepository,
                notificationPusher ?? new FakeNotificationPusher()),
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
            new NotificationClientRefreshService(
                resolvedNotificationRepository,
                notificationPusher ?? new FakeNotificationPusher()),
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
            new NotificationClientRefreshService(
                resolvedNotificationRepository,
                notificationPusher ?? new FakeNotificationPusher()),
            new ImmediatePostCommitActionQueue());
    }

    private static CreateRecommendationCommandHandler CreateRecommendationHandler(
        IDietologistInvitationRepository? invitationRepository = null,
        IRecommendationRepository? recommendationRepository = null,
        IDietologistUserContextService? userRepository = null) {
        return new(
            invitationRepository ?? new InMemoryInvitationRepository(),
            recommendationRepository ?? new InMemoryRecommendationRepository(),
            userRepository ?? new InMemoryUserRepository());
    }

    // InviteDietologist

    private static GetInvitationForCurrentUserQueryHandler CreateGetInvitationForCurrentUserHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        IDietologistUserContextService? userContextService = null) {
        IDietologistUserContextService resolvedUserContextService = userContextService ?? new InMemoryUserRepository();
        return new(
            CreateDietologistInvitationReadService(
                invitationRepository ?? new InMemoryInvitationRepository(),
                resolvedUserContextService,
                Substitute.For<ICurrentUserAccessService>()),
            resolvedUserContextService);
    }

    private static GetInvitationByTokenQueryHandler CreateGetInvitationByTokenHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        IDietologistUserContextService? userContextService = null) {
        IDietologistUserContextService resolvedUserContextService = userContextService ?? new InMemoryUserRepository();
        return new(
            CreateDietologistInvitationReadService(
                invitationRepository ?? new InMemoryInvitationRepository(),
                resolvedUserContextService,
                Substitute.For<ICurrentUserAccessService>()),
            resolvedUserContextService);
    }

    private static GetMyDietologistQueryHandler CreateGetMyDietologistHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            CreateDietologistInvitationReadService(
            invitationRepository ?? new InMemoryInvitationRepository(),
            Substitute.For<IDietologistUserContextService>(),
            currentUserAccessService ?? new InMemoryUserRepository()),
            currentUserAccessService ?? new InMemoryUserRepository());

    private static GetMyClientsQueryHandler CreateGetMyClientsHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            CreateDietologistInvitationReadService(
            invitationRepository ?? new InMemoryInvitationRepository(),
            Substitute.For<IDietologistUserContextService>(),
            currentUserAccessService ?? new InMemoryUserRepository()),
            currentUserAccessService ?? new InMemoryUserRepository());

    private static GetMyDietologistRelationshipQueryHandler CreateGetMyDietologistRelationshipHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            CreateDietologistInvitationReadService(
            invitationRepository ?? new InMemoryInvitationRepository(),
            Substitute.For<IDietologistUserContextService>(),
            currentUserAccessService ?? new InMemoryUserRepository()),
            currentUserAccessService ?? new InMemoryUserRepository());

    private static IDietologistInvitationReadService CreateDietologistInvitationReadService(
        IDietologistInvitationReadModelRepository invitationRepository,
        IDietologistUserContextService userContextService,
        ICurrentUserAccessService currentUserAccessService) =>
        new DietologistInvitationReadService(invitationRepository, userContextService, currentUserAccessService, TimeProvider.System);

    private static GetClientDashboardQueryHandler CreateGetClientDashboardHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        IDashboardSnapshotBuilder? snapshotBuilder = null,
        InMemoryUserRepository? userRepository = null) =>
        new(
            CreateDietologistClientReadService(
            invitationRepository ?? new InMemoryInvitationRepository(),
            snapshotBuilder ?? new ThrowingDashboardSnapshotBuilder(),
            userRepository ?? new InMemoryUserRepository()),
            Substitute.For<IAuditEntryWriter>(),
            Substitute.For<IUnitOfWork>(),
            userRepository ?? new InMemoryUserRepository());

    private static GetClientGoalsQueryHandler CreateGetClientGoalsHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        InMemoryUserRepository? userRepository = null) =>
        new(
            CreateDietologistClientReadService(
            invitationRepository ?? new InMemoryInvitationRepository(),
            new ThrowingDashboardSnapshotBuilder(),
            userRepository ?? new InMemoryUserRepository()),
            userRepository ?? new InMemoryUserRepository());

    private static IDietologistClientReadService CreateDietologistClientReadService(
        IDietologistInvitationReadModelRepository invitationRepository,
        IDashboardSnapshotBuilder snapshotBuilder,
        InMemoryUserRepository userRepository) =>
        new DietologistClientReadService(invitationRepository, snapshotBuilder, userRepository, userRepository);

    private static GetMyRecommendationsQueryHandler CreateGetMyRecommendationsHandler(
        IRecommendationReadModelRepository? recommendationRepository = null,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            CreateDietologistRecommendationReadService(
            new InMemoryInvitationRepository(),
            recommendationRepository ?? new InMemoryRecommendationRepository(),
            currentUserAccessService ?? new InMemoryUserRepository()),
            currentUserAccessService ?? new InMemoryUserRepository());

    private static GetRecommendationsForClientQueryHandler CreateGetRecommendationsForClientHandler(
        IDietologistInvitationReadModelRepository? invitationRepository = null,
        IRecommendationReadModelRepository? recommendationRepository = null,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            CreateDietologistRecommendationReadService(
            invitationRepository ?? new InMemoryInvitationRepository(),
            recommendationRepository ?? new InMemoryRecommendationRepository(),
            currentUserAccessService ?? new InMemoryUserRepository()),
            currentUserAccessService ?? new InMemoryUserRepository());

    private static IDietologistRecommendationReadService CreateDietologistRecommendationReadService(
        IDietologistInvitationReadModelRepository invitationRepository,
        IRecommendationReadModelRepository recommendationRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new DietologistRecommendationReadService(invitationRepository, recommendationRepository, currentUserAccessService);

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ AcceptInvitation ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ DeclineInvitation ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ RevokeInvitation ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ DisconnectDietologist ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ UpdateDietologistPermissions ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ CreateRecommendation ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ MarkRecommendationRead ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetMyDietologist ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetMyClients ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetInvitationByToken ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetClientDashboard ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetClientGoals ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetMyRecommendations ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ GetRecommendationsForClient ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ RecommendationCreatedEventHandler ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    // ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ Test Doubles ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬

    [Fact]
    public async Task DietologistInvitationReadService_GetForCurrentUser_WhenEmailLookupFails_ReturnsFailure() {
        var service = new DietologistInvitationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository(),
            TimeProvider.System);

        Result<DietologistInvitationForCurrentUserModel> result = await service.GetForCurrentUserAsync(
            UserId.New(),
            Guid.NewGuid(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DietologistInvitationReadService_GetByToken_WithEmptyInvitationId_ReturnsFailure() {
        var service = new DietologistInvitationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository(),
            TimeProvider.System);

        Result<InvitationModel> result = await service.GetByTokenAsync(
            UserId.New(),
            Guid.Empty,
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryInvitationRepository : IDietologistInvitationRepository {
        private readonly List<DietologistInvitation> _invitations = [];
        public List<DietologistInvitation> Added { get; } = [];

        public void Seed(DietologistInvitation invitation) => _invitations.Add(invitation);

        public Task<DietologistInvitationReadModel?> GetByIdReadModelAsync(
            DietologistInvitationId id,
            CancellationToken ct = default) =>
            Task.FromResult(ToReadModel(_invitations.FirstOrDefault(i => i.Id == id)));

        public Task<DietologistInvitation?> GetByIdAsync(
            DietologistInvitationId id,
            bool asTracking = false,
            CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(i => i.Id == id));

        public Task<DietologistInvitationReadModel?> GetByClientAndStatusReadModelAsync(
            UserId clientUserId, DietologistInvitationStatus status, CancellationToken ct = default) =>
            Task.FromResult(ToReadModel(_invitations.FirstOrDefault(i => i.ClientUserId == clientUserId && i.Status == status)));

        public Task<DietologistInvitation?> GetByClientAndStatusAsync(
            UserId clientUserId, DietologistInvitationStatus status, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(i => i.ClientUserId == clientUserId && i.Status == status));

        public Task<DietologistInvitationReadModel?> GetActiveByClientReadModelAsync(
            UserId clientUserId, CancellationToken ct = default) =>
            Task.FromResult(ToReadModel(_invitations.FirstOrDefault(
                i => i.ClientUserId == clientUserId && i.Status == DietologistInvitationStatus.Accepted)));

        public Task<DietologistInvitation?> GetActiveByClientAsync(
            UserId clientUserId, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(
                i => i.ClientUserId == clientUserId && i.Status == DietologistInvitationStatus.Accepted));

        public Task<DietologistInvitationReadModel?> GetActiveByClientAndDietologistReadModelAsync(
            UserId clientUserId, UserId dietologistUserId, CancellationToken ct = default) =>
            Task.FromResult(ToReadModel(_invitations.FirstOrDefault(
                i => i.ClientUserId == clientUserId && i.DietologistUserId == dietologistUserId
                     && i.Status == DietologistInvitationStatus.Accepted)));

        public Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
            UserId clientUserId, UserId dietologistUserId, CancellationToken ct = default) =>
            Task.FromResult(_invitations.FirstOrDefault(
                i => i.ClientUserId == clientUserId && i.DietologistUserId == dietologistUserId
                     && i.Status == DietologistInvitationStatus.Accepted));

        public Task<IReadOnlyList<DietologistInvitationReadModel>> GetActiveByDietologistReadModelsAsync(
            UserId dietologistUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DietologistInvitationReadModel>>(
                _invitations.Where(i => i.DietologistUserId == dietologistUserId
                                        && i.Status == DietologistInvitationStatus.Accepted)
                    .Select(static invitation => ToReadModel(invitation)!)
                    .ToList());

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

        private static DietologistInvitationReadModel? ToReadModel(DietologistInvitation? invitation) =>
            invitation is null
                ? null
                : new(
                invitation.Id.Value,
                invitation.ClientUserId.Value,
                invitation.DietologistUserId?.Value,
                invitation.DietologistEmail,
                invitation.ClientUser?.Email ?? "client@example.com",
                invitation.ClientUser?.FirstName,
                invitation.ClientUser?.LastName,
                invitation.ClientUser?.ProfileImage,
                invitation.ClientUser?.BirthDate,
                invitation.ClientUser?.Gender,
                invitation.ClientUser?.Height,
                invitation.ClientUser?.ActivityLevel ?? ActivityLevel.Moderate,
                invitation.DietologistUser?.Email,
                invitation.DietologistUser?.FirstName,
                invitation.DietologistUser?.LastName,
                invitation.Status,
                new DietologistPermissionsReadModel(
                    invitation.ShareMeals,
                    invitation.ShareStatistics,
                    invitation.ShareWeight,
                    invitation.ShareWaist,
                    invitation.ShareGoals,
                    invitation.ShareHydration,
                    invitation.ShareProfile,
                    invitation.ShareFasting),
                invitation.CreatedOnUtc,
                invitation.ExpiresAtUtc,
                invitation.AcceptedAtUtc);
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
    private static IDietologistUserContextService CreateAccessCheckedFailingDietologistUserContext(UserId userId) {
        IDietologistUserContextService userContextService = Substitute.For<IDietologistUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        return userContextService;
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

        public async Task<Result<string>> GetAccessibleUserEmailAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return userResult.IsFailure
                ? Result.Failure<string>(userResult.Error)
                : Result.Success(userResult.Value.Email);
        }

        public async Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken) {
            User? user = await GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            return user?.Email;
        }

        public async Task<Result<UserModel>> GetUserModelByIdAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            User? user = await GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            return user is null
                ? Result.Failure<UserModel>(Errors.Dietologist.AccessDenied)
                : Result.Success(user.ToModel());
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

        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            User? user = _users.Count > 0 ? _users.Peek() : null;
            Error? error = user switch {
                null => Errors.Authentication.InvalidToken,
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error);
        }

        public async Task<Result<string>> GetAccessibleUserEmailAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return userResult.IsFailure
                ? Result.Failure<string>(userResult.Error)
                : Result.Success(userResult.Value.Email);
        }

        public async Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken) {
            User? user = await GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            return user?.Email;
        }

        public async Task<Result<UserModel>> GetUserModelByIdAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            User? user = await GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            return user is null
                ? Result.Failure<UserModel>(Errors.Dietologist.AccessDenied)
                : Result.Success(user.ToModel());
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

        public Task<IReadOnlyList<RecommendationReadModel>> GetByClientReadModelsAsync(
            UserId clientUserId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RecommendationReadModel>>(
                _recommendations.Where(r => r.ClientUserId == clientUserId)
                    .Take(limit)
                    .Select(ToReadModel)
                    .ToList());

        public Task<IReadOnlyList<Recommendation>> GetByClientAsync(
            UserId clientUserId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Recommendation>>(
                _recommendations.Where(r => r.ClientUserId == clientUserId).Take(limit).ToList());

        public Task<IReadOnlyList<RecommendationReadModel>> GetByDietologistAndClientReadModelsAsync(
            UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RecommendationReadModel>>(
                _recommendations.Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
                    .Take(limit)
                    .Select(ToReadModel)
                    .ToList());

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

        private static RecommendationReadModel ToReadModel(Recommendation recommendation) =>
            new(
                recommendation.Id.Value,
                recommendation.DietologistUserId.Value,
                recommendation.DietologistUser?.FirstName,
                recommendation.DietologistUser?.LastName,
                recommendation.Text,
                recommendation.IsRead,
                recommendation.CreatedOnUtc,
                recommendation.ReadAtUtc);
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

        public Task<IReadOnlyList<NotificationReadModel>> GetByUserReadModelsAsync(UserId userId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<NotificationReadModel>>([.. _notifications
                .Where(notification => notification.UserId == userId)
                .Take(limit)
                .Select(notification => new NotificationReadModel(
                    notification.Id.Value,
                    notification.Type,
                    notification.ReferenceId,
                    notification.PayloadJson,
                    notification.IsRead,
                    notification.CreatedOnUtc))]);
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

        public void Enqueue(string actionName, Func<CancellationToken, Task> action) {
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
