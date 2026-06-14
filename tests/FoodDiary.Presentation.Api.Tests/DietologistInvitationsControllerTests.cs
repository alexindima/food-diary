using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;
using FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;
using FoodDiary.Presentation.Api.Features.Dietologist;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationsControllerTests {
    [Fact]
    public async Task Accept_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var request = new AcceptInvitationHttpRequest(invitationId, "accept-token");

        IActionResult result = await controller.Accept(userId, request);

        Assert.IsType<NoContentResult>(result);
        AcceptInvitationCommand command = Assert.IsType<AcceptInvitationCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
        Assert.Equal("accept-token", command.Token);
    }

    [Fact]
    public async Task Decline_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var request = new DeclineInvitationHttpRequest(invitationId, "decline-token");

        IActionResult result = await controller.Decline(userId, request);

        Assert.IsType<NoContentResult>(result);
        DeclineInvitationCommand command = Assert.IsType<DeclineInvitationCommand>(sender.Request);
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
        RecordingSender sender = new(Result.Success(model));
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetInvitation(invitationId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        InvitationHttpResponse response = Assert.IsType<InvitationHttpResponse>(ok.Value);
        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal("client@example.com", response.ClientEmail);
        GetInvitationByTokenQuery query = Assert.IsType<GetInvitationByTokenQuery>(sender.Request);
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
        RecordingSender sender = new(Result.Success(model));
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetInvitationForCurrentUser(invitationId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DietologistInvitationForCurrentUserHttpResponse response = Assert.IsType<DietologistInvitationForCurrentUserHttpResponse>(ok.Value);
        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal(clientUserId, response.ClientUserId);
        GetInvitationForCurrentUserQuery query = Assert.IsType<GetInvitationForCurrentUserQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(invitationId, query.InvitationId);
    }

    [Fact]
    public async Task AcceptForCurrentUser_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        IActionResult result = await controller.AcceptForCurrentUser(invitationId, userId);

        Assert.IsType<NoContentResult>(result);
        AcceptInvitationForCurrentUserCommand command = Assert.IsType<AcceptInvitationForCurrentUserCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
    }

    [Fact]
    public async Task DeclineForCurrentUser_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        DietologistInvitationsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        IActionResult result = await controller.DeclineForCurrentUser(invitationId, userId);

        Assert.IsType<NoContentResult>(result);
        DeclineInvitationForCurrentUserCommand command = Assert.IsType<DeclineInvitationForCurrentUserCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(invitationId, command.InvitationId);
    }

    private static DietologistInvitationsController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
