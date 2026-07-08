using FoodDiary.Results;
using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;
using FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Queries.GetGamification;
using FoodDiary.Application.Lessons.Commands.MarkLessonRead;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Application.Lessons.Queries.GetLessonById;
using FoodDiary.Application.Lessons.Queries.GetLessons;
using FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Application.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Application.MealPlans.Queries.GetMealPlans;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.FavoriteMeals;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Features.Gamification;
using FoodDiary.Presentation.Api.Features.Gamification.Responses;
using FoodDiary.Presentation.Api.Features.Lessons;
using FoodDiary.Presentation.Api.Features.Lessons.Responses;
using FoodDiary.Presentation.Api.Features.MealPlans;
using FoodDiary.Presentation.Api.Features.MealPlans.Responses;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class MealPlanGamificationLessonControllerTests {
    [Fact]
    public async Task MealPlansController_CoversEndpoints() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        MealPlanModel mealPlan = CreateMealPlan(planId);

        IRequest<Result<IReadOnlyList<MealPlanSummaryModel>>>? allRequest = null;
        ISender allSender = SubstituteSender.Create(Result.Success<IReadOnlyList<MealPlanSummaryModel>>([
            new MealPlanSummaryModel(planId, "Balanced", "Desc", "Balanced", 7, 2100, IsCurated: true, TotalRecipes: 14),
        ]), request => allRequest = request);
        MealPlansController allController = CreateController(new MealPlansController(allSender));
        IActionResult all = await allController.GetAll(userId, "Balanced");
        Assert.IsAssignableFrom<IReadOnlyList<MealPlanSummaryHttpResponse>>(Assert.IsType<OkObjectResult>(all).Value);
        Assert.Equal("Balanced", Assert.IsType<GetMealPlansQuery>(allRequest).DietType);

        IRequest<Result<MealPlanModel>>? byIdRequest = null;
        ISender byIdSender = SubstituteSender.Create(Result.Success(mealPlan), request => byIdRequest = request);
        MealPlansController byIdController = CreateController(new MealPlansController(byIdSender));
        IActionResult byId = await byIdController.GetById(userId, planId);
        Assert.IsType<MealPlanHttpResponse>(Assert.IsType<OkObjectResult>(byId).Value);
        Assert.Equal(planId, Assert.IsType<GetMealPlanByIdQuery>(byIdRequest).PlanId);

        IRequest<Result<MealPlanModel>>? adoptRequest = null;
        ISender adoptSender = SubstituteSender.Create(Result.Success(mealPlan), request => adoptRequest = request);
        MealPlansController adoptController = CreateController(new MealPlansController(adoptSender));
        Assert.IsType<CreatedResult>(await adoptController.Adopt(userId, planId));
        Assert.Equal(planId, Assert.IsType<AdoptMealPlanCommand>(adoptRequest).PlanId);

        IRequest<Result<ShoppingListModel>>? shoppingRequest = null;
        ISender shoppingSender = SubstituteSender.Create(Result.Success(CreateShoppingList()), request => shoppingRequest = request);
        MealPlansController shoppingController = CreateController(new MealPlansController(shoppingSender));
        IActionResult shopping = await shoppingController.GenerateShoppingList(userId, planId);
        Assert.IsType<ShoppingListHttpResponse>(Assert.IsType<CreatedResult>(shopping).Value);
        Assert.Equal(planId, Assert.IsType<GenerateShoppingListCommand>(shoppingRequest).PlanId);
    }

    [Fact]
    public async Task GamificationController_Get_SendsQueryAndReturnsResponse() {
        var userId = Guid.NewGuid();
        var model = new GamificationModel(3, 7, 20, 88, 0.9, [new BadgeModel("streak", "Streak", 3, IsEarned: true)]);
        IRequest<Result<GamificationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        GamificationController controller = CreateController(new GamificationController(sender));

        IActionResult result = await controller.Get(userId);

        Assert.IsType<GamificationHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(userId, Assert.IsType<GetGamificationQuery>(sentRequest).UserId);
    }

    [Fact]
    public async Task FavoriteMealsController_CoversRequestedEndpoints() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var favoriteId = Guid.NewGuid();
        FavoriteMealModel favorite = CreateFavoriteMeal(favoriteId, mealId);

        IRequest<Result<IReadOnlyList<FavoriteMealModel>>>? allRequest = null;
        ISender allSender = SubstituteSender.Create(Result.Success<IReadOnlyList<FavoriteMealModel>>([favorite]), request => allRequest = request);
        FavoriteMealsController allController = CreateController(new FavoriteMealsController(allSender));
        IActionResult all = await allController.GetAll(userId);
        Assert.IsType<List<FavoriteMealHttpResponse>>(Assert.IsType<OkObjectResult>(all).Value);
        Assert.Equal(userId, Assert.IsType<GetFavoriteMealsQuery>(allRequest).UserId);

        IRequest<Result<bool>>? checkRequest = null;
        ISender checkSender = SubstituteSender.Create(Result.Success(value: true), request => checkRequest = request);
        FavoriteMealsController checkController = CreateController(new FavoriteMealsController(checkSender));
        IActionResult check = await checkController.IsFavorite(mealId, userId);
        Assert.True(Assert.IsType<bool>(Assert.IsType<OkObjectResult>(check).Value));
        Assert.Equal(mealId, Assert.IsType<IsMealFavoriteQuery>(checkRequest).MealId);

        IRequest<Result<FavoriteMealModel>>? addRequest = null;
        ISender addSender = SubstituteSender.Create(Result.Success(favorite), request => addRequest = request);
        FavoriteMealsController addController = CreateController(new FavoriteMealsController(addSender));
        IActionResult add = await addController.Add(userId, new AddFavoriteMealHttpRequest(mealId, "Breakfast"));
        Assert.IsType<FavoriteMealHttpResponse>(Assert.IsType<OkObjectResult>(add).Value);
        Assert.Equal(mealId, Assert.IsType<AddFavoriteMealCommand>(addRequest).MealId);

        IRequest<Result>? removeRequest = null;
        ISender removeSender = SubstituteSender.Create(Result.Success(), request => removeRequest = request);
        FavoriteMealsController removeController = CreateController(new FavoriteMealsController(removeSender));
        Assert.IsType<NoContentResult>(await removeController.Remove(favoriteId, userId));
        Assert.Equal(favoriteId, Assert.IsType<RemoveFavoriteMealCommand>(removeRequest).FavoriteMealId);
    }

    [Fact]
    public async Task LessonsController_CoversEndpoints() {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        IRequest<Result<IReadOnlyList<LessonSummaryModel>>>? allRequest = null;
        ISender allSender = SubstituteSender.Create(Result.Success<IReadOnlyList<LessonSummaryModel>>([
            new LessonSummaryModel(lessonId, "Basics", "Summary", "nutrition", "beginner", 5, IsRead: false),
        ]), request => allRequest = request);
        LessonsController allController = CreateController(new LessonsController(allSender));
        IActionResult all = await allController.GetAll(userId, "ru", "nutrition");
        Assert.IsAssignableFrom<IReadOnlyList<LessonSummaryHttpResponse>>(Assert.IsType<OkObjectResult>(all).Value);
        GetLessonsQuery allQuery = Assert.IsType<GetLessonsQuery>(allRequest);
        Assert.Equal("ru", allQuery.Locale);
        Assert.Equal("nutrition", allQuery.Category);

        IRequest<Result<LessonDetailModel>>? byIdRequest = null;
        ISender byIdSender = SubstituteSender.Create(
            Result.Success(new LessonDetailModel(lessonId, "Title", "Content", "Summary", "nutrition", "beginner", 6, IsRead: true)),
            request => byIdRequest = request);
        LessonsController byIdController = CreateController(new LessonsController(byIdSender));
        IActionResult byId = await byIdController.GetById(userId, lessonId);
        Assert.IsType<LessonDetailHttpResponse>(Assert.IsType<OkObjectResult>(byId).Value);
        Assert.Equal(lessonId, Assert.IsType<GetLessonByIdQuery>(byIdRequest).LessonId);

        IRequest<Result>? readRequest = null;
        ISender readSender = SubstituteSender.Create(Result.Success(), request => readRequest = request);
        LessonsController readController = CreateController(new LessonsController(readSender));
        Assert.IsType<NoContentResult>(await readController.MarkRead(userId, lessonId));
        Assert.Equal(lessonId, Assert.IsType<MarkLessonReadCommand>(readRequest).LessonId);
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static MealPlanModel CreateMealPlan(Guid planId) =>
        new(
            planId,
            "Balanced",
            "Desc",
            "Balanced",
            DurationDays: 7,
            TargetCaloriesPerDay: 2100,
            IsCurated: true,
            Days: [new MealPlanDayModel(Guid.NewGuid(), 1, [])]);

    private static ShoppingListModel CreateShoppingList() =>
        new(Guid.NewGuid(), "Weekly", DateTime.UtcNow, []);

    private static FavoriteMealModel CreateFavoriteMeal(Guid favoriteId, Guid mealId) =>
        new(favoriteId, mealId, "Breakfast", DateTime.UtcNow, DateTime.UtcNow.Date, "Breakfast", 450, 30, 15, 55, 3);
}
