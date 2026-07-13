using FoodDiary.Results;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
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
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

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

        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler(invRepo, userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Accepted", result.Value.Status);
    }


    [Fact]
    public async Task GetInvitationForCurrentUser_WithNullUserId_ReturnsFailure() {
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler();

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
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler(userContextService: userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task GetInvitationForCurrentUser_WhenInvitationMissingAfterUserAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler(
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
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler(userContextService: userRepo);

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
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler(invRepo, userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }


    [Fact]
    public async Task GetMyDietologist_WithNullUserId_ReturnsFailure() {
        GetMyDietologistQueryHandler handler = CreateGetMyDietologistHandler();

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

        GetMyDietologistQueryHandler handler = CreateGetMyDietologistHandler(currentUserAccessService: userRepo);

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

        GetMyDietologistQueryHandler handler = CreateGetMyDietologistHandler(currentUserAccessService: userRepo);

        Result<DietologistInfoModel?> result = await handler.Handle(
            new GetMyDietologistQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task GetMyDietologist_WithAcceptedInvitation_ReturnsDietologistInfo() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        User client = CreateUser(clientId, "client@example.com");
        User dietologist = CreateUser(dietologistId, "diet@example.com");
        typeof(User).GetProperty(nameof(User.FirstName))!.SetValue(dietologist, "Dana");
        typeof(User).GetProperty(nameof(User.LastName))!.SetValue(dietologist, "Smith");

        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.ClientUser))!.SetValue(invitation, client);
        typeof(DietologistInvitation).GetProperty(nameof(DietologistInvitation.DietologistUser))!.SetValue(invitation, dietologist);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(client);
        GetMyDietologistQueryHandler handler = CreateGetMyDietologistHandler(invRepo, userRepo);

        Result<DietologistInfoModel?> result = await handler.Handle(
            new GetMyDietologistQuery(clientId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(dietologistId.Value, result.Value.DietologistUserId);
        Assert.Equal("diet@example.com", result.Value.Email);
        Assert.Equal("Dana", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
    }


    [Fact]
    public async Task GetMyClients_WithNullUserId_ReturnsFailure() {
        GetMyClientsQueryHandler handler = CreateGetMyClientsHandler();

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

        GetMyClientsQueryHandler handler = CreateGetMyClientsHandler(currentUserAccessService: userRepo);

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

        GetMyClientsQueryHandler handler = CreateGetMyClientsHandler(currentUserAccessService: userRepo);

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

        GetMyClientsQueryHandler handler = CreateGetMyClientsHandler(invRepo, userRepo);

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


    [Fact]
    public async Task GetInvitationByToken_WhenNotFound_ReturnsFailure() {
        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler();

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetInvitationByToken_WithEmptyUserId_ReturnsFailure() {
        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler();

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetInvitationByToken_WithEmptyInvitationId_ReturnsFailure() {
        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler();

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
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

        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler(invRepo, userRepo);

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

        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler(invRepo, userRepo);

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
        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler(invRepo, userRepo);

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

        GetInvitationByTokenQueryHandler handler = CreateGetInvitationByTokenHandler(invRepo, userRepo);

        Result<InvitationModel> result = await handler.Handle(
            new GetInvitationByTokenQuery(dietologistId.Value, invitation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(invitation.Id.Value, result.Value.InvitationId);
        Assert.Equal("client@example.com", result.Value.ClientEmail);
        Assert.Equal(DietologistInvitationStatus.Pending.ToString(), result.Value.Status);
    }


    [Fact]
    public async Task GetClientDashboard_WithNullUserId_ReturnsFailure() {
        GetClientDashboardQueryHandler handler = CreateGetClientDashboardHandler();

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
        GetClientDashboardQueryHandler handler = CreateGetClientDashboardHandler(userRepository: userRepo);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, Guid.NewGuid(), DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetClientDashboard_WhenDietologistAccessFails_ReturnsFailure() {
        var service = new DietologistClientReadService(
            new InMemoryInvitationRepository(),
            new ThrowingDashboardSnapshotBuilder(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository());

        Result<DashboardSnapshotModel> result = await service.GetDashboardAsync(
            UserId.New(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            dateTo: null,
            "en",
            trendDays: 7,
            page: 1,
            pageSize: 10,
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetClientDashboard_WithEmptyClientId_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var service = new DietologistClientReadService(
            new InMemoryInvitationRepository(),
            new ThrowingDashboardSnapshotBuilder(),
            userRepo,
            userRepo);

        Result<DashboardSnapshotModel> result = await service.GetDashboardAsync(
            dietologistId,
            Guid.Empty,
            DateTime.UtcNow,
            dateTo: null,
            "en",
            trendDays: 7,
            page: 1,
            pageSize: 10,
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
        GetClientDashboardQueryHandler handler = CreateGetClientDashboardHandler(invRepo, snapshotBuilder, userRepo);
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
        GetClientDashboardQueryHandler handler = CreateGetClientDashboardHandler(invRepo, userRepository: userRepo);

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

        GetClientDashboardQueryHandler handler = CreateGetClientDashboardHandler(invRepo, userRepository: userRepo);

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

        GetClientDashboardQueryHandler handler = CreateGetClientDashboardHandler(invRepo, new FailingDashboardSnapshotBuilder(), userRepo);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetClientDashboardQuery(dietologistId.Value, clientId.Value, DateTime.UtcNow, DateTo: null, 1, 10, "en", 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }


    [Fact]
    public async Task GetClientGoals_WithNullUserId_ReturnsFailure() {
        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler();

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetClientGoals_WhenNoAccess_ReturnsFailure() {
        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler();

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetClientGoals_WhenDietologistEmailLookupFails_ReturnsFailure() {
        var service = new DietologistClientReadService(
            new InMemoryInvitationRepository(),
            new ThrowingDashboardSnapshotBuilder(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository());

        Result<UserModel> result = await service.GetGoalsAsync(
            UserId.New(),
            Guid.NewGuid(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetClientGoals_WithEmptyClientId_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var service = new DietologistClientReadService(
            new InMemoryInvitationRepository(),
            new ThrowingDashboardSnapshotBuilder(),
            userRepo,
            userRepo);

        Result<UserModel> result = await service.GetGoalsAsync(
            dietologistId,
            Guid.Empty,
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetClientGoals_WhenDietologistHasNoRelationshipWithClient_ReturnsAccessDenied() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet-no-relationship@example.com"));
        userRepo.Seed(CreateUser(clientId, "client-no-relationship@example.com"));
        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler(userRepository: userRepo);

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
        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler(invRepo, userRepo);

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

        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler(invRepo, userRepo);

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

        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler(invRepo, userRepo);

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

        GetClientGoalsQueryHandler handler = CreateGetClientGoalsHandler(invRepo, userRepo);

        Result<UserModel> result = await handler.Handle(
            new GetClientGoalsQuery(dietologistId.Value, clientId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Dietologist.AccessDenied", result.Error.Code);
    }


    [Fact]
    public async Task GetMyRecommendations_WithNullUserId_ReturnsFailure() {
        GetMyRecommendationsQueryHandler handler = CreateGetMyRecommendationsHandler();

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

        GetMyRecommendationsQueryHandler handler = CreateGetMyRecommendationsHandler(recRepo, userRepo);

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

        GetMyRecommendationsQueryHandler handler = CreateGetMyRecommendationsHandler(currentUserAccessService: userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetMyRecommendationsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task GetRecommendationsForClient_WithNullUserId_ReturnsFailure() {
        GetRecommendationsForClientQueryHandler handler = CreateGetRecommendationsForClientHandler();

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetRecommendationsForClientQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetMyRecommendations_WhenCurrentUserAccessFails_ReturnsFailure() {
        var service = new DietologistRecommendationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryRecommendationRepository(),
            new InMemoryUserRepository());

        Result<IReadOnlyList<RecommendationModel>> result = await service.GetForCurrentUserAsync(
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetRecommendationsForClient_WhenDietologistAccessFails_ReturnsFailure() {
        var service = new DietologistRecommendationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryRecommendationRepository(),
            new InMemoryUserRepository());

        Result<IReadOnlyList<RecommendationModel>> result = await service.GetForClientAsync(
            UserId.New(),
            Guid.NewGuid(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetRecommendationsForClient_WithEmptyClientId_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        var service = new DietologistRecommendationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryRecommendationRepository(),
            userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await service.GetForClientAsync(
            dietologistId,
            Guid.Empty,
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetRecommendationsForClient_WhenNoAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        GetRecommendationsForClientQueryHandler handler = CreateGetRecommendationsForClientHandler(currentUserAccessService: userRepo);

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
        GetRecommendationsForClientQueryHandler handler = CreateGetRecommendationsForClientHandler(invRepo, recRepo, userRepo);

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

        GetRecommendationsForClientQueryHandler handler = CreateGetRecommendationsForClientHandler(invRepo, currentUserAccessService: userRepo);

        Result<IReadOnlyList<RecommendationModel>> result = await handler.Handle(
            new GetRecommendationsForClientQuery(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task GetMyDietologistRelationship_WithNullUserId_ReturnsFailure() {
        GetMyDietologistRelationshipQueryHandler handler = CreateGetMyDietologistRelationshipHandler();

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

        GetMyDietologistRelationshipQueryHandler handler = CreateGetMyDietologistRelationshipHandler(currentUserAccessService: userRepo);

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

        GetMyDietologistRelationshipQueryHandler handler = CreateGetMyDietologistRelationshipHandler(invRepo, userRepo);

        Result<DietologistRelationshipModel?> result = await handler.Handle(
            new GetMyDietologistRelationshipQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(invitation.Id.Value, result.Value.InvitationId);
        Assert.Equal(DietologistInvitationStatus.Pending.ToString(), result.Value.Status);
    }


    [Fact]
    public async Task GetInvitationForCurrentUser_WhenEmailLookupFails_ReturnsFailure() {
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler();

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(UserId.New().Value, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetInvitationForCurrentUser_WithEmptyInvitationId_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(userId, "diet@example.com"));
        GetInvitationForCurrentUserQueryHandler handler = CreateGetInvitationForCurrentUserHandler(
            userContextService: userRepo);

        Result<DietologistInvitationForCurrentUserModel> result = await handler.Handle(
            new GetInvitationForCurrentUserQuery(userId.Value, Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetMyDietologist_WhenCurrentUserAccessFails_ReturnsFailure() {
        var service = new DietologistInvitationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository(),
            TimeProvider.System);

        Result<DietologistInfoModel?> result = await service.GetMyDietologistAsync(
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetMyClients_WhenCurrentUserAccessFails_ReturnsFailure() {
        var service = new DietologistInvitationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository(),
            TimeProvider.System);

        Result<IReadOnlyList<ClientSummaryModel>> result = await service.GetMyClientsAsync(
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task GetMyRelationship_WhenCurrentUserAccessFails_ReturnsFailure() {
        var service = new DietologistInvitationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository(),
            TimeProvider.System);

        Result<DietologistRelationshipModel?> result = await service.GetMyRelationshipAsync(
            UserId.New(),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task ProfileDietologistReadService_WhenRelationshipExists_MapsConsumerOwnedProjection() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        var invitationRepository = new InMemoryInvitationRepository();
        invitationRepository.Seed(CreateAcceptedInvitation(clientId, dietologistId));
        var users = new InMemoryUserRepository();
        users.Seed(CreateUser(clientId, "client-profile@example.com"));
        var service = new DietologistInvitationReadService(
            invitationRepository,
            users,
            users,
            TimeProvider.System);

        Result<ProfileDietologistRelationshipModel?> result = await ((IProfileDietologistReadService)service)
            .GetRelationshipAsync(clientId, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Multiple(
            () => Assert.Equal(dietologistId.Value, result.Value.DietologistUserId),
            () => Assert.True(result.Value.Permissions.ShareMeals),
            () => Assert.True(result.Value.Permissions.ShareFasting));
    }

    [Fact]
    public async Task ProfileDietologistReadService_WhenAccessFails_PropagatesFailure() {
        var service = new DietologistInvitationReadService(
            new InMemoryInvitationRepository(),
            new InMemoryUserRepository(),
            new InMemoryUserRepository(),
            TimeProvider.System);

        Result<ProfileDietologistRelationshipModel?> result = await ((IProfileDietologistReadService)service)
            .GetRelationshipAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
    }

}
