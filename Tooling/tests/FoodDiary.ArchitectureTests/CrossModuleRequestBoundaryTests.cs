using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
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
    [InlineData(typeof(FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods.SearchUsdaFoodsQuery), typeof(FoodDiary.Modules.Usda.Application.Queries.SearchUsdaFoods.SearchUsdaFoodsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.WeeklyGoals.Contracts.Commands.SendWeeklyGoalReminders.SendWeeklyGoalRemindersCommand), typeof(FoodDiary.Modules.WeeklyGoals.Application.Commands.SendWeeklyGoalReminders.SendWeeklyGoalRemindersCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts.ReadRecentProductsQuery), typeof(FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentProducts.ReadRecentProductsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes.ReadRecentRecipesQuery), typeof(FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentRecipes.ReadRecentRecipesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationEntries.ReadHydrationEntriesQuery), typeof(FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationEntries.ReadHydrationEntriesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal.ReadHydrationDailyTotalQuery), typeof(FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotal.ReadHydrationDailyTotalQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals.ReadHydrationDailyTotalsQuery), typeof(FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotals.ReadHydrationDailyTotalsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval.ReadHydrationIntervalQuery), typeof(FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationInterval.ReadHydrationIntervalQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupLoginEvents.CleanupLoginEventsCommand), typeof(FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupLoginEvents.CleanupLoginEventsCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Authentication.Commands.BootstrapInitialAdmin.BootstrapInitialAdminCommand), typeof(FoodDiary.Modules.Identity.Application.Authentication.Commands.BootstrapInitialAdmin.BootstrapInitialAdminCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages.CleanupOrphanImagesCommand), typeof(FoodDiary.Modules.Images.Application.Commands.CleanupOrphanImages.CleanupOrphanImagesCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements.ReconcileAchievementsCommand), typeof(FoodDiary.Modules.Gamification.Application.Commands.ReconcileAchievements.ReconcileAchievementsCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQuery), typeof(FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry.ReadLatestWeightEntryQuery), typeof(FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadLatestWeightEntry.ReadLatestWeightEntryQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery), typeof(FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQuery), typeof(FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadLatestWaistEntry.ReadLatestWaistEntryQuery), typeof(FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadLatestWaistEntry.ReadLatestWaistEntryQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery), typeof(FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQuery), typeof(FoodDiary.Modules.Ai.Application.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageForUser.GetAiUsageForUserQuery), typeof(FoodDiary.Modules.Ai.Application.Queries.GetAiUsageForUser.GetAiUsageForUserQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageSummary.GetAiUsageSummaryQuery), typeof(FoodDiary.Modules.Ai.Application.Queries.GetAiUsageSummary.GetAiUsageSummaryQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates.GetAiPromptTemplatesQuery), typeof(FoodDiary.Modules.Ai.Application.Queries.GetAiPromptTemplates.GetAiPromptTemplatesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt.UpsertAiPromptCommand), typeof(FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt.UpsertAiPromptCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition.GetCompletedFoodRecognitionQuery), typeof(FoodDiary.Modules.Ai.Application.Queries.GetCompletedFoodRecognition.GetCompletedFoodRecognitionQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Contracts.Commands.CreateLesson.CreateLessonCommand), typeof(FoodDiary.Modules.Lessons.Application.Commands.CreateLesson.CreateLessonCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson.UpdateLessonCommand), typeof(FoodDiary.Modules.Lessons.Application.Commands.UpdateLesson.UpdateLessonCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson.DeleteLessonCommand), typeof(FoodDiary.Modules.Lessons.Application.Commands.DeleteLesson.DeleteLessonCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons.ImportLessonsCommand), typeof(FoodDiary.Modules.Lessons.Application.Commands.ImportLessons.ImportLessonsCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration.GetLessonsForAdministrationQuery), typeof(FoodDiary.Modules.Lessons.Application.Queries.GetLessonsForAdministration.GetLessonsForAdministrationQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration.GetAchievementDefinitionsForAdministrationQuery), typeof(FoodDiary.Modules.Gamification.Application.Queries.GetAchievementDefinitionsForAdministration.GetAchievementDefinitionsForAdministrationQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition.CreateAchievementDefinitionCommand), typeof(FoodDiary.Modules.Gamification.Application.Commands.CreateAchievementDefinition.CreateAchievementDefinitionCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Contracts.Commands.UpdateAchievementDefinition.UpdateAchievementDefinitionCommand), typeof(FoodDiary.Modules.Gamification.Application.Commands.UpdateAchievementDefinition.UpdateAchievementDefinitionCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport.ReviewContentReportCommand), typeof(FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport.ReviewContentReportCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport.DismissContentReportCommand), typeof(FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport.DismissContentReportCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Contracts.Queries.GetContentReportsForAdministration.GetContentReportsForAdministrationQuery), typeof(FoodDiary.Modules.ContentReports.Application.Queries.GetContentReportsForAdministration.GetContentReportsForAdministrationQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Contracts.Queries.CountContentReports.CountContentReportsQuery), typeof(FoodDiary.Modules.ContentReports.Application.Queries.CountContentReports.CountContentReportsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Email.Commands.UpsertEmailTemplate.UpsertEmailTemplateCommand), typeof(FoodDiary.Modules.Identity.Application.Email.Commands.UpsertEmailTemplate.UpsertEmailTemplateCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQuery), typeof(FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates.GetEmailTemplatesQuery), typeof(FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates.GetEmailTemplatesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers.CleanupDeletedUsersCommand), typeof(FoodDiary.Modules.Users.Application.Commands.CleanupDeletedUsers.CleanupDeletedUsersCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.CreateUserByAdministrator.CreateUserByAdministratorCommand), typeof(FoodDiary.Modules.Users.Application.Commands.CreateUserByAdministrator.CreateUserByAdministratorCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.UpdateUserByAdministrator.UpdateUserByAdministratorCommand), typeof(FoodDiary.Modules.Users.Application.Commands.UpdateUserByAdministrator.UpdateUserByAdministratorCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator.SetUserPasswordByAdministratorCommand), typeof(FoodDiary.Modules.Users.Application.Commands.SetUserPasswordByAdministrator.SetUserPasswordByAdministratorCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.GetFilteredUsersForAdministration.GetFilteredUsersForAdministrationQuery), typeof(FoodDiary.Modules.Users.Application.Queries.GetFilteredUsersForAdministration.GetFilteredUsersForAdministrationQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.GetUserForAdministration.GetUserForAdministrationQuery), typeof(FoodDiary.Modules.Users.Application.Queries.GetUserForAdministration.GetUserForAdministrationQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.GetUsersForAdministration.GetUsersForAdministrationQuery), typeof(FoodDiary.Modules.Users.Application.Queries.GetUsersForAdministration.GetUsersForAdministrationQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.GetUserAdministrationSummary.GetUserAdministrationSummaryQuery), typeof(FoodDiary.Modules.Users.Application.Queries.GetUserAdministrationSummary.GetUserAdministrationSummaryQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile.GetUserBillingProfileQuery), typeof(FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfile.GetUserBillingProfileQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted.GetUserBillingProfileIncludingDeletedQuery), typeof(FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfileIncludingDeleted.GetUserBillingProfileIncludingDeletedQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial.StartUserPremiumTrialCommand), typeof(FoodDiary.Modules.Users.Application.Commands.StartUserPremiumTrial.StartUserPremiumTrialCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole.EnsureUserPremiumRoleCommand), typeof(FoodDiary.Modules.Users.Application.Commands.EnsureUserPremiumRole.EnsureUserPremiumRoleCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole.RemoveUserPremiumRoleCommand), typeof(FoodDiary.Modules.Users.Application.Commands.RemoveUserPremiumRole.RemoveUserPremiumRoleCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess.CheckUserAccessQuery), typeof(FoodDiary.Modules.Users.Application.Queries.CheckUserAccess.CheckUserAccessQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion.RecordPremiumConversionCommand), typeof(FoodDiary.Modules.Marketing.Application.Commands.RecordPremiumConversion.RecordPremiumConversionCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginEvents.GetLoginEventsQuery), typeof(FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginEvents.GetLoginEventsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginDeviceSummary.GetLoginDeviceSummaryQuery), typeof(FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginDeviceSummary.GetLoginDeviceSummaryQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle.GetCurrentCycleQuery), typeof(FoodDiary.Modules.Cycles.Application.Queries.GetCurrentCycle.GetCurrentCycleQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories.ReadExerciseCaloriesQuery), typeof(FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseCalories.ReadExerciseCaloriesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries.ReadExerciseEntriesQuery), typeof(FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseEntries.ReadExerciseEntriesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Fasting.Contracts.Commands.CleanupFastingTelemetry.CleanupFastingTelemetryCommand), typeof(FoodDiary.Modules.Fasting.Application.Commands.CleanupFastingTelemetry.CleanupFastingTelemetryCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Fasting.Contracts.Commands.SendFastingNotifications.SendFastingNotificationsCommand), typeof(FoodDiary.Modules.Fasting.Application.Commands.SendFastingNotifications.SendFastingNotificationsCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting.ReadCurrentFastingQuery), typeof(FoodDiary.Modules.Fasting.Application.Queries.ReadCurrentFasting.ReadCurrentFastingQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingInsights.ReadFastingInsightsQuery), typeof(FoodDiary.Modules.Fasting.Application.Queries.ReadFastingInsights.ReadFastingInsightsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingOverview.ReadFastingOverviewQuery), typeof(FoodDiary.Modules.Fasting.Application.Queries.ReadFastingOverview.ReadFastingOverviewQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadFavoriteMeals.ReadFavoriteMealsQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadFavoriteMeals.ReadFavoriteMealsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts.ReadFavoriteProductsQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadFavoriteProducts.ReadFavoriteProductsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipes.ReadFavoriteRecipesQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadFavoriteRecipes.ReadFavoriteRecipesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds.ReadMealFavoriteIdsQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteIds.ReadMealFavoriteIdsQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteStatus.ReadMealFavoriteStatusQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteStatus.ReadMealFavoriteStatusQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview.ReadMealFavoritesOverviewQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoritesOverview.ReadMealFavoritesOverviewQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadProductFavoriteStatus.ReadProductFavoriteStatusQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadProductFavoriteStatus.ReadProductFavoriteStatusQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus.ReadRecipeFavoriteStatusQuery), typeof(FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus.ReadRecipeFavoriteStatusQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Marketing.Contracts.Commands.CleanupMarketingAttribution.CleanupMarketingAttributionCommand), typeof(FoodDiary.Modules.Marketing.Application.Commands.CleanupMarketingAttribution.CleanupMarketingAttributionCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications.CleanupExpiredNotificationsCommand), typeof(FoodDiary.Modules.Notifications.Application.Commands.CleanupExpiredNotifications.CleanupExpiredNotificationsCommandHandler))]
    [InlineData(typeof(FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount.ReadMealCountQuery), typeof(FoodDiary.Modules.Meals.Application.Queries.ReadMealCount.ReadMealCountQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates.ReadDistinctMealDatesQuery), typeof(FoodDiary.Modules.Meals.Application.Queries.ReadDistinctMealDates.ReadDistinctMealDatesQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount.ReadTotalMealCountQuery), typeof(FoodDiary.Modules.Meals.Application.Queries.ReadTotalMealCount.ReadTotalMealCountQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.Meals.Contracts.Queries.ReadMealsForExport.ReadMealsForExportQuery), typeof(FoodDiary.Modules.Meals.Application.Queries.ReadMealsForExport.ReadMealsForExportQueryHandler))]
    [InlineData(typeof(FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts.SearchOpenFoodFactsQuery), typeof(FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchProducts.SearchOpenFoodFactsQueryHandler))]
    public void OwnerRequest_HasMatchingHandlerAndPreservesCallerCommit(Type request, Type handler) {
        Type contract = Assert.Single(request.GetInterfaces(), type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>));
        Type expectedHandler = typeof(IRequestHandler<,>).MakeGenericType(request, contract.GetGenericArguments()[0]);
        Assert.True(expectedHandler.IsAssignableFrom(handler), handler.FullName);
        Assert.False(typeof(ITransactionalCommand).IsAssignableFrom(request), request.FullName);
        Assert.DoesNotContain(request.GetProperties(), property => property.PropertyType == typeof(CancellationToken));
    }
}
