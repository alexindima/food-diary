using FoodDiary.Modules.Ai.Application.Commands.DeleteFoodRecognition;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions;
using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Modules.Ai.Presentation.Requests;
using FoodDiary.Modules.Ai.Presentation.Controllers;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Ai.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionListControllerTests {
    [Fact]
    public async Task Delete_UsesAuthenticatedOwnerAndReturnsNoContent() {
        var owner = Guid.NewGuid();
        var id = Guid.NewGuid();
        IRequest<Result>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sent = request);
        var controller = new FoodRecognitionController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.IsType<NoContentResult>(await controller.Delete(owner, id));
        DeleteFoodRecognitionCommand command = Assert.IsType<DeleteFoodRecognitionCommand>(sent);
        Assert.Equal(owner, command.UserId);
        Assert.Equal(id, command.Id);
    }

    [Fact]
    public async Task List_RequestsOwnedJobsAndMapsEachResult() {
        var owner = Guid.NewGuid();
        var job = new FoodRecognitionJobModel(Guid.NewGuid(), owner, Guid.NewGuid(), "https://example.com/image", Description: null, "Queued", DateTime.UtcNow, DateTime.UtcNow);
        var jobs = new PagedResponse<FoodRecognitionJobModel>([job], 2, 20, 2, 21);
        IRequest<Result<PagedResponse<FoodRecognitionJobModel>>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(jobs), request => sent = request);
        var controller = new FoodRecognitionController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.List(owner, new ListFoodRecognitionsHttpQuery(2, 20, IsProductLabel: true)));

        Assert.Equal(owner, Assert.IsType<ListFoodRecognitionsQuery>(sent).UserId);
        PagedHttpResponse<FoodRecognitionJobHttpResponse> page = Assert.IsType<PagedHttpResponse<FoodRecognitionJobHttpResponse>>(result.Value);
        Assert.Equal(21, page.TotalItems);
        Assert.Equal(2, page.Page);
        Assert.True(Assert.IsType<ListFoodRecognitionsQuery>(sent).IsProductLabel);
        FoodRecognitionJobHttpResponse response = Assert.Single(page.Data);
        Assert.Equal(job.Id, response.Id);
        Assert.Equal(job.Status, response.Status);
    }
}
