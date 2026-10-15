using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.MealPlans;

public partial class MealPlansFeatureTests {
    [Theory]
    [InlineData(null, 2)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public async Task GetMealPlans_FiltersSourceAndDietBeforeCountingAndPaging(bool? isCurated, int expectedTotal) {
        var userId = UserId.New();
        var curated = MealPlan.CreateCurated("Curated keto", description: null, DietType.Keto, 1, targetCaloriesPerDay: null);
        var saved = MealPlan.CreateForUser(userId, "Saved keto", description: null, DietType.Keto, 1, targetCaloriesPerDay: null);
        var balanced = MealPlan.CreateForUser(userId, "Saved balanced", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        var repository = new StubMealPlanRepository(curated, curatedPlans: [curated], userPlans: [saved, balanced]);
        GetMealPlansQueryHandler handler = CreateMealPlansHandler(repository);

        Result<PagedResponse<MealPlanSummaryModel>> first = await handler.Handle(new GetMealPlansQuery(userId.Value, "Keto", Page: 1, Limit: 1, IsCurated: isCurated), CancellationToken.None);
        Result<PagedResponse<MealPlanSummaryModel>> second = await handler.Handle(new GetMealPlansQuery(userId.Value, "Keto", Page: 2, Limit: 1, IsCurated: isCurated), CancellationToken.None);

        ResultAssert.Success(first);
        ResultAssert.Success(second);
        MealPlanSummaryModel firstPlan = Assert.Single(first.Value.Data);
        Assert.Multiple(
            () => Assert.Equal(expectedTotal, first.Value.TotalItems),
            () => Assert.Equal(expectedTotal, first.Value.TotalPages),
            () => Assert.Equal(isCurated is false ? saved.Id.Value : curated.Id.Value, firstPlan.Id),
            () => Assert.Equal(expectedTotal, second.Value.TotalItems),
            () => Assert.DoesNotContain(first.Value.Data.Concat(second.Value.Data), plan => plan.Id == balanced.Id.Value));
        if (isCurated.HasValue) {
            Assert.Empty(second.Value.Data);
        } else {
            Assert.Equal(saved.Id.Value, Assert.Single(second.Value.Data).Id);
        }
    }
}
