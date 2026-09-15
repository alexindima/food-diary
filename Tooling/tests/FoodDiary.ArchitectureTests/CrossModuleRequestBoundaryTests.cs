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
    [InlineData("FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods.SearchUsdaFoodsQuery, FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Usda.Application.Queries.SearchUsdaFoods.SearchUsdaFoodsQueryHandler, FoodDiary.Modules.Usda.Application")]
    [InlineData("FoodDiary.Modules.WeeklyGoals.Contracts.Commands.SendWeeklyGoalReminders.SendWeeklyGoalRemindersCommand, FoodDiary.Modules.WeeklyGoals.Contracts", "FoodDiary.Modules.WeeklyGoals.Application.Commands.SendWeeklyGoalReminders.SendWeeklyGoalRemindersCommandHandler, FoodDiary.Modules.WeeklyGoals.Application")]
    [InlineData("FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts.ReadRecentProductsQuery, FoodDiary.Modules.RecentItems.Contracts", "FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentProducts.ReadRecentProductsQueryHandler, FoodDiary.Modules.RecentItems.Application")]
    [InlineData("FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes.ReadRecentRecipesQuery, FoodDiary.Modules.RecentItems.Contracts", "FoodDiary.Modules.RecentItems.Application.Queries.ReadRecentRecipes.ReadRecentRecipesQueryHandler, FoodDiary.Modules.RecentItems.Application")]
    [InlineData("FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationEntries.ReadHydrationEntriesQuery, FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationEntries.ReadHydrationEntriesQueryHandler, FoodDiary.Modules.Hydration.Application")]
    [InlineData("FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal.ReadHydrationDailyTotalQuery, FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotal.ReadHydrationDailyTotalQueryHandler, FoodDiary.Modules.Hydration.Application")]
    [InlineData("FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals.ReadHydrationDailyTotalsQuery, FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationDailyTotals.ReadHydrationDailyTotalsQueryHandler, FoodDiary.Modules.Hydration.Application")]
    [InlineData("FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationInterval.ReadHydrationIntervalQuery, FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Application.Queries.ReadHydrationInterval.ReadHydrationIntervalQueryHandler, FoodDiary.Modules.Hydration.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupLoginEvents.CleanupLoginEventsCommand, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupLoginEvents.CleanupLoginEventsCommandHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Authentication.Commands.BootstrapInitialAdmin.BootstrapInitialAdminCommand, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Authentication.Commands.BootstrapInitialAdmin.BootstrapInitialAdminCommandHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages.CleanupOrphanImagesCommand, FoodDiary.Modules.Images.Service.Contracts", "FoodDiary.Modules.Images.Application.Commands.CleanupOrphanImages.CleanupOrphanImagesCommandHandler, FoodDiary.Modules.Images.Application")]
    [InlineData("FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements.ReconcileAchievementsCommand, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Modules.Gamification.Application.Commands.ReconcileAchievements.ReconcileAchievementsCommandHandler, FoodDiary.Modules.Gamification.Application")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQuery, FoodDiary.Modules.BodyMetrics.Contracts", "FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQueryHandler, FoodDiary.Modules.BodyMetrics.Application")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry.ReadLatestWeightEntryQuery, FoodDiary.Modules.BodyMetrics.Contracts", "FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadLatestWeightEntry.ReadLatestWeightEntryQueryHandler, FoodDiary.Modules.BodyMetrics.Application")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery, FoodDiary.Modules.BodyMetrics.Contracts", "FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQueryHandler, FoodDiary.Modules.BodyMetrics.Application")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQuery, FoodDiary.Modules.BodyMetrics.Contracts", "FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQueryHandler, FoodDiary.Modules.BodyMetrics.Application")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadLatestWaistEntry.ReadLatestWaistEntryQuery, FoodDiary.Modules.BodyMetrics.Contracts", "FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadLatestWaistEntry.ReadLatestWaistEntryQueryHandler, FoodDiary.Modules.BodyMetrics.Application")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery, FoodDiary.Modules.BodyMetrics.Contracts", "FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQueryHandler, FoodDiary.Modules.BodyMetrics.Application")]
    [InlineData("FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQuery, FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Application.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQueryHandler, FoodDiary.Application.Ai")]
    [InlineData("FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageForUser.GetAiUsageForUserQuery, FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Application.Queries.GetAiUsageForUser.GetAiUsageForUserQueryHandler, FoodDiary.Application.Ai")]
    [InlineData("FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageSummary.GetAiUsageSummaryQuery, FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Application.Queries.GetAiUsageSummary.GetAiUsageSummaryQueryHandler, FoodDiary.Application.Ai")]
    [InlineData("FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates.GetAiPromptTemplatesQuery, FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Application.Queries.GetAiPromptTemplates.GetAiPromptTemplatesQueryHandler, FoodDiary.Application.Ai")]
    [InlineData("FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt.UpsertAiPromptCommand, FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt.UpsertAiPromptCommandHandler, FoodDiary.Application.Ai")]
    [InlineData("FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition.GetCompletedFoodRecognitionQuery, FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Application.Queries.GetCompletedFoodRecognition.GetCompletedFoodRecognitionQueryHandler, FoodDiary.Application.Ai")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.CreateLesson.CreateLessonCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Application.Commands.CreateLesson.CreateLessonCommandHandler, FoodDiary.Modules.Lessons.Application")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson.UpdateLessonCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Application.Commands.UpdateLesson.UpdateLessonCommandHandler, FoodDiary.Modules.Lessons.Application")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson.DeleteLessonCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Application.Commands.DeleteLesson.DeleteLessonCommandHandler, FoodDiary.Modules.Lessons.Application")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons.ImportLessonsCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Application.Commands.ImportLessons.ImportLessonsCommandHandler, FoodDiary.Modules.Lessons.Application")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration.GetLessonsForAdministrationQuery, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Application.Queries.GetLessonsForAdministration.GetLessonsForAdministrationQueryHandler, FoodDiary.Modules.Lessons.Application")]
    [InlineData("FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration.GetAchievementDefinitionsForAdministrationQuery, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Modules.Gamification.Application.Queries.GetAchievementDefinitionsForAdministration.GetAchievementDefinitionsForAdministrationQueryHandler, FoodDiary.Modules.Gamification.Application")]
    [InlineData("FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition.CreateAchievementDefinitionCommand, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Modules.Gamification.Application.Commands.CreateAchievementDefinition.CreateAchievementDefinitionCommandHandler, FoodDiary.Modules.Gamification.Application")]
    [InlineData("FoodDiary.Modules.Gamification.Contracts.Commands.UpdateAchievementDefinition.UpdateAchievementDefinitionCommand, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Modules.Gamification.Application.Commands.UpdateAchievementDefinition.UpdateAchievementDefinitionCommandHandler, FoodDiary.Modules.Gamification.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport.ReviewContentReportCommand, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport.ReviewContentReportCommandHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport.DismissContentReportCommand, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport.DismissContentReportCommandHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Queries.GetContentReportsForAdministration.GetContentReportsForAdministrationQuery, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Queries.GetContentReportsForAdministration.GetContentReportsForAdministrationQueryHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Queries.CountContentReports.CountContentReportsQuery, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Queries.CountContentReports.CountContentReportsQueryHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Email.Commands.UpsertEmailTemplate.UpsertEmailTemplateCommand, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Email.Commands.UpsertEmailTemplate.UpsertEmailTemplateCommandHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQueryHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates.GetEmailTemplatesQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates.GetEmailTemplatesQueryHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers.CleanupDeletedUsersCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.CleanupDeletedUsers.CleanupDeletedUsersCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.CreateUserByAdministrator.CreateUserByAdministratorCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.CreateUserByAdministrator.CreateUserByAdministratorCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.UpdateUserByAdministrator.UpdateUserByAdministratorCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.UpdateUserByAdministrator.UpdateUserByAdministratorCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator.SetUserPasswordByAdministratorCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.SetUserPasswordByAdministrator.SetUserPasswordByAdministratorCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.GetFilteredUsersForAdministration.GetFilteredUsersForAdministrationQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.GetFilteredUsersForAdministration.GetFilteredUsersForAdministrationQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.GetUserForAdministration.GetUserForAdministrationQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.GetUserForAdministration.GetUserForAdministrationQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.GetUsersForAdministration.GetUsersForAdministrationQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.GetUsersForAdministration.GetUsersForAdministrationQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.GetUserAdministrationSummary.GetUserAdministrationSummaryQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.GetUserAdministrationSummary.GetUserAdministrationSummaryQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile.GetUserBillingProfileQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfile.GetUserBillingProfileQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted.GetUserBillingProfileIncludingDeletedQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfileIncludingDeleted.GetUserBillingProfileIncludingDeletedQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial.StartUserPremiumTrialCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.StartUserPremiumTrial.StartUserPremiumTrialCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole.EnsureUserPremiumRoleCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.EnsureUserPremiumRole.EnsureUserPremiumRoleCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole.RemoveUserPremiumRoleCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Commands.RemoveUserPremiumRole.RemoveUserPremiumRoleCommandHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess.CheckUserAccessQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Application.Queries.CheckUserAccess.CheckUserAccessQueryHandler, FoodDiary.Modules.Users.Application")]
    [InlineData("FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion.RecordPremiumConversionCommand, FoodDiary.Modules.Marketing.Contracts", "FoodDiary.Modules.Marketing.Application.Commands.RecordPremiumConversion.RecordPremiumConversionCommandHandler, FoodDiary.Modules.Marketing.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginEvents.GetLoginEventsQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginEvents.GetLoginEventsQueryHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginDeviceSummary.GetLoginDeviceSummaryQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginDeviceSummary.GetLoginDeviceSummaryQueryHandler, FoodDiary.Modules.Identity.Application")]
    [InlineData("FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle.GetCurrentCycleQuery, FoodDiary.Modules.Cycles.Contracts", "FoodDiary.Modules.Cycles.Application.Queries.GetCurrentCycle.GetCurrentCycleQueryHandler, FoodDiary.Modules.Cycles.Application")]
    [InlineData("FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories.ReadExerciseCaloriesQuery, FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseCalories.ReadExerciseCaloriesQueryHandler, FoodDiary.Modules.Exercises.Application")]
    [InlineData("FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries.ReadExerciseEntriesQuery, FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseEntries.ReadExerciseEntriesQueryHandler, FoodDiary.Modules.Exercises.Application")]
    [InlineData("FoodDiary.Modules.Fasting.Contracts.Commands.CleanupFastingTelemetry.CleanupFastingTelemetryCommand, FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Application.Commands.CleanupFastingTelemetry.CleanupFastingTelemetryCommandHandler, FoodDiary.Modules.Fasting.Application")]
    [InlineData("FoodDiary.Modules.Fasting.Contracts.Commands.SendFastingNotifications.SendFastingNotificationsCommand, FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Application.Commands.SendFastingNotifications.SendFastingNotificationsCommandHandler, FoodDiary.Modules.Fasting.Application")]
    [InlineData("FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting.ReadCurrentFastingQuery, FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Application.Queries.ReadCurrentFasting.ReadCurrentFastingQueryHandler, FoodDiary.Modules.Fasting.Application")]
    [InlineData("FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingInsights.ReadFastingInsightsQuery, FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Application.Queries.ReadFastingInsights.ReadFastingInsightsQueryHandler, FoodDiary.Modules.Fasting.Application")]
    [InlineData("FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingOverview.ReadFastingOverviewQuery, FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Application.Queries.ReadFastingOverview.ReadFastingOverviewQueryHandler, FoodDiary.Modules.Fasting.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadFavoriteMeals.ReadFavoriteMealsQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadFavoriteMeals.ReadFavoriteMealsQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts.ReadFavoriteProductsQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadFavoriteProducts.ReadFavoriteProductsQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipes.ReadFavoriteRecipesQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadFavoriteRecipes.ReadFavoriteRecipesQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds.ReadMealFavoriteIdsQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteIds.ReadMealFavoriteIdsQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteStatus.ReadMealFavoriteStatusQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoriteStatus.ReadMealFavoriteStatusQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview.ReadMealFavoritesOverviewQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoritesOverview.ReadMealFavoritesOverviewQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadProductFavoriteStatus.ReadProductFavoriteStatusQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadProductFavoriteStatus.ReadProductFavoriteStatusQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus.ReadRecipeFavoriteStatusQuery, FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus.ReadRecipeFavoriteStatusQueryHandler, FoodDiary.Modules.Favorites.Application")]
    [InlineData("FoodDiary.Modules.Marketing.Contracts.Commands.CleanupMarketingAttribution.CleanupMarketingAttributionCommand, FoodDiary.Modules.Marketing.Contracts", "FoodDiary.Modules.Marketing.Application.Commands.CleanupMarketingAttribution.CleanupMarketingAttributionCommandHandler, FoodDiary.Modules.Marketing.Application")]
    [InlineData("FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications.CleanupExpiredNotificationsCommand, FoodDiary.Modules.Notifications.Contracts", "FoodDiary.Modules.Notifications.Application.Commands.CleanupExpiredNotifications.CleanupExpiredNotificationsCommandHandler, FoodDiary.Modules.Notifications.Application")]
    [InlineData("FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount.ReadMealCountQuery, FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Application.Queries.ReadMealCount.ReadMealCountQueryHandler, FoodDiary.Modules.Meals.Application")]
    [InlineData("FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates.ReadDistinctMealDatesQuery, FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Application.Queries.ReadDistinctMealDates.ReadDistinctMealDatesQueryHandler, FoodDiary.Modules.Meals.Application")]
    [InlineData("FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount.ReadTotalMealCountQuery, FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Application.Queries.ReadTotalMealCount.ReadTotalMealCountQueryHandler, FoodDiary.Modules.Meals.Application")]
    [InlineData("FoodDiary.Modules.Meals.Contracts.Queries.ReadMealsForExport.ReadMealsForExportQuery, FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Application.Queries.ReadMealsForExport.ReadMealsForExportQueryHandler, FoodDiary.Modules.Meals.Application")]
    [InlineData("FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts.SearchOpenFoodFactsQuery, FoodDiary.Modules.OpenFoodFacts.Contracts", "FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchProducts.SearchOpenFoodFactsQueryHandler, FoodDiary.Modules.OpenFoodFacts.Application")]
    public void OwnerRequest_HasMatchingHandlerAndPreservesCallerCommit(string requestName, string handlerName) {
        Type request = Type.GetType(requestName, throwOnError: true)!;
        Type handler = Type.GetType(handlerName, throwOnError: true)!;
        Type contract = Assert.Single(request.GetInterfaces(), type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>));
        Type expectedHandler = typeof(IRequestHandler<,>).MakeGenericType(request, contract.GetGenericArguments()[0]);
        Assert.True(expectedHandler.IsAssignableFrom(handler), handlerName);
        Assert.False(typeof(ITransactionalCommand).IsAssignableFrom(request), requestName);
        Assert.DoesNotContain(request.GetProperties(), property => property.PropertyType == typeof(CancellationToken));
    }
}
