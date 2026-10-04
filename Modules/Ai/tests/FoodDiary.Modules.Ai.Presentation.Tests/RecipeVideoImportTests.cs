using System.Reflection;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Presentation.Recipes;
using FoodDiary.Modules.Ai.Presentation.Recipes.Controllers;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Modules.Ai.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoImportTests {
    [Fact]
    public async Task Upload_SendsOpenStreamToApplication_AndReturnsRecipeDraft() {
        var draft = new RecipeImportDraftModel("Salad", Description: null, [new("yoghurt", "180g")], ["Mix"], Servings: null, PrepMinutes: null, CookMinutes: null, AuthorNutrition: null, SourceUrl: null);
        ImportRecipeVideoCommand? command = null;
        byte[]? received = null;
        ISender sender = SubstituteSender.Create(Result.Success(draft), request => {
            command = Assert.IsType<ImportRecipeVideoCommand>(request);
            Assert.NotNull(command.Video);
            using var copied = new MemoryStream();
            command.Video.CopyTo(copied);
            received = copied.ToArray();
        });
        var http = new DefaultHttpContext();
        typeof(IdempotencyRequestContext).GetMethod("SetRequestId", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [http, new string('A', 64)]);
        var controller = new RecipeVideoImportController(sender) { ControllerContext = new ControllerContext { HttpContext = http } };
        await using var original = new MemoryStream([1, 2, 3]);
        var file = new FormFile(original, 0, 3, "video", "user-filename.mp4");
        IActionResult result = await controller.ImportRecipeVideo(Guid.NewGuid(), file, sourceUrl: null, "caption");
        RecipeImportHttpResponse response = Assert.IsType<RecipeImportHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Multiple(() => Assert.Equal("Salad", response.Name), () => Assert.Equal(new byte[] { 1, 2, 3 }, received), () => Assert.Equal("caption", command!.Text));
    }

    [Fact]
    public async Task Admission_RejectsThirdRequestBeforeBinding_AndReleasesSlots() {
        var filter = new RecipeVideoAdmissionAttribute();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int admitted = 0;
        ResourceExecutingContext first = CreateResourceContext();
        ResourceExecutingContext second = CreateResourceContext();
        async Task<ResourceExecutedContext> HoldAsync(ResourceExecutingContext context) {
            if (Interlocked.Increment(ref admitted) == 2) {
                entered.TrySetResult();
            }
            await release.Task;
            return new ResourceExecutedContext(context, []);
        }
        Task firstRequest = filter.OnResourceExecutionAsync(first, () => HoldAsync(first));
        Task secondRequest = filter.OnResourceExecutionAsync(second, () => HoldAsync(second));
        try {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            ResourceExecutingContext third = CreateResourceContext();
            bool bound = false;
            await filter.OnResourceExecutionAsync(third, () => {
                bound = true;
                return Task.FromResult(new ResourceExecutedContext(third, []));
            });
            Assert.Multiple(() => Assert.False(bound), () => Assert.Equal(StatusCodes.Status429TooManyRequests, Assert.IsType<ObjectResult>(third.Result).StatusCode));
        } finally {
            release.TrySetResult();
            await Task.WhenAll(firstRequest, secondRequest);
        }
        ResourceExecutingContext retry = CreateResourceContext();
        await filter.OnResourceExecutionAsync(retry, () => Task.FromResult(new ResourceExecutedContext(retry, [])));
        Assert.Null(retry.Result);
    }

    private static ResourceExecutingContext CreateResourceContext() => new(
        new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), [], []);
}
