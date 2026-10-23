using FoodDiary.Modules.Admin.Infrastructure;
using FoodDiary.Modules.Identity.Infrastructure;
using FoodDiary.Modules.Meals.Infrastructure;
using FoodDiary.Modules.Products.Infrastructure;
using FoodDiary.Modules.Recipes.Infrastructure;
using FoodDiary.Modules.Users.Infrastructure;
using System.Reflection;
using AiModuleRegistration = FoodDiary.Modules.Ai.Infrastructure.ModuleRegistration;
using BillingModuleRegistration = FoodDiary.Modules.Billing.Infrastructure.ModuleRegistration;
using BodyMetricsModuleRegistration = FoodDiary.Modules.BodyMetrics.Infrastructure.ModuleRegistration;
using ContentReportsModuleRegistration = FoodDiary.Modules.ContentReports.Infrastructure.ModuleRegistration;
using CyclesModuleRegistration = FoodDiary.Modules.Cycles.Infrastructure.ModuleRegistration;
using DailyAdvicesModuleRegistration = FoodDiary.Modules.DailyAdvices.Infrastructure.ModuleRegistration;
using DietologistModuleRegistration = FoodDiary.Modules.Dietologist.Infrastructure.ModuleRegistration;
using ExercisesModuleRegistration = FoodDiary.Modules.Exercises.Infrastructure.ModuleRegistration;
using FastingModuleRegistration = FoodDiary.Modules.Fasting.Infrastructure.ModuleRegistration;
using FavoritesModuleRegistration = FoodDiary.Modules.Favorites.Infrastructure.ModuleRegistration;
using GamificationModuleRegistration = FoodDiary.Modules.Gamification.Infrastructure.ModuleRegistration;
using HydrationModuleRegistration = FoodDiary.Modules.Hydration.Infrastructure.ModuleRegistration;
using ImagesDependencyInjection = FoodDiary.Modules.Images.Infrastructure.DependencyInjection;
using LessonsModuleRegistration = FoodDiary.Modules.Lessons.Infrastructure.ModuleRegistration;
using MarketingModuleRegistration = FoodDiary.Modules.Marketing.Infrastructure.ModuleRegistration;
using MealPlanningModuleRegistration = FoodDiary.Modules.MealPlanning.Infrastructure.ModuleRegistration;
using NotificationsModuleRegistration = FoodDiary.Modules.Notifications.Infrastructure.ModuleRegistration;
using OpenFoodFactsModuleRegistration = FoodDiary.Modules.OpenFoodFacts.Infrastructure.ModuleRegistration;
using UsdaModuleRegistration = FoodDiary.Modules.Usda.Infrastructure.ModuleRegistration;
using WearablesModuleRegistration = FoodDiary.Modules.Wearables.Infrastructure.ModuleRegistration;
using WeeklyGoalsModuleRegistration = FoodDiary.Modules.WeeklyGoals.Infrastructure.ModuleRegistration;
using FoodDiary.Modules.RecipeCommunity.Infrastructure;
using FoodDiary.Modules.RecentItems.Infrastructure;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
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
            foreach (PropertyInfo property in type.GetProperties().Where(property =>
                         property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))) {
                object? set = property.GetValue(owner);
                Assert.NotNull(set);
                Assert.Same(set, property.GetValue(owner));
                Assert.NotNull(owner.Model.FindEntityType(property.PropertyType.GenericTypeArguments[0]));
            }
            Assert.Equal(type.Name.Equals("UsersDbContext", StringComparison.Ordinal) ? -100 : 100, coordinator.GetSaveOrder(owner));
        }
        Assert.Equal(29, coordinator.ModuleContexts.Count);
    }

    private static void RegisterModules(IServiceCollection services) {
        DailyAdvicesModuleRegistration.AddDailyAdvicesModule(services);
        ContentReportsModuleRegistration.AddContentReportsModule(services);
        WeeklyGoalsModuleRegistration.AddWeeklyGoalsModule(services);
        GamificationModuleRegistration.AddGamificationModule(services);
        BillingModuleRegistration.AddBillingModule(services);
        CyclesModuleRegistration.AddCyclesModule(services);
        BodyMetricsModuleRegistration.AddBodyMetricsModule(services);
        AiModuleRegistration.AddAiPersistence(services);
        FavoritesModuleRegistration.AddFavoritesModule(services);
        ExercisesModuleRegistration.AddExercisesModule(services);
        AdminModuleRegistration.AddAdminPersistence(services);
        services.AddRecipeCommunityModule();
        WearablesModuleRegistration.AddWearablesModule(services);
        ProductsModuleRegistration.AddProductsPersistence(services);
        DietologistModuleRegistration.AddDietologistModule(services);
        FastingModuleRegistration.AddFastingModule(services);
        services.AddRecentItemsModule();
        OpenFoodFactsModuleRegistration.AddOpenFoodFactsModule(services);
        UsdaModuleRegistration.AddUsdaModule(services);
        UsersModuleRegistration.AddUsersPersistence(services);
        MealsModuleRegistration.AddMealsPersistence(services);
        MealPlanningModuleRegistration.AddMealPlanningModule(services);
        NotificationsModuleRegistration.AddNotificationsPersistence(services);
        RecipesModuleRegistration.AddRecipesPersistence(services);
        ImagesDependencyInjection.AddImagesInfrastructure(services);
        MarketingModuleRegistration.AddMarketingModule(services);
        LessonsModuleRegistration.AddLessonsModule(services);
        IdentityModuleRegistration.AddIdentityPersistence(services);
        HydrationModuleRegistration.AddHydrationModule(services);
    }
}
