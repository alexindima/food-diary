using System.Security.Claims;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Commands.ImportRecipe;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Presentation.Controllers;
using FoodDiary.Modules.Ai.Presentation.Requests;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Modules.Ai.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeTextImportTests {
    [Fact]
    public async Task TextImport_UsesTheAdmittedRequestIdentityAndMapsTheOwnerDraftWithCancellation() {
        var user = Guid.NewGuid();
        var request = new RecipeImportHttpRequest("https://example.test/recipe", "Mix the ingredients");
        var draft = new RecipeImportDraftModel("Soup", "Text draft", [new("Salt", "a pinch", 0)], ["Mix"],
            2, 3, 4, AuthorNutrition: null, request.SourceUrl);
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ImportRecipeCommand>(), cancellation.Token).Returns(Result.Success(draft));
        var http = new DefaultHttpContext {
            RequestAborted = cancellation.Token,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], "fixture")),
        };
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/api/v1.0/ai/food/recipe-import";
        http.Request.Headers["Idempotency-Key"] = "recipe-text-fixture-key";
        http.Request.ContentType = "application/json";
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request, JsonSerializerOptions.Web);
        await using var body = new MemoryStream(payload);
        http.Request.Body = body;
        http.Request.ContentLength = payload.Length;
        var controller = new AiFoodController(sender) { ControllerContext = new ControllerContext { HttpContext = http } };
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();
        store.ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservation(IdempotencyReservationStatus.Acquired, OwnerToken: "fixture-owner"));
        store.RenewAsync(Arg.Any<string>(), Arg.Any<string>(), "fixture-owner", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        store.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), "fixture-owner", 200, Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        var context = new ActionExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()),
            [new EnableIdempotencyAttribute(requireKey: true)], new Dictionary<string, object?>(StringComparer.Ordinal) { ["request"] = request }, controller);
        ActionExecutedContext? executed = null;

        await new IdempotencyFilter(store).OnActionExecutionAsync(context, async () => {
            executed = new ActionExecutedContext(context, [], controller) { Result = await controller.ImportRecipe(user, request).ConfigureAwait(false) };
            return executed;
        });

        Assert.NotNull(executed);
        string requestId = Assert.IsType<string>(IdempotencyRequestContext.GetRequestId(http));
        ImportRecipeCommand command = Assert.IsType<ImportRecipeCommand>(Assert.Single(sender.ReceivedCalls()).GetArguments()[0]);
        RecipeImportHttpResponse response = Assert.IsType<RecipeImportHttpResponse>(Assert.IsType<OkObjectResult>(executed.Result).Value);
        RecipeImportIngredientHttpResponse ingredient = Assert.Single(response.Ingredients);
        Assert.Multiple(
            () => Assert.Equal(user, command.UserId),
            () => Assert.Equal(request.SourceUrl, command.SourceUrl),
            () => Assert.Equal("Mix the ingredients", command.Text),
            () => Assert.Equal(requestId, command.RequestId),
            () => Assert.Equal("Soup", response.Name),
            () => Assert.Equal("Text draft", response.Description),
            () => Assert.Equal("Salt", ingredient.Name),
            () => Assert.Equal("a pinch", ingredient.Amount),
            () => Assert.Equal(0, ingredient.StepIndex),
            () => Assert.Equal(new[] { "Mix" }, response.Steps),
            () => Assert.Equal(2, response.Servings),
            () => Assert.Equal(3, response.PrepMinutes),
            () => Assert.Equal(4, response.CookMinutes),
            () => Assert.Equal(request.SourceUrl, response.SourceUrl));
        await sender.Received(1).Send(Arg.Any<ImportRecipeCommand>(), cancellation.Token);
        await store.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), "fixture-owner", 200,
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }
}
