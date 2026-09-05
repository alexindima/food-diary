using FoodDiary.Application.Dietologist.Commands.ArchiveRecommendationTemplate;
using FoodDiary.Application.Dietologist.Commands.BulkCreateRecommendations;
using FoodDiary.Application.Dietologist.Commands.CancelClientTask;
using FoodDiary.Application.Dietologist.Commands.ChangeClientTaskStatus;
using FoodDiary.Application.Dietologist.Commands.CreateClientTask;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationTemplate;
using FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;
using FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;
using FoodDiary.Application.Dietologist.Commands.UpdateRecommendationTemplate;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetAttentionSignals;
using FoodDiary.Application.Dietologist.Queries.GetClientTasksForDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyClientTasks;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationComments;
using FoodDiary.Application.Dietologist.Queries.SearchRecommendationTemplates;
using FoodDiary.Domain.Enums;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Dietologist;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable MA0003

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistNewEndpointsCoverageTests {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewDietologistMappings_MapAllRequestAndResponseFields() {
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        var signalRequest = new SetAttentionSignalStateHttpRequest(
            clientId,
            "Snooze",
            UtcNow.AddDays(1));
        var attentionQuery = new GetAttentionSignalsHttpQuery(4, 30, 5, 4, 21);
        SetAttentionSignalStateCommand signalCommand = signalRequest.ToCommand(userId, "signal");
        GetAttentionSignalsQuery query = attentionQuery.ToQuery(userId);
        CreateClientTaskCommand createTask = new CreateClientTaskHttpRequest(
            "Task",
            "Details",
            UtcNow.AddDays(1)).ToCommand(userId, clientId);
        ChangeClientTaskStatusCommand changeTask =
            new ChangeClientTaskStatusHttpRequest("Completed").ToCommand(userId, taskId);
        CreateRecommendationTemplateCommand createTemplate =
            new RecommendationTemplateHttpRequest("Name", "Text").ToCreateTemplateCommand(userId);
        UpdateRecommendationTemplateCommand updateTemplate =
            new RecommendationTemplateHttpRequest("Updated", "Updated text")
                .ToUpdateTemplateCommand(templateId, userId);
        BulkCreateRecommendationsCommand bulk = new BulkCreateRecommendationsHttpRequest(
            [clientId],
            "Recommendation",
            "key").ToCommand(userId);
        CreateRecommendationCommentCommand createComment =
            new CreateRecommendationCommentHttpRequest("Comment").ToCommand(userId, recommendationId);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(4, query.InactivityDays),
            () => Assert.Equal(30, query.CalorieDeviationPercent),
            () => Assert.Equal(5, query.SustainedDays),
            () => Assert.Equal(4, query.WeightChangePercent),
            () => Assert.Equal(21, query.LookbackDays),
            () => Assert.Equal("signal", signalCommand.SignalId),
            () => Assert.Equal(clientId, signalCommand.ClientUserId),
            () => Assert.Equal("Snooze", signalCommand.Action),
            () => Assert.Equal("Task", createTask.Title),
            () => Assert.Equal(clientId, createTask.ClientUserId),
            () => Assert.Equal(taskId, changeTask.TaskId),
            () => Assert.Equal(ClientTaskStatus.Completed.ToString(), changeTask.Status),
            () => Assert.Equal("Name", createTemplate.Name),
            () => Assert.Equal(templateId, updateTemplate.TemplateId),
            () => Assert.Equal("key", bulk.IdempotencyKey),
            () => Assert.Equal("Comment", createComment.Text),
            () => Assert.Equal(recommendationId, createComment.RecommendationId),
            () => Assert.Equal(taskId, taskId.ToCancelClientTaskCommand(userId).TaskId),
            () => Assert.Equal(clientId, clientId.ToClientTasksQuery(userId).ClientUserId),
            () => Assert.Equal(userId, userId.ToMyClientTasksQuery().UserId),
            () => Assert.Equal(templateId, templateId.ToArchiveTemplateCommand(userId).TemplateId),
            () => Assert.Equal("search", userId.ToSearchTemplatesQuery("search", true).Search),
            () => Assert.Equal(recommendationId, recommendationId.ToRecommendationCommentsQuery(userId).RecommendationId));

        object[] responses = [
            new AttentionSignalModel("signal", clientId, "Client", "Type", "High", "Reason", UtcNow, null)
                .ToHttpResponse(),
            CreateTaskModel().ToHttpResponse(),
            new RecommendationCommentModel(
                Guid.NewGuid(), recommendationId, userId, "Author", "Name", "author@example.com", "Text", UtcNow)
                .ToHttpResponse(),
            CreateTemplateModel().ToHttpResponse(),
            CreateBulkModel(clientId).ToHttpResponse(),
        ];
        foreach (object response in responses) {
            foreach (System.Reflection.PropertyInfo property in response.GetType().GetProperties()) {
                _ = property.GetValue(response);
            }
        }

        BulkRecommendationResultHttpResponse bulkResponse =
            Assert.IsType<BulkRecommendationResultHttpResponse>(responses[^1]);
        BulkRecommendationRecipientResultHttpResponse recipient = Assert.Single(bulkResponse.Recipients);
        Assert.Multiple(
            () => Assert.Equal("key", bulkResponse.IdempotencyKey),
            () => Assert.Equal(clientId, recipient.ClientUserId),
            () => Assert.True(recipient.Succeeded),
            () => Assert.NotNull(recipient.RecommendationId),
            () => Assert.False(recipient.WasAlreadyProcessed),
            () => Assert.Null(recipient.ErrorCode));
    }

    [Fact]
    public async Task AttentionController_CoversBothEndpoints() {
        var userId = Guid.NewGuid();
        AttentionSignalModel signal =
            new("signal", Guid.NewGuid(), "Client", "Type", "High", "Reason", UtcNow, null);
        IRequest<Result<IReadOnlyList<AttentionSignalModel>>>? getRequest = null;
        ISender getSender = SubstituteSender.Create(
            Result.Success<IReadOnlyList<AttentionSignalModel>>([signal]),
            request => getRequest = request);
        DietologistAttentionController getController = CreateController(
            new DietologistAttentionController(getSender));

        IActionResult get = await getController.GetAttentionSignals(
            userId,
            new GetAttentionSignalsHttpQuery());

        Assert.IsType<List<AttentionSignalHttpResponse>>(Assert.IsType<OkObjectResult>(get).Value);
        Assert.IsType<GetAttentionSignalsQuery>(getRequest);

        IRequest<Result>? stateRequest = null;
        ISender stateSender = SubstituteSender.Create(Result.Success(), request => stateRequest = request);
        DietologistAttentionController stateController = CreateController(
            new DietologistAttentionController(stateSender));

        IActionResult state = await stateController.SetAttentionSignalState(
            "signal",
            userId,
            new SetAttentionSignalStateHttpRequest(signal.ClientUserId, "Acknowledge", null));

        Assert.IsType<NoContentResult>(state);
        Assert.IsType<SetAttentionSignalStateCommand>(stateRequest);
    }

    [Fact]
    public async Task ClientTaskControllers_CoverAllEndpoints() {
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        ClientTaskModel model = CreateTaskModel();

        IRequest<Result<IReadOnlyList<ClientTaskModel>>>? myRequest = null;
        ClientTasksController myController = CreateController(new ClientTasksController(
            SubstituteSender.Create(
                Result.Success<IReadOnlyList<ClientTaskModel>>([model]),
                request => myRequest = request)));
        IActionResult my = await myController.GetMyTasks(userId);
        Assert.IsType<List<ClientTaskHttpResponse>>(Assert.IsType<OkObjectResult>(my).Value);
        Assert.IsType<GetMyClientTasksQuery>(myRequest);

        IRequest<Result<ClientTaskModel>>? changeRequest = null;
        ClientTasksController changeController = CreateController(new ClientTasksController(
            SubstituteSender.Create(Result.Success(model), request => changeRequest = request)));
        IActionResult change = await changeController.ChangeStatus(
            model.Id,
            userId,
            new ChangeClientTaskStatusHttpRequest("Completed"));
        Assert.IsType<ClientTaskHttpResponse>(Assert.IsType<OkObjectResult>(change).Value);
        Assert.IsType<ChangeClientTaskStatusCommand>(changeRequest);

        IRequest<Result<IReadOnlyList<ClientTaskModel>>>? listRequest = null;
        DietologistClientTasksController listController = CreateController(
            new DietologistClientTasksController(SubstituteSender.Create(
                Result.Success<IReadOnlyList<ClientTaskModel>>([model]),
                request => listRequest = request)));
        IActionResult list = await listController.GetTasksForClient(clientId, userId);
        Assert.IsType<List<ClientTaskHttpResponse>>(Assert.IsType<OkObjectResult>(list).Value);
        Assert.IsType<GetClientTasksForDietologistQuery>(listRequest);

        IRequest<Result<ClientTaskModel>>? createRequest = null;
        DietologistClientTasksController createController = CreateController(
            new DietologistClientTasksController(SubstituteSender.Create(
                Result.Success(model),
                request => createRequest = request)));
        IActionResult create = await createController.CreateTask(
            clientId,
            userId,
            new CreateClientTaskHttpRequest("Task", "Details", UtcNow));
        Assert.IsType<ClientTaskHttpResponse>(Assert.IsType<CreatedResult>(create).Value);
        Assert.IsType<CreateClientTaskCommand>(createRequest);

        IRequest<Result<ClientTaskModel>>? cancelRequest = null;
        DietologistClientTasksController cancelController = CreateController(
            new DietologistClientTasksController(SubstituteSender.Create(
                Result.Success(model),
                request => cancelRequest = request)));
        IActionResult cancel = await cancelController.CancelTask(model.Id, userId);
        Assert.IsType<ClientTaskHttpResponse>(Assert.IsType<OkObjectResult>(cancel).Value);
        Assert.IsType<CancelClientTaskCommand>(cancelRequest);
    }

    [Fact]
    public async Task RecommendationTemplatesController_CoversAllEndpoints() {
        var userId = Guid.NewGuid();
        RecommendationTemplateModel model = CreateTemplateModel();

        IRequest<Result<IReadOnlyList<RecommendationTemplateModel>>>? searchRequest = null;
        RecommendationTemplatesController searchController = CreateController(
            new RecommendationTemplatesController(SubstituteSender.Create(
                Result.Success<IReadOnlyList<RecommendationTemplateModel>>([model]),
                request => searchRequest = request)));
        IActionResult search = await searchController.Search(userId, "name", true);
        Assert.IsType<List<RecommendationTemplateHttpResponse>>(Assert.IsType<OkObjectResult>(search).Value);
        Assert.IsType<SearchRecommendationTemplatesQuery>(searchRequest);

        IRequest<Result<RecommendationTemplateModel>>? createRequest = null;
        RecommendationTemplatesController createController = CreateController(
            new RecommendationTemplatesController(SubstituteSender.Create(
                Result.Success(model),
                request => createRequest = request)));
        IActionResult create = await createController.Create(
            userId,
            new RecommendationTemplateHttpRequest("Name", "Text"));
        Assert.IsType<RecommendationTemplateHttpResponse>(Assert.IsType<CreatedResult>(create).Value);
        Assert.IsType<CreateRecommendationTemplateCommand>(createRequest);

        IRequest<Result<RecommendationTemplateModel>>? updateRequest = null;
        RecommendationTemplatesController updateController = CreateController(
            new RecommendationTemplatesController(SubstituteSender.Create(
                Result.Success(model),
                request => updateRequest = request)));
        IActionResult update = await updateController.Update(
            model.Id,
            userId,
            new RecommendationTemplateHttpRequest("Updated", "Updated text"));
        Assert.IsType<RecommendationTemplateHttpResponse>(Assert.IsType<OkObjectResult>(update).Value);
        Assert.IsType<UpdateRecommendationTemplateCommand>(updateRequest);

        IRequest<Result>? archiveRequest = null;
        RecommendationTemplatesController archiveController = CreateController(
            new RecommendationTemplatesController(
                SubstituteSender.Create(Result.Success(), request => archiveRequest = request)));
        IActionResult archive = await archiveController.Archive(model.Id, userId);
        Assert.IsType<NoContentResult>(archive);
        Assert.IsType<ArchiveRecommendationTemplateCommand>(archiveRequest);
    }

    [Fact]
    public async Task BulkRecommendationsController_CreatesResponse() {
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        BulkRecommendationResultModel model = CreateBulkModel(clientId);
        IRequest<Result<BulkRecommendationResultModel>>? sentRequest = null;
        BulkRecommendationsController controller = CreateController(
            new BulkRecommendationsController(SubstituteSender.Create(
                Result.Success(model),
                request => sentRequest = request)));

        IActionResult result = await controller.Create(
            userId,
            new BulkCreateRecommendationsHttpRequest([clientId], "Text", "key"));

        BulkRecommendationResultHttpResponse response =
            Assert.IsType<BulkRecommendationResultHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(Assert.Single(response.Recipients).Succeeded);
        Assert.IsType<BulkCreateRecommendationsCommand>(sentRequest);
    }

    [Fact]
    public async Task RecommendationsController_CoversCommentEndpoints() {
        var userId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        RecommendationCommentModel comment = new(
            Guid.NewGuid(),
            recommendationId,
            userId,
            "Alex",
            "User",
            "alex@example.com",
            "Comment",
            UtcNow);
        IRequest<Result<IReadOnlyList<RecommendationCommentModel>>>? getRequest = null;
        RecommendationsController getController = CreateController(
            new RecommendationsController(SubstituteSender.Create(
                Result.Success<IReadOnlyList<RecommendationCommentModel>>([comment]),
                request => getRequest = request)));

        IActionResult get = await getController.GetComments(recommendationId, userId);

        Assert.IsType<List<RecommendationCommentHttpResponse>>(Assert.IsType<OkObjectResult>(get).Value);
        Assert.IsType<GetRecommendationCommentsQuery>(getRequest);

        IRequest<Result<RecommendationCommentModel>>? createRequest = null;
        RecommendationsController createController = CreateController(
            new RecommendationsController(SubstituteSender.Create(
                Result.Success(comment),
                request => createRequest = request)));

        IActionResult create = await createController.CreateComment(
            recommendationId,
            userId,
            new CreateRecommendationCommentHttpRequest("Comment"));

        Assert.IsType<RecommendationCommentHttpResponse>(Assert.IsType<CreatedResult>(create).Value);
        Assert.IsType<CreateRecommendationCommentCommand>(createRequest);
    }

    [Fact]
    public async Task RecommendationsController_MarkAsRead_SendsCommandAndReturnsNoContent() {
        var userId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        IRequest<Result>? sentRequest = null;
        RecommendationsController controller = CreateController(
            new RecommendationsController(SubstituteSender.Create(
                Result.Success(),
                request => sentRequest = request)));

        IActionResult result = await controller.MarkAsRead(recommendationId, userId);

        Assert.IsType<NoContentResult>(result);
        MarkRecommendationReadCommand command = Assert.IsType<MarkRecommendationReadCommand>(sentRequest);
        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(recommendationId, command.RecommendationId));
    }

    [Fact]
    public async Task RecommendationsController_GetMyRecommendations_MapsResponse() {
        var userId = Guid.NewGuid();
        RecommendationModel recommendation = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Diet",
            "Ologist",
            "Text",
            false,
            UtcNow,
            null);
        RecommendationsController controller = CreateController(
            new RecommendationsController(SubstituteSender.Create(
                Result.Success<IReadOnlyList<RecommendationModel>>([recommendation]))));

        IActionResult result = await controller.GetMyRecommendations(userId);

        Assert.IsType<List<RecommendationHttpResponse>>(Assert.IsType<OkObjectResult>(result).Value);
    }

    private static T CreateController<T>(T controller) where T : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static ClientTaskModel CreateTaskModel() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Task",
            "Details",
            UtcNow.AddDays(1),
            ClientTaskStatus.Open,
            false,
            UtcNow,
            null);

    private static RecommendationTemplateModel CreateTemplateModel() =>
        new(Guid.NewGuid(), "Name", "Text", false, UtcNow, null);

    private static BulkRecommendationResultModel CreateBulkModel(Guid clientId) =>
        new(
            "key",
            [new BulkRecommendationRecipientResultModel(clientId, true, Guid.NewGuid(), false, null)]);
}
