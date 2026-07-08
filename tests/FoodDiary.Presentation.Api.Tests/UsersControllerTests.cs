using FoodDiary.Results;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Commands.UpdateUserAppearance;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Users;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsersControllerTests {
    [Fact]
    public async Task GetCurrentUserInfo_SendsUserQueryAndReturnsUser() {
        UserModel model = CreateUserModel();
        IRequest<Result<UserModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        UsersController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetCurrentUserInfo(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UserHttpResponse response = Assert.IsType<UserHttpResponse>(ok.Value);
        Assert.Equal(model.Id, response.Id);
        GetUserByIdQuery query = Assert.IsType<GetUserByIdQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task UpdateCurrentUser_SendsUpdateCommandAndReturnsUser() {
        UserModel model = CreateUserModel();
        IRequest<Result<UserModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        UsersController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new UpdateUserHttpRequest(
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            BirthDate: null,
            Gender: null,
            Weight: 82,
            Height: 181,
            ActivityLevel: "Moderate",
            StepGoal: 9000,
            HydrationGoal: 2.7,
            Language: "en",
            Theme: "light",
            UiStyle: "compact",
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: true,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: null,
            IsActive: true);

        IActionResult result = await controller.UpdateCurrentUser(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UserHttpResponse response = Assert.IsType<UserHttpResponse>(ok.Value);
        Assert.Equal(model.Id, response.Id);
        UpdateUserCommand command = Assert.IsType<UpdateUserCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("alex", command.Username);
        Assert.Equal("light", command.Theme);
    }

    [Fact]
    public async Task UpdateAppearance_SendsAppearanceCommandAndReturnsUser() {
        UserModel model = CreateUserModel();
        IRequest<Result<UserModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        UsersController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new UpdateUserAppearanceHttpRequest("dark", "dense");

        IActionResult result = await controller.UpdateAppearance(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UserHttpResponse response = Assert.IsType<UserHttpResponse>(ok.Value);
        Assert.Equal(model.Id, response.Id);
        UpdateUserAppearanceCommand command = Assert.IsType<UpdateUserAppearanceCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("dark", command.Theme);
        Assert.Equal("dense", command.UiStyle);
    }

    [Fact]
    public async Task DesiredWeightEndpoints_SendQueriesAndCommands() {
        var userId = Guid.NewGuid();
        IRequest<Result<UserDesiredWeightModel>>? getSentRequest = null;
        ISender getSender = SubstituteSender.Create(Result.Success(new UserDesiredWeightModel(76.5)), request => getSentRequest = request);
        UsersController getController = CreateController(getSender);

        IActionResult getResult = await getController.GetDesiredWeight(userId);

        OkObjectResult getOk = Assert.IsType<OkObjectResult>(getResult);
        UserDesiredWeightHttpResponse getResponse = Assert.IsType<UserDesiredWeightHttpResponse>(getOk.Value);
        Assert.Equal(76.5, getResponse.DesiredWeight);
        GetDesiredWeightQuery getQuery = Assert.IsType<GetDesiredWeightQuery>(getSentRequest);
        Assert.Equal(userId, getQuery.UserId);

        IRequest<Result<UserDesiredWeightModel>>? updateSentRequest = null;
        ISender updateSender = SubstituteSender.Create(Result.Success(new UserDesiredWeightModel(75)), request => updateSentRequest = request);
        UsersController updateController = CreateController(updateSender);
        var request = new UpdateDesiredWeightHttpRequest(75);

        IActionResult updateResult = await updateController.UpdateDesiredWeight(userId, request);

        OkObjectResult updateOk = Assert.IsType<OkObjectResult>(updateResult);
        UserDesiredWeightHttpResponse updateResponse = Assert.IsType<UserDesiredWeightHttpResponse>(updateOk.Value);
        Assert.Equal(75, updateResponse.DesiredWeight);
        UpdateDesiredWeightCommand command = Assert.IsType<UpdateDesiredWeightCommand>(updateSentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(75, command.DesiredWeight);
    }

    [Fact]
    public async Task DesiredWaistEndpoints_SendQueriesAndCommands() {
        var userId = Guid.NewGuid();
        IRequest<Result<UserDesiredWaistModel>>? getSentRequest = null;
        ISender getSender = SubstituteSender.Create(Result.Success(new UserDesiredWaistModel(84)), request => getSentRequest = request);
        UsersController getController = CreateController(getSender);

        IActionResult getResult = await getController.GetDesiredWaist(userId);

        OkObjectResult getOk = Assert.IsType<OkObjectResult>(getResult);
        UserDesiredWaistHttpResponse getResponse = Assert.IsType<UserDesiredWaistHttpResponse>(getOk.Value);
        Assert.Equal(84, getResponse.DesiredWaist);
        GetDesiredWaistQuery getQuery = Assert.IsType<GetDesiredWaistQuery>(getSentRequest);
        Assert.Equal(userId, getQuery.UserId);

        IRequest<Result<UserDesiredWaistModel>>? updateSentRequest = null;
        ISender updateSender = SubstituteSender.Create(Result.Success(new UserDesiredWaistModel(82)), request => updateSentRequest = request);
        UsersController updateController = CreateController(updateSender);
        var request = new UpdateDesiredWaistHttpRequest(82);

        IActionResult updateResult = await updateController.UpdateDesiredWaist(userId, request);

        OkObjectResult updateOk = Assert.IsType<OkObjectResult>(updateResult);
        UserDesiredWaistHttpResponse updateResponse = Assert.IsType<UserDesiredWaistHttpResponse>(updateOk.Value);
        Assert.Equal(82, updateResponse.DesiredWaist);
        UpdateDesiredWaistCommand command = Assert.IsType<UpdateDesiredWaistCommand>(updateSentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(82, command.DesiredWaist);
    }

    [Fact]
    public async Task DeleteCurrentUser_SendsDeleteCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        UsersController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.DeleteCurrentUser(userId);

        Assert.IsType<NoContentResult>(result);
        DeleteUserCommand command = Assert.IsType<DeleteUserCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    private static UsersController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static UserModel CreateUserModel() =>
        new(
            Guid.NewGuid(),
            "alex@example.com",
            HasPassword: true,
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            BirthDate: null,
            Gender: null,
            Weight: null,
            DesiredWeight: null,
            DesiredWaist: null,
            Height: null,
            ActivityLevel: "Moderate",
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            StepGoal: null,
            WaterGoal: null,
            HydrationGoal: null,
            Language: "en",
            Theme: "light",
            UiStyle: "default",
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 8,
            FastingCheckInFollowUpReminderHours: 2,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: null,
            IsActive: true,
            IsEmailConfirmed: true,
            LastLoginAtUtc: null,
            AiConsentAcceptedAt: null);
}
