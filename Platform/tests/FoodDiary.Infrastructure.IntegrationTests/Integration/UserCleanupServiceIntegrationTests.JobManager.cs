using FoodDiary.Infrastructure.Persistence;
using FoodDiary.JobManager.Services;
using FoodDiary.Mediator;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [Fact]
    public void MealPlanningPersistence_RegistersPurgeWithoutApplicationHandlers() {
        var services = new ServiceCollection();

        services.AddMealPlanningPersistence();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUserDataPurgeParticipant));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithJobManagerRegistration_RemovesOwnedMealPlansBeforeRecipes() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var deleted = User.Create("job-manager-purge@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        var survivor = User.Create("job-manager-survivor@example.test", "hash");
        var recipe = Recipe.Create(deleted.Id, "Owned planned recipe", servings: 1);
        var survivingRecipe = Recipe.Create(survivor.Id, "Surviving planned recipe", servings: 1);
        var plan = MealPlan.CreateForUser(deleted.Id, "Owned plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Dinner, recipe.Id, servings: 1);
        var survivingPlan = MealPlan.CreateForUser(survivor.Id, "Surviving plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        survivingPlan.AddDay(1).AddMeal(MealType.Dinner, survivingRecipe.Id, servings: 1);
        context.AddRange(deleted, survivor, recipe, survivingRecipe, plan, survivingPlan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var outbox = new RecordingImageObjectDeletionOutbox();
        // The production host method is the fixture's only MealPlanning registration.
        await using ServiceProvider provider = CreateServiceProvider(context, outbox, out UserCleanupService service,
            configureMealPlanning: services => services.AddJobManagerServices(new ConfigurationBuilder().Build()));
        Assert.Equal([7, 10], provider.GetServices<IUserContentRetentionParticipant>().Select(participant => participant.Order).Order());

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.Multiple(
            () => Assert.Equal(1, result.RemovedCount),
            () => Assert.Empty(outbox.ObjectKeys));
        Assert.False(await observer.Users.AnyAsync(user => user.Id == deleted.Id));
        Assert.False(await observer.Recipes.AnyAsync(item => item.Id == recipe.Id));
        Assert.False(await observer.MealPlans.AnyAsync(item => item.Id == plan.Id));
        Assert.True(await observer.Users.AnyAsync(user => user.Id == survivor.Id));
        Assert.True(await observer.Recipes.AnyAsync(item => item.Id == survivingRecipe.Id));
        Assert.True(await observer.MealPlans.AnyAsync(item => item.Id == survivingPlan.Id));
    }
}
