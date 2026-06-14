using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.ContentReports.Commands.CreateContentReport;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.ContentReports;
using FoodDiary.Presentation.Api.Features.ContentReports.Requests;
using FoodDiary.Presentation.Api.Features.ContentReports.Responses;
using FoodDiary.Presentation.Api.Features.RecipeLikes;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportRecipeLikeControllerTests {
    [Fact]
    public async Task ContentReportsController_Create_SendsCommandAndReturnsCreatedReport() {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var model = new ContentReportModel(
            Guid.NewGuid(),
            userId,
            "Recipe",
            targetId,
            "Spam",
            "Pending",
            AdminNote: null,
            DateTime.UtcNow,
            ReviewedAtUtc: null);
        IRequest<Result<ContentReportModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        ContentReportsController controller = CreateController(new ContentReportsController(sender));

        IActionResult result = await controller.Create(userId, new CreateContentReportHttpRequest("Recipe", targetId, "Spam"));

        Assert.IsType<ContentReportHttpResponse>(Assert.IsType<CreatedResult>(result).Value);
        CreateContentReportCommand command = Assert.IsType<CreateContentReportCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(targetId, command.TargetId);
        Assert.Equal("Recipe", command.TargetType);
        Assert.Equal("Spam", command.Reason);
    }

    [Fact]
    public async Task RecipeLikesController_Toggle_SendsCommandAndReturnsStatus() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        IRequest<Result<RecipeLikeStatusModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new RecipeLikeStatusModel(IsLiked: true, TotalLikes: 12)),
            request => sentRequest = request);
        RecipeLikesController controller = CreateController(new RecipeLikesController(sender));

        IActionResult result = await controller.Toggle(userId, recipeId);

        RecipeLikeStatusHttpResponse response = Assert.IsType<RecipeLikeStatusHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(response.IsLiked);
        ToggleRecipeLikeCommand command = Assert.IsType<ToggleRecipeLikeCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
    }

    [Fact]
    public async Task RecipeLikesController_GetStatus_SendsQueryAndReturnsStatus() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        IRequest<Result<RecipeLikeStatusModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new RecipeLikeStatusModel(IsLiked: false, TotalLikes: 3)),
            request => sentRequest = request);
        RecipeLikesController controller = CreateController(new RecipeLikesController(sender));

        IActionResult result = await controller.GetStatus(userId, recipeId);

        RecipeLikeStatusHttpResponse response = Assert.IsType<RecipeLikeStatusHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.False(response.IsLiked);
        GetRecipeLikeStatusQuery query = Assert.IsType<GetRecipeLikeStatusQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(recipeId, query.RecipeId);
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }
}
