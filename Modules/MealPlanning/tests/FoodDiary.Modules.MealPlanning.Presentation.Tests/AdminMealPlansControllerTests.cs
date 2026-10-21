using FoodDiary.Mediator;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlans;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.SearchCatalogRecipes;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Controllers;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Responses;
using FoodDiary.Presentation.Api.Requests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.MealPlanning.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminMealPlansControllerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPage_MapsPagingAndReturnsOrderedSummariesIncludingAnEmptyPage(bool empty) {
        var id = Guid.NewGuid();
        MealPlanSummaryModel[] rows = empty ? [] : [new(id, "Weekly", "Description", "Balanced", 2, 2100, IsCurated: true, 3)];
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<IReadOnlyList<MealPlanSummaryModel>>>>(), cancellation.Token)
            .Returns(Result.Success<IReadOnlyList<MealPlanSummaryModel>>(rows));

        OkObjectResult result = Assert.IsType<OkObjectResult>(await CreateController(sender, cancellation.Token).GetPage(new OffsetPaginationHttpQuery(2, 7)));
        IReadOnlyList<MealPlanSummaryHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<MealPlanSummaryHttpResponse>>(result.Value);

        Assert.Equal(rows.Length, response.Count);
        if (!empty) { Assert.Equal(new MealPlanSummaryHttpResponse(id, "Weekly", "Description", "Balanced", 2, 2100, IsCurated: true, 3), Assert.Single(response)); }
        await sender.Received(1).Send(Arg.Is<IRequest<Result<IReadOnlyList<MealPlanSummaryModel>>>>(request =>
            request.Equals(new GetCatalogMealPlansQuery(2, 7))), cancellation.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  soup  ")]
    public async Task SearchRecipes_ForwardsTheExactSearchAndLimitAndReturnsRecipeIdentity(string? search) {
        var id = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<IReadOnlyList<CatalogRecipeReadModel>>>>(), cancellation.Token)
            .Returns(Result.Success<IReadOnlyList<CatalogRecipeReadModel>>([new(id, "Soup", 3)]));

        OkObjectResult result = Assert.IsType<OkObjectResult>(await CreateController(sender, cancellation.Token).SearchRecipes(search, 11));
        IReadOnlyList<CatalogRecipeHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<CatalogRecipeHttpResponse>>(result.Value);

        Assert.Equal(new CatalogRecipeHttpResponse(id, "Soup", 3), Assert.Single(response));
        await sender.Received(1).Send(Arg.Is<IRequest<Result<IReadOnlyList<CatalogRecipeReadModel>>>>(request =>
            request.Equals(new SearchCatalogRecipesQuery(search, 11))), cancellation.Token);
    }

    [Fact]
    public async Task GetById_MapsTheRouteAndCompleteNestedPlan() {
        MealPlanModel plan = CreatePlan();
        using var cancellation = new CancellationTokenSource();
        ISender sender = CreatePlanSender(plan, cancellation.Token);

        OkObjectResult result = Assert.IsType<OkObjectResult>(await CreateController(sender, cancellation.Token).GetById(plan.Id));

        AssertPlan(plan, Assert.IsType<MealPlanHttpResponse>(result.Value));
        await sender.Received(1).Send(Arg.Is<IRequest<Result<MealPlanModel>>>(request =>
            request.Equals(new GetCatalogMealPlanQuery(plan.Id))), cancellation.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_UsesTheRouteIdentityAndReturnsTheDocumentWithTheCorrectHttpStatus(bool update) {
        MealPlanModel plan = CreatePlan();
        MealPlanMealModel meal = plan.Days[0].Meals[0];
        var request = new SaveCatalogMealPlanHttpRequest("Weekly", "Description", "Balanced", 1, 2100, IsPublished: true,
            [new CatalogDayHttpRequest(1, [new CatalogMealHttpRequest("Lunch", meal.RecipeId, 3)])]);
        using var cancellation = new CancellationTokenSource();
        ISender sender = CreatePlanSender(plan, cancellation.Token);
        AdminMealPlansController controller = CreateController(sender, cancellation.Token);

        IActionResult action = update ? await controller.Update(plan.Id, request) : await controller.Create(request);
        ObjectResult result = update ? Assert.IsType<OkObjectResult>(action) : Assert.IsType<CreatedResult>(action);

        Assert.Equal(update ? StatusCodes.Status200OK : StatusCodes.Status201Created, result.StatusCode);
        AssertPlan(plan, Assert.IsType<MealPlanHttpResponse>(result.Value));
        await sender.Received(1).Send(Arg.Any<IRequest<Result<MealPlanModel>>>(), cancellation.Token);
        SaveCatalogMealPlanCommand command = Assert.IsType<SaveCatalogMealPlanCommand>(Assert.Single(sender.ReceivedCalls()).GetArguments()[0]);
        CatalogDayInput day = Assert.Single(command.Days);
        CatalogMealInput savedMeal = Assert.Single(day.Meals);
        Assert.Multiple(
            () => Assert.Equal(update ? plan.Id : (Guid?)null, command.Id),
            () => Assert.Equal("Weekly", command.Name),
            () => Assert.Equal("Description", command.Description),
            () => Assert.Equal("Balanced", command.DietType),
            () => Assert.Equal(1, command.DurationDays),
            () => Assert.Equal(2100, command.TargetCaloriesPerDay),
            () => Assert.True(command.IsPublished),
            () => Assert.Equal(1, day.DayNumber),
            () => Assert.Equal(new CatalogMealInput("Lunch", meal.RecipeId, 3), savedMeal));
    }

    private static MealPlanModel CreatePlan() => new(Guid.NewGuid(), "Weekly", "Description", "Balanced", 1, 2100, IsCurated: true,
        [new MealPlanDayModel(Guid.NewGuid(), 1, [new MealPlanMealModel(Guid.NewGuid(), "Lunch", Guid.NewGuid(), "Soup", 3, 321.5, 12.25, 8.5, 49.75)])]);

    private static ISender CreatePlanSender(MealPlanModel plan, CancellationToken cancellationToken) {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<MealPlanModel>>>(), cancellationToken).Returns(Result.Success(plan));
        return sender;
    }

    private static void AssertPlan(MealPlanModel expected, MealPlanHttpResponse response) {
        MealPlanDayHttpResponse day = Assert.Single(response.Days);
        MealPlanMealHttpResponse meal = Assert.Single(day.Meals);
        Assert.Multiple(
            () => Assert.Equal(expected.Id, response.Id),
            () => Assert.Equal("Weekly", response.Name),
            () => Assert.Equal("Description", response.Description),
            () => Assert.Equal("Balanced", response.DietType),
            () => Assert.Equal(1, response.DurationDays),
            () => Assert.Equal(2100, response.TargetCaloriesPerDay),
            () => Assert.True(response.IsCurated),
            () => Assert.Equal(expected.Days[0].Id, day.Id),
            () => Assert.Equal(1, day.DayNumber),
            () => Assert.Equal(new MealPlanMealHttpResponse(expected.Days[0].Meals[0].Id, "Lunch", expected.Days[0].Meals[0].RecipeId,
                "Soup", 3, 321.5, 12.25, 8.5, 49.75), meal));
    }

    private static AdminMealPlansController CreateController(ISender sender, CancellationToken cancellationToken) => new(sender) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellationToken } },
    };
}
