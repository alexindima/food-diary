using FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Application.Queries.GetAiPromptRevisions;
using FoodDiary.Modules.Ai.Application.Queries.GetAiPromptTemplates;
using FoodDiary.Modules.Ai.Application.Queries.GetAiUsageForUser;
using FoodDiary.Modules.Ai.Application.Queries.GetAiUsageSummary;
using FoodDiary.Modules.Ai.Application.Queries.GetCompletedFoodRecognition;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageForUser;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageSummary;
using FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadLatestWaistEntry;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadLatestWeightEntry;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadLatestWaistEntry;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport;
using FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport;
using FoodDiary.Modules.ContentReports.Application.Queries.CountContentReports;
using FoodDiary.Modules.ContentReports.Application.Queries.GetContentReportsForAdministration;
using FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport;
using FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport;
using FoodDiary.Modules.ContentReports.Contracts.Queries.CountContentReports;
using FoodDiary.Modules.ContentReports.Contracts.Queries.GetContentReportsForAdministration;
using FoodDiary.Modules.Cycles.Application.Queries.GetCurrentCycle;
using FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle;
using FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseCalories;
using FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseEntries;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries;
using FoodDiary.Modules.Fasting.Application.Commands.CleanupFastingTelemetry;
using FoodDiary.Modules.Fasting.Application.Commands.SendFastingNotifications;
using FoodDiary.Modules.Fasting.Application.Queries.ReadCurrentFasting;
using FoodDiary.Modules.Fasting.Application.Queries.ReadFastingInsights;
using FoodDiary.Modules.Fasting.Application.Queries.ReadFastingOverview;
using FoodDiary.Modules.Fasting.Contracts.Commands.CleanupFastingTelemetry;
using FoodDiary.Modules.Fasting.Contracts.Commands.SendFastingNotifications;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingInsights;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingOverview;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadFavoriteMeals;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteIds;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteStatus;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoritesOverview;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadFavoriteProducts;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadProductFavoriteStatus;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadFavoriteRecipes;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadFavoriteMeals;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteStatus;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadProductFavoriteStatus;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipes;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus;
using FoodDiary.Modules.Gamification.Application.Commands.CreateAchievementDefinition;
using FoodDiary.Modules.Gamification.Application.Commands.ReconcileAchievements;
using FoodDiary.Modules.Gamification.Application.Commands.UpdateAchievementDefinition;
using FoodDiary.Modules.Gamification.Application.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
using FoodDiary.Modules.Gamification.Contracts.Commands.UpdateAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotal;
using FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotals;
using FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationEntries;
using FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationInterval;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationEntries;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupLoginEvents;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginDeviceSummary;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginEvents;
using FoodDiary.Modules.Identity.Application.Email.Commands.UpsertEmailTemplate;
using FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupLoginEvents;
using FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginDeviceSummary;
using FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginEvents;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.UpsertEmailTemplate;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates;
using FoodDiary.Modules.Images.Application.Commands.CleanupOrphanImages;
using FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;
using FoodDiary.Modules.Lessons.Application.Commands.CreateLesson;
using FoodDiary.Modules.Lessons.Application.Commands.DeleteLesson;
using FoodDiary.Modules.Lessons.Application.Commands.ImportLessons;
using FoodDiary.Modules.Lessons.Application.Commands.UpdateLesson;
using FoodDiary.Modules.Lessons.Application.Queries.GetLessonsForAdministration;
using FoodDiary.Modules.Lessons.Contracts.Commands.CreateLesson;
using FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson;
using FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons;
using FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson;
using FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration;
using FoodDiary.Modules.Marketing.Application.Commands.CleanupMarketingAttribution;
using FoodDiary.Modules.Marketing.Application.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Marketing.Contracts.Commands.CleanupMarketingAttribution;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Meals.Application.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.Meals.Application.Queries.ReadMealCount;
using FoodDiary.Modules.Meals.Application.Queries.ReadMealsForExport;
using FoodDiary.Modules.Meals.Application.Queries.ReadTotalMealCount;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealsForExport;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount;
using FoodDiary.Modules.Notifications.Application.Commands.CleanupExpiredNotifications;
using FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications;
using FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchProducts;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts;
using FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentProducts;
using FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentRecipes;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes;
using FoodDiary.Modules.Usda.Application.Queries.SearchUsdaFoods;
using FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods;
using FoodDiary.Modules.Users.Application.Commands.CleanupDeletedUsers;
using FoodDiary.Modules.Users.Application.Commands.CreateUserByAdministrator;
using FoodDiary.Modules.Users.Application.Commands.EnsureUserPremiumRole;
using FoodDiary.Modules.Users.Application.Commands.RemoveUserPremiumRole;
using FoodDiary.Modules.Users.Application.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Modules.Users.Application.Commands.StartUserPremiumTrial;
using FoodDiary.Modules.Users.Application.Commands.UpdateUserByAdministrator;
using FoodDiary.Modules.Users.Application.Queries.CheckUserAccess;
using FoodDiary.Modules.Users.Application.Queries.GetFilteredUsersForAdministration;
using FoodDiary.Modules.Users.Application.Queries.GetUserAdministrationSummary;
using FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Application.Queries.GetUserForAdministration;
using FoodDiary.Modules.Users.Application.Queries.GetUsersForAdministration;
using FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers;
using FoodDiary.Modules.Users.Contracts.Commands.CreateUserByAdministrator;
using FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial;
using FoodDiary.Modules.Users.Contracts.Commands.UpdateUserByAdministrator;
using FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;
using FoodDiary.Modules.Users.Contracts.Queries.GetFilteredUsersForAdministration;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserAdministrationSummary;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserForAdministration;
using FoodDiary.Modules.Users.Contracts.Queries.GetUsersForAdministration;
using FoodDiary.Modules.WeeklyGoals.Application.Commands.SendWeeklyGoalReminders;
using FoodDiary.Modules.WeeklyGoals.Contracts.Commands.SendWeeklyGoalReminders;
using System.Reflection;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Mediator;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CrossModuleRequestBoundaryTests {
    [Fact]
    public void UsersCleanup_ExposesRequestInsteadOfInfrastructurePort() {
        var contracts = Assembly.Load("FoodDiary.Modules.Users.Contracts");
        Assert.DoesNotContain(contracts.GetExportedTypes(), type => string.Equals(type.Name, "IUserCleanupService", StringComparison.Ordinal));
        string job = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.JobManager", "Services", "UserCleanupJob.cs"));
        Assert.Contains("ISender sender", job, StringComparison.Ordinal);
        Assert.Contains("new CleanupDeletedUsersCommand(", job, StringComparison.Ordinal);
        Assert.DoesNotContain("IUserCleanupService", job, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Ai")]
    [InlineData("Billing")]
    [InlineData("BodyMetrics")]
    [InlineData("Cycles")]
    [InlineData("ContentReports")]
    [InlineData("DailyAdvices")]
    [InlineData("Dashboard")]
    [InlineData("Exercises")]
    [InlineData("Fasting")]
    [InlineData("Hydration")]
    [InlineData("Lessons")]
    [InlineData("Marketing")]
    [InlineData("OpenFoodFacts")]
    public void MigratedContracts_DoNotExportServiceInterfaces(string module) {
        var contracts = Assembly.Load($"FoodDiary.Modules.{module}.Contracts");
        Assert.DoesNotContain(contracts.GetExportedTypes(), type => type.IsInterface);
    }

    [Fact]
    public void RecentItemsContracts_ExportOnlyPostCommitRecorderPort() {
        var contracts = Assembly.Load("FoodDiary.Modules.RecentItems.Contracts");
        string[] interfaces = [.. contracts.GetExportedTypes().Where(type => type.IsInterface)
            .Select(type => type.Name).Order(StringComparer.Ordinal)];
        Assert.Equal(["IRecentItemUsageRecorder"], interfaces);
    }

    [Fact]
    public void FavoritesContracts_ExportOnlyOutboundSourcePorts() {
        var contracts = Assembly.Load("FoodDiary.Modules.Favorites.Contracts");
        string[] interfaces = [.. contracts.GetExportedTypes().Where(type => type.IsInterface)
            .Select(type => type.Name).Order(StringComparer.Ordinal)];
        Assert.Equal(["IFavoriteMealSourceReadService", "IFavoriteProductSourceReadService", "IFavoriteRecipeSourceReadService"], interfaces);
    }

    [Theory]
    [InlineData(typeof(SearchUsdaFoodsQuery), typeof(SearchUsdaFoodsQueryHandler))]
    [InlineData(typeof(SendWeeklyGoalRemindersCommand), typeof(SendWeeklyGoalRemindersCommandHandler))]
    [InlineData(typeof(ReadRecentProductsQuery), typeof(ReadRecentProductsQueryHandler))]
    [InlineData(typeof(ReadRecentRecipesQuery), typeof(ReadRecentRecipesQueryHandler))]
    [InlineData(typeof(ReadHydrationEntriesQuery), typeof(ReadHydrationEntriesQueryHandler))]
    [InlineData(typeof(ReadHydrationDailyTotalQuery), typeof(ReadHydrationDailyTotalQueryHandler))]
    [InlineData(typeof(ReadHydrationDailyTotalsQuery), typeof(ReadHydrationDailyTotalsQueryHandler))]
    [InlineData(typeof(ReadHydrationIntervalQuery), typeof(ReadHydrationIntervalQueryHandler))]
    [InlineData(typeof(CleanupLoginEventsCommand), typeof(CleanupLoginEventsCommandHandler))]
    [InlineData(typeof(BootstrapInitialAdminCommand), typeof(BootstrapInitialAdminCommandHandler))]
    [InlineData(typeof(CleanupOrphanImagesCommand), typeof(CleanupOrphanImagesCommandHandler))]
    [InlineData(typeof(ReconcileAchievementsCommand), typeof(ReconcileAchievementsCommandHandler))]
    [InlineData(typeof(ReadWeightEntriesQuery), typeof(ReadWeightEntriesQueryHandler))]
    [InlineData(typeof(ReadLatestWeightEntryQuery), typeof(ReadLatestWeightEntryQueryHandler))]
    [InlineData(typeof(ReadWeightSummariesQuery), typeof(ReadWeightSummariesQueryHandler))]
    [InlineData(typeof(ReadWaistEntriesQuery), typeof(ReadWaistEntriesQueryHandler))]
    [InlineData(typeof(ReadLatestWaistEntryQuery), typeof(ReadLatestWaistEntryQueryHandler))]
    [InlineData(typeof(ReadWaistSummariesQuery), typeof(ReadWaistSummariesQueryHandler))]
    [InlineData(typeof(GetAiPromptRevisionsQuery), typeof(GetAiPromptRevisionsQueryHandler))]
    [InlineData(typeof(GetAiUsageForUserQuery), typeof(GetAiUsageForUserQueryHandler))]
    [InlineData(typeof(GetAiUsageSummaryQuery), typeof(GetAiUsageSummaryQueryHandler))]
    [InlineData(typeof(GetAiPromptTemplatesQuery), typeof(GetAiPromptTemplatesQueryHandler))]
    [InlineData(typeof(UpsertAiPromptCommand), typeof(UpsertAiPromptCommandHandler))]
    [InlineData(typeof(GetCompletedFoodRecognitionQuery), typeof(GetCompletedFoodRecognitionQueryHandler))]
    [InlineData(typeof(CreateLessonCommand), typeof(CreateLessonCommandHandler))]
    [InlineData(typeof(UpdateLessonCommand), typeof(UpdateLessonCommandHandler))]
    [InlineData(typeof(DeleteLessonCommand), typeof(DeleteLessonCommandHandler))]
    [InlineData(typeof(ImportLessonsCommand), typeof(ImportLessonsCommandHandler))]
    [InlineData(typeof(GetLessonsForAdministrationQuery), typeof(GetLessonsForAdministrationQueryHandler))]
    [InlineData(typeof(GetAchievementDefinitionsForAdministrationQuery), typeof(GetAchievementDefinitionsForAdministrationQueryHandler))]
    [InlineData(typeof(CreateAchievementDefinitionCommand), typeof(CreateAchievementDefinitionCommandHandler))]
    [InlineData(typeof(UpdateAchievementDefinitionCommand), typeof(UpdateAchievementDefinitionCommandHandler))]
    [InlineData(typeof(ReviewContentReportCommand), typeof(ReviewContentReportCommandHandler))]
    [InlineData(typeof(DismissContentReportCommand), typeof(DismissContentReportCommandHandler))]
    [InlineData(typeof(GetContentReportsForAdministrationQuery), typeof(GetContentReportsForAdministrationQueryHandler))]
    [InlineData(typeof(CountContentReportsQuery), typeof(CountContentReportsQueryHandler))]
    [InlineData(typeof(UpsertEmailTemplateCommand), typeof(UpsertEmailTemplateCommandHandler))]
    [InlineData(typeof(GetEmailTemplateRevisionsQuery), typeof(GetEmailTemplateRevisionsQueryHandler))]
    [InlineData(typeof(GetEmailTemplatesQuery), typeof(GetEmailTemplatesQueryHandler))]
    [InlineData(typeof(CleanupDeletedUsersCommand), typeof(CleanupDeletedUsersCommandHandler))]
    [InlineData(typeof(CreateUserByAdministratorCommand), typeof(CreateUserByAdministratorCommandHandler))]
    [InlineData(typeof(UpdateUserByAdministratorCommand), typeof(UpdateUserByAdministratorCommandHandler))]
    [InlineData(typeof(SetUserPasswordByAdministratorCommand), typeof(SetUserPasswordByAdministratorCommandHandler))]
    [InlineData(typeof(GetFilteredUsersForAdministrationQuery), typeof(GetFilteredUsersForAdministrationQueryHandler))]
    [InlineData(typeof(GetUserForAdministrationQuery), typeof(GetUserForAdministrationQueryHandler))]
    [InlineData(typeof(GetUsersForAdministrationQuery), typeof(GetUsersForAdministrationQueryHandler))]
    [InlineData(typeof(GetUserAdministrationSummaryQuery), typeof(GetUserAdministrationSummaryQueryHandler))]
    [InlineData(typeof(GetUserBillingProfileQuery), typeof(GetUserBillingProfileQueryHandler))]
    [InlineData(typeof(GetUserBillingProfileIncludingDeletedQuery), typeof(GetUserBillingProfileIncludingDeletedQueryHandler))]
    [InlineData(typeof(StartUserPremiumTrialCommand), typeof(StartUserPremiumTrialCommandHandler))]
    [InlineData(typeof(EnsureUserPremiumRoleCommand), typeof(EnsureUserPremiumRoleCommandHandler))]
    [InlineData(typeof(RemoveUserPremiumRoleCommand), typeof(RemoveUserPremiumRoleCommandHandler))]
    [InlineData(typeof(CheckUserAccessQuery), typeof(CheckUserAccessQueryHandler))]
    [InlineData(typeof(RecordPremiumConversionCommand), typeof(RecordPremiumConversionCommandHandler))]
    [InlineData(typeof(GetLoginEventsQuery), typeof(GetLoginEventsQueryHandler))]
    [InlineData(typeof(GetLoginDeviceSummaryQuery), typeof(GetLoginDeviceSummaryQueryHandler))]
    [InlineData(typeof(GetCurrentCycleQuery), typeof(GetCurrentCycleQueryHandler))]
    [InlineData(typeof(ReadExerciseCaloriesQuery), typeof(ReadExerciseCaloriesQueryHandler))]
    [InlineData(typeof(ReadExerciseEntriesQuery), typeof(ReadExerciseEntriesQueryHandler))]
    [InlineData(typeof(CleanupFastingTelemetryCommand), typeof(CleanupFastingTelemetryCommandHandler))]
    [InlineData(typeof(SendFastingNotificationsCommand), typeof(SendFastingNotificationsCommandHandler))]
    [InlineData(typeof(ReadCurrentFastingQuery), typeof(ReadCurrentFastingQueryHandler))]
    [InlineData(typeof(ReadFastingInsightsQuery), typeof(ReadFastingInsightsQueryHandler))]
    [InlineData(typeof(ReadFastingOverviewQuery), typeof(ReadFastingOverviewQueryHandler))]
    [InlineData(typeof(ReadFavoriteMealsQuery), typeof(ReadFavoriteMealsQueryHandler))]
    [InlineData(typeof(ReadFavoriteProductsQuery), typeof(ReadFavoriteProductsQueryHandler))]
    [InlineData(typeof(ReadFavoriteRecipesQuery), typeof(ReadFavoriteRecipesQueryHandler))]
    [InlineData(typeof(ReadMealFavoriteIdsQuery), typeof(ReadMealFavoriteIdsQueryHandler))]
    [InlineData(typeof(ReadMealFavoriteStatusQuery), typeof(ReadMealFavoriteStatusQueryHandler))]
    [InlineData(typeof(ReadMealFavoritesOverviewQuery), typeof(ReadMealFavoritesOverviewQueryHandler))]
    [InlineData(typeof(ReadProductFavoriteStatusQuery), typeof(ReadProductFavoriteStatusQueryHandler))]
    [InlineData(typeof(ReadRecipeFavoriteStatusQuery), typeof(ReadRecipeFavoriteStatusQueryHandler))]
    [InlineData(typeof(CleanupMarketingAttributionCommand), typeof(CleanupMarketingAttributionCommandHandler))]
    [InlineData(typeof(CleanupExpiredNotificationsCommand), typeof(CleanupExpiredNotificationsCommandHandler))]
    [InlineData(typeof(ReadMealCountQuery), typeof(ReadMealCountQueryHandler))]
    [InlineData(typeof(ReadDistinctMealDatesQuery), typeof(ReadDistinctMealDatesQueryHandler))]
    [InlineData(typeof(ReadTotalMealCountQuery), typeof(ReadTotalMealCountQueryHandler))]
    [InlineData(typeof(ReadMealsForExportQuery), typeof(ReadMealsForExportQueryHandler))]
    [InlineData(typeof(SearchOpenFoodFactsQuery), typeof(SearchOpenFoodFactsQueryHandler))]
    public void OwnerRequest_HasMatchingHandlerAndPreservesCallerCommit(Type request, Type handler) {
        Type contract = Assert.Single(request.GetInterfaces(), type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>));
        Type expectedHandler = typeof(IRequestHandler<,>).MakeGenericType(request, contract.GetGenericArguments()[0]);
        Assert.True(expectedHandler.IsAssignableFrom(handler), handler.FullName);
        Assert.False(typeof(ITransactionalCommand).IsAssignableFrom(request), request.FullName);
        Assert.DoesNotContain(request.GetProperties(), property => property.PropertyType == typeof(CancellationToken));
    }
}
