using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Queries.ListFoodRecognitions;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionListControllerTests {
    [Fact]
    public async Task List_RequestsOwnedJobsAndMapsEachResult() {
        var owner = Guid.NewGuid();
        var job = new FoodRecognitionJobModel(Guid.NewGuid(), owner, Guid.NewGuid(), "https://example.com/image", Description: null, "Queued", DateTime.UtcNow, DateTime.UtcNow);
        IReadOnlyList<FoodRecognitionJobModel> jobs = [job];
        IRequest<Result<IReadOnlyList<FoodRecognitionJobModel>>>? sent = null;
        ISender sender = SubstituteSender.Create(Result.Success(jobs), request => sent = request);
        var controller = new FoodRecognitionController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.List(owner));

        Assert.Equal(owner, Assert.IsType<ListFoodRecognitionsQuery>(sent).UserId);
        FoodRecognitionJobHttpResponse response = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<FoodRecognitionJobHttpResponse>>(result.Value));
        Assert.Equal(job.Id, response.Id);
        Assert.Equal(job.Status, response.Status);
    }
}
