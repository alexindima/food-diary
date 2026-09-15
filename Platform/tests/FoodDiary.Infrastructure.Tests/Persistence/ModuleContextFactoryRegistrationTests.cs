using FoodDiary.Modules.RecipeCommunity.Infrastructure;
using FoodDiary.Modules.RecentItems.Infrastructure;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class ModuleContextFactoryRegistrationTests {
    [Fact]
    public void AllOwnerContexts_UseSameScopedCoordinatorAndConnection() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=factory_test;Username=test;Password=test",
            }).Build();
        services.AddInfrastructure(configuration).AddOutboxProcessing(configuration).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        RegisterModules(services);
        Type[] ownerTypes = [.. services.Select(service => service.ServiceType)
            .Where(type => typeof(DbContext).IsAssignableFrom(type) && type != typeof(FoodDiaryDbContext) && type != typeof(SharedPersistenceDbContext)).Distinct()];
        Assert.Equal(29, ownerTypes.Length);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        using IServiceScope otherScope = provider.CreateScope();
        SharedPersistenceDbContext coordinator = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        Assert.Same(coordinator.Session, scope.ServiceProvider.GetRequiredService<IModuleContextFactory>());
        Assert.NotSame(coordinator.Session, otherScope.ServiceProvider.GetRequiredService<IModuleContextFactory>());
        IModuleTransactionCoordinator transactions = scope.ServiceProvider.GetRequiredService<IModuleTransactionCoordinator>();
        Assert.Same(transactions, scope.ServiceProvider.GetRequiredService<IModuleTransactionCoordinator>());
        Assert.NotSame(transactions, otherScope.ServiceProvider.GetRequiredService<IModuleTransactionCoordinator>());
        Assert.Null(transactions.CurrentTransaction);
        foreach (Type type in ownerTypes) {
            var owner = (DbContext)scope.ServiceProvider.GetRequiredService(type);
            var otherOwner = (DbContext)otherScope.ServiceProvider.GetRequiredService(type);
            Assert.Same(owner, scope.ServiceProvider.GetRequiredService(type));
            Assert.NotSame(owner, otherOwner);
            Assert.Same(coordinator.Database.GetDbConnection(), owner.Database.GetDbConnection());
            Assert.NotSame(owner.Database.GetDbConnection(), otherOwner.Database.GetDbConnection());
            Assert.Contains(owner, coordinator.ModuleContexts);
            Assert.Equal(type.Name.Equals("UsersDbContext", StringComparison.Ordinal) ? -100 : 100, coordinator.GetSaveOrder(owner));
        }
        Assert.Equal(29, coordinator.ModuleContexts.Count);
    }

    private static void RegisterModules(IServiceCollection services) {
        global::FoodDiary.Modules.DailyAdvices.Infrastructure.ModuleRegistration.AddDailyAdvicesModule(services);
        global::FoodDiary.Modules.ContentReports.Infrastructure.ModuleRegistration.AddContentReportsModule(services);
        global::FoodDiary.Modules.WeeklyGoals.Infrastructure.ModuleRegistration.AddWeeklyGoalsModule(services);
        global::FoodDiary.Modules.Gamification.Infrastructure.ModuleRegistration.AddGamificationModule(services);
        global::FoodDiary.Modules.Billing.Infrastructure.ModuleRegistration.AddBillingModule(services);
        global::FoodDiary.Modules.Cycles.Infrastructure.ModuleRegistration.AddCyclesModule(services);
        global::FoodDiary.Modules.BodyMetrics.Infrastructure.ModuleRegistration.AddBodyMetricsModule(services);
        global::FoodDiary.Modules.Ai.Infrastructure.ModuleRegistration.AddAiPersistence(services);
        global::FoodDiary.Modules.Favorites.Infrastructure.ModuleRegistration.AddFavoritesModule(services);
        global::FoodDiary.Modules.Exercises.Infrastructure.ModuleRegistration.AddExercisesModule(services);
        global::FoodDiary.Modules.Admin.Infrastructure.AdminModuleRegistration.AddAdminPersistence(services);
        services.AddRecipeCommunityModule();
        global::FoodDiary.Modules.Wearables.Infrastructure.ModuleRegistration.AddWearablesModule(services);
        global::FoodDiary.Modules.Products.Infrastructure.ProductsModuleRegistration.AddProductsPersistence(services);
        global::FoodDiary.Modules.Dietologist.Infrastructure.ModuleRegistration.AddDietologistModule(services);
        global::FoodDiary.Modules.Fasting.Infrastructure.ModuleRegistration.AddFastingModule(services);
        services.AddRecentItemsModule();
        global::FoodDiary.Modules.OpenFoodFacts.Infrastructure.ModuleRegistration.AddOpenFoodFactsModule(services);
        global::FoodDiary.Modules.Usda.Infrastructure.ModuleRegistration.AddUsdaModule(services);
        global::FoodDiary.Modules.Users.Infrastructure.UsersModuleRegistration.AddUsersPersistence(services);
        global::FoodDiary.Modules.Meals.Infrastructure.MealsModuleRegistration.AddMealsPersistence(services);
        global::FoodDiary.Modules.MealPlanning.Infrastructure.ModuleRegistration.AddMealPlanningModule(services);
        global::FoodDiary.Modules.Notifications.Infrastructure.ModuleRegistration.AddNotificationsPersistence(services);
        global::FoodDiary.Modules.Recipes.Infrastructure.RecipesModuleRegistration.AddRecipesPersistence(services);
        global::FoodDiary.Modules.Images.Infrastructure.DependencyInjection.AddImagesInfrastructure(services);
        global::FoodDiary.Modules.Marketing.Infrastructure.ModuleRegistration.AddMarketingModule(services);
        global::FoodDiary.Modules.Lessons.Infrastructure.ModuleRegistration.AddLessonsModule(services);
        global::FoodDiary.Modules.Identity.Infrastructure.IdentityModuleRegistration.AddIdentityPersistence(services);
        global::FoodDiary.Modules.Hydration.Infrastructure.ModuleRegistration.AddHydrationModule(services);
    }
}
