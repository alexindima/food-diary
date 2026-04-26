using FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.MealPlans;

public class MealPlansFeatureTests {
    [Fact]
    public async Task AdoptMealPlan_WhenPlanNotFound_ReturnsFailure() {
        var repo = new StubMealPlanRepository(null);
        var handler = new AdoptMealPlanCommandHandler(repo);

        var result = await handler.Handle(
            new AdoptMealPlanCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AdoptMealPlan_WhenNotCurated_ReturnsFailure() {
        var userId = UserId.New();
        var plan = MealPlan.CreateForUser(userId, "My Plan", null, DietType.Balanced, 7, null);
        var repo = new StubMealPlanRepository(plan);
        var handler = new AdoptMealPlanCommandHandler(repo);

        var result = await handler.Handle(
            new AdoptMealPlanCommand(Guid.NewGuid(), plan.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotCurated", result.Error.Code);
    }

    [Fact]
    public async Task AdoptMealPlan_WithNullUserId_ReturnsFailure() {
        var handler = new AdoptMealPlanCommandHandler(new StubMealPlanRepository(null));

        var result = await handler.Handle(
            new AdoptMealPlanCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private sealed class StubMealPlanRepository(MealPlan? plan) : IMealPlanRepository {
        public Task<MealPlan?> GetByIdAsync(MealPlanId id, bool includeDays = false, CancellationToken ct = default) =>
            Task.FromResult(plan);

        public Task<MealPlan> AddAsync(MealPlan p, CancellationToken ct = default) => Task.FromResult(p);
        public Task<IReadOnlyList<MealPlan>> GetCuratedAsync(DietType? dietType = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<MealPlan>> GetByUserAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
