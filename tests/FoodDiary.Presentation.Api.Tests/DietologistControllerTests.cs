using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dietologist.Commands.RevokeInvitation;
using FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Dietologist;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistControllerTests {
    [Fact]
    public async Task Invite_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new InviteDietologistHttpRequest("diet@example.com", CreatePermissionsRequest());

        IActionResult result = await controller.Invite(userId, request);

        Assert.IsType<NoContentResult>(result);
        InviteDietologistCommand command = Assert.IsType<InviteDietologistCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("diet@example.com", command.DietologistEmail);
    }

    [Fact]
    public async Task RevokeOrDisconnect_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.RevokeOrDisconnect(userId);

        Assert.IsType<NoContentResult>(result);
        RevokeInvitationCommand command = Assert.IsType<RevokeInvitationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task UpdatePermissions_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var permissions = new DietologistPermissionsHttpRequest(
            ShareMeals: true,
            ShareStatistics: false,
            ShareWeight: true,
            ShareWaist: false,
            ShareGoals: true,
            ShareHydration: false,
            ShareProfile: true,
            ShareFasting: false);
        var request = new UpdateDietologistPermissionsHttpRequest(permissions);

        IActionResult result = await controller.UpdatePermissions(userId, request);

        Assert.IsType<NoContentResult>(result);
        UpdateDietologistPermissionsCommand command = Assert.IsType<UpdateDietologistPermissionsCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.True(command.Permissions.ShareMeals);
        Assert.False(command.Permissions.ShareStatistics);
    }

    [Fact]
    public async Task GetMyDietologist_SendsQueryAndReturnsNullableResponse() {
        var invitationId = Guid.NewGuid();
        var dietologistUserId = Guid.NewGuid();
        DateTime acceptedAtUtc = DateTime.UtcNow.AddDays(-1);
        var model = new DietologistInfoModel(
            invitationId,
            dietologistUserId,
            "diet@example.com",
            "Dana",
            "Doc",
            CreatePermissions(),
            acceptedAtUtc);
        IRequest<Result<DietologistInfoModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<DietologistInfoModel?>(model), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetMyDietologist(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DietologistInfoHttpResponse response = Assert.IsType<DietologistInfoHttpResponse>(ok.Value);
        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal(dietologistUserId, response.DietologistUserId);
        GetMyDietologistQuery query = Assert.IsType<GetMyDietologistQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task GetMyDietologist_WhenMissing_ReturnsNullResponse() {
        IRequest<Result<DietologistInfoModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<DietologistInfoModel?>(value: null), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetMyDietologist(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(ok.Value);
        GetMyDietologistQuery query = Assert.IsType<GetMyDietologistQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task GetRelationship_SendsQueryAndReturnsRelationship() {
        var invitationId = Guid.NewGuid();
        var dietologistUserId = Guid.NewGuid();
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-2);
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(5);
        DateTime acceptedAtUtc = DateTime.UtcNow.AddDays(-1);
        var model = new DietologistRelationshipModel(
            invitationId,
            Status: "Accepted",
            Email: "diet@example.com",
            FirstName: "Dana",
            LastName: "Doc",
            dietologistUserId,
            CreatePermissions(),
            createdAtUtc,
            expiresAtUtc,
            acceptedAtUtc);
        IRequest<Result<DietologistRelationshipModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<DietologistRelationshipModel?>(model), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetRelationship(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DietologistRelationshipHttpResponse response = Assert.IsType<DietologistRelationshipHttpResponse>(ok.Value);
        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal("Accepted", response.Status);
        GetMyDietologistRelationshipQuery query = Assert.IsType<GetMyDietologistRelationshipQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task GetRelationship_WhenMissing_ReturnsNullResponse() {
        IRequest<Result<DietologistRelationshipModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<DietologistRelationshipModel?>(value: null), request => sentRequest = request);
        DietologistController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetRelationship(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(ok.Value);
        GetMyDietologistRelationshipQuery query = Assert.IsType<GetMyDietologistRelationshipQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    private static DietologistController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

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

    private static DietologistPermissionsHttpRequest CreatePermissionsRequest() =>
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
