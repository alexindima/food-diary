using FoodDiary.Results;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetClientDashboard;
using FoodDiary.Application.Dietologist.Queries.GetClientGoals;
using FoodDiary.Application.Dietologist.Queries.GetMyClients;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;
using FoodDiary.Application.Users.Models;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using FoodDiary.Presentation.Api.Features.Dietologist;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistClientsControllerTests {
    [Fact]
    public async Task GetMyClients_SendsQueryAndReturnsClients() {
        ClientSummaryModel client = CreateClient();
        IRequest<Result<IReadOnlyList<ClientSummaryModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<ClientSummaryModel>>([client]), request => sentRequest = request);
        DietologistClientsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetMyClients(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<ClientSummaryHttpResponse> response = Assert.IsType<List<ClientSummaryHttpResponse>>(ok.Value);
        Assert.Single(response);
        Assert.Equal(client.UserId, response[0].UserId);
        GetMyClientsQuery query = Assert.IsType<GetMyClientsQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task DisconnectClient_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistClientsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        IActionResult result = await controller.DisconnectClient(clientUserId, userId);

        Assert.IsType<NoContentResult>(result);
        DisconnectDietologistCommand command = Assert.IsType<DisconnectDietologistCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(clientUserId, command.ClientUserId);
    }

    [Fact]
    public async Task GetClientDashboard_SendsQueryAndReturnsDashboard() {
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        DashboardSnapshotModel model = CreateDashboardSnapshot(date);
        IRequest<Result<DashboardSnapshotModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        DietologistClientsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var query = new GetClientDashboardHttpQuery(date, Page: 2, PageSize: 20, Locale: "ru", TrendDays: 14);

        IActionResult result = await controller.GetClientDashboard(clientUserId, userId, query);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DashboardSnapshotHttpResponse response = Assert.IsType<DashboardSnapshotHttpResponse>(ok.Value);
        Assert.Equal(date, response.Date);
        GetClientDashboardQuery sentQuery = Assert.IsType<GetClientDashboardQuery>(sentRequest);
        Assert.Equal(userId, sentQuery.UserId);
        Assert.Equal(clientUserId, sentQuery.ClientUserId);
        Assert.Equal(date, sentQuery.Date);
        Assert.Equal(2, sentQuery.Page);
    }

    [Fact]
    public async Task GetClientGoals_SendsQueryAndReturnsUser() {
        UserModel model = CreateUser();
        IRequest<Result<UserModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        DietologistClientsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        IActionResult result = await controller.GetClientGoals(clientUserId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UserHttpResponse response = Assert.IsType<UserHttpResponse>(ok.Value);
        Assert.Equal(model.Id, response.Id);
        GetClientGoalsQuery query = Assert.IsType<GetClientGoalsQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(clientUserId, query.ClientUserId);
    }

    [Fact]
    public async Task GetRecommendationsForClient_SendsQueryAndReturnsRecommendations() {
        RecommendationModel recommendation = CreateRecommendation();
        IRequest<Result<IReadOnlyList<RecommendationModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<RecommendationModel>>([recommendation]), request => sentRequest = request);
        DietologistClientsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        IActionResult result = await controller.GetRecommendationsForClient(clientUserId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<RecommendationHttpResponse> response = Assert.IsType<List<RecommendationHttpResponse>>(ok.Value);
        Assert.Single(response);
        Assert.Equal(recommendation.Id, response[0].Id);
        GetRecommendationsForClientQuery query = Assert.IsType<GetRecommendationsForClientQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(clientUserId, query.ClientUserId);
    }

    [Fact]
    public async Task CreateRecommendation_SendsCommandAndReturnsCreatedResponse() {
        RecommendationModel recommendation = CreateRecommendation();
        IRequest<Result<RecommendationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(recommendation), request => sentRequest = request);
        DietologistClientsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var request = new CreateRecommendationHttpRequest("Eat more fiber");

        IActionResult result = await controller.CreateRecommendation(clientUserId, userId, request);

        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
        RecommendationHttpResponse response = Assert.IsType<RecommendationHttpResponse>(created.Value);
        Assert.Equal(recommendation.Id, response.Id);
        Assert.Equal(nameof(DietologistClientsController.CreateRecommendation), created.ActionName);
        CreateRecommendationCommand command = Assert.IsType<CreateRecommendationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(clientUserId, command.ClientUserId);
        Assert.Equal("Eat more fiber", command.Text);
    }

    private static DietologistClientsController CreateController(ISender sender) =>
        new(sender, TimeProvider.System) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static ClientSummaryModel CreateClient() =>
        new(
            Guid.NewGuid(),
            "client@example.com",
            "Client",
            "User",
            "https://cdn.example/profile.png",
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Female",
            170,
            "Moderate",
            CreatePermissions(),
            DateTime.UtcNow.AddDays(-5));

    private static RecommendationModel CreateRecommendation() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Dana", "Doc", "Eat more fiber", IsRead: false, DateTime.UtcNow, ReadAtUtc: null);

    private static DashboardSnapshotModel CreateDashboardSnapshot(DateTime date) =>
        new(
            date,
            date.AddDays(1).AddTicks(-1),
            DailyGoal: 2100,
            WeeklyCalorieGoal: 14700,
            new DashboardStatisticsModel(
                TotalCalories: 1800,
                AverageProteins: 120,
                AverageFats: 65,
                AverageCarbs: 180,
                AverageFiber: 25,
                ProteinGoal: 130,
                FatGoal: 70,
                CarbGoal: 200,
                FiberGoal: 30),
            [new DailyCaloriesModel(date, 1800)],
            new DashboardWeightModel(Latest: null, Previous: null, Desired: null),
            new DashboardWaistModel(Latest: null, Previous: null, Desired: null),
            new DashboardMealsModel([], Total: 0),
            Advice: new DailyAdviceModel(Guid.NewGuid(), "ru", "Advice", "tag", 1));

    private static UserModel CreateUser() =>
        new(
            Guid.NewGuid(),
            "client@example.com",
            HasPassword: true,
            Username: "client",
            FirstName: "Client",
            LastName: "User",
            BirthDate: null,
            Gender: null,
            Weight: 70,
            DesiredWeight: 65,
            DesiredWaist: 80,
            Height: 170,
            ActivityLevel: "Moderate",
            DailyCalorieTarget: 2100,
            ProteinTarget: 120,
            FatTarget: 60,
            CarbTarget: 220,
            FiberTarget: 30,
            StepGoal: 8000,
            WaterGoal: 2,
            HydrationGoal: 2,
            Language: "ru",
            Theme: "dark",
            UiStyle: "system",
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: false,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 20,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: null,
            IsActive: true,
            IsEmailConfirmed: true,
            LastLoginAtUtc: null,
            AiConsentAcceptedAt: null);

    private static DietologistPermissionsModel CreatePermissions() =>
        new(
            ShareMeals: true,
            ShareStatistics: false,
            ShareWeight: true,
            ShareWaist: false,
            ShareGoals: true,
            ShareHydration: false,
            ShareProfile: true,
            ShareFasting: false);
}
