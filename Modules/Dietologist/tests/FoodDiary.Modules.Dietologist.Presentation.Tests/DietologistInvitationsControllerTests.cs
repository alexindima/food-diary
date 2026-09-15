using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitation;
using FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitation;
using FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationByToken;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationForCurrentUser;
using FoodDiary.Mediator;
using FoodDiary.Modules.Dietologist.Presentation.Controllers;
using FoodDiary.Modules.Dietologist.Presentation.Requests;
using FoodDiary.Modules.Dietologist.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Dietologist.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationsControllerTests {
    [Fact]
    public async Task Accept_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var request = new AcceptInvitationHttpRequest(invitationId, "accept-token");

        IActionResult result = await controller.Accept(userId, request);

        Assert.IsType<NoContentResult>(result);
        AcceptInvitationCommand command = Assert.IsType<AcceptInvitationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
        Assert.Equal("accept-token", command.Token);
    }

    [Fact]
    public async Task Decline_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var request = new DeclineInvitationHttpRequest(invitationId, "decline-token");

        IActionResult result = await controller.Decline(userId, request);

        Assert.IsType<NoContentResult>(result);
        DeclineInvitationCommand command = Assert.IsType<DeclineInvitationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
        Assert.Equal("decline-token", command.Token);
    }

    [Fact]
    public async Task GetInvitation_SendsQueryAndReturnsInvitation() {
        var invitationId = Guid.NewGuid();
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(6);
        var model = new InvitationModel(invitationId, "client@example.com", "Client", "User", "Pending", createdAtUtc, expiresAtUtc);
        IRequest<Result<InvitationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetInvitation(invitationId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        InvitationHttpResponse response = Assert.IsType<InvitationHttpResponse>(ok.Value);
        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal("client@example.com", response.ClientEmail);
        GetInvitationByTokenQuery query = Assert.IsType<GetInvitationByTokenQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(invitationId, query.InvitationId);
    }

    [Fact]
    public async Task GetInvitationForCurrentUser_SendsQueryAndReturnsInvitation() {
        var invitationId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(6);
        var model = new DietologistInvitationForCurrentUserModel(
            invitationId,
            clientUserId,
            "client@example.com",
            "Client",
            "User",
            "Pending",
            createdAtUtc,
            expiresAtUtc);
        IRequest<Result<DietologistInvitationForCurrentUserModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetInvitationForCurrentUser(invitationId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DietologistInvitationForCurrentUserHttpResponse response = Assert.IsType<DietologistInvitationForCurrentUserHttpResponse>(ok.Value);
        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal(clientUserId, response.ClientUserId);
        GetInvitationForCurrentUserQuery query = Assert.IsType<GetInvitationForCurrentUserQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(invitationId, query.InvitationId);
    }

    [Fact]
    public async Task AcceptForCurrentUser_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        IActionResult result = await controller.AcceptForCurrentUser(invitationId, userId);

        Assert.IsType<NoContentResult>(result);
        AcceptInvitationForCurrentUserCommand command = Assert.IsType<AcceptInvitationForCurrentUserCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
    }

    [Fact]
    public async Task DeclineForCurrentUser_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        IActionResult result = await controller.DeclineForCurrentUser(invitationId, userId);

        Assert.IsType<NoContentResult>(result);
        DeclineInvitationForCurrentUserCommand command = Assert.IsType<DeclineInvitationForCurrentUserCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
    }

    private static DietologistInvitationsController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
