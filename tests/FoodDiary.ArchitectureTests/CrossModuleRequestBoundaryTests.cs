using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CrossModuleRequestBoundaryTests {
    [Theory]
    [InlineData("Admin")]
    [InlineData("Ai")]
    [InlineData("Billing")]
    [InlineData("BodyMetrics")]
    [InlineData("Cycles")]
    [InlineData("ContentReports")]
    public void MigratedContracts_DoNotExportServiceInterfaces(string module) {
        var contracts = Assembly.Load($"FoodDiary.Modules.{module}.Contracts");
        Assert.DoesNotContain(contracts.GetExportedTypes(), type => type.IsInterface);
    }

    [Theory]
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
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.CreateLesson.CreateLessonCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Application.Lessons.Commands.CreateLesson.CreateLessonCommandHandler, FoodDiary.Application.Lessons")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson.UpdateLessonCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Application.Lessons.Commands.UpdateLesson.UpdateLessonCommandHandler, FoodDiary.Application.Lessons")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson.DeleteLessonCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Application.Lessons.Commands.DeleteLesson.DeleteLessonCommandHandler, FoodDiary.Application.Lessons")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons.ImportLessonsCommand, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Application.Lessons.Commands.ImportLessons.ImportLessonsCommandHandler, FoodDiary.Application.Lessons")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration.GetLessonsForAdministrationQuery, FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Application.Lessons.Queries.GetLessonsForAdministration.GetLessonsForAdministrationQueryHandler, FoodDiary.Application.Lessons")]
    [InlineData("FoodDiary.Application.Gamification.Queries.GetAchievementDefinitionsForAdministration.GetAchievementDefinitionsForAdministrationQuery, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Application.Gamification.Queries.GetAchievementDefinitionsForAdministration.GetAchievementDefinitionsForAdministrationQueryHandler, FoodDiary.Application.Gamification")]
    [InlineData("FoodDiary.Application.Gamification.Commands.CreateAchievementDefinition.CreateAchievementDefinitionCommand, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Application.Gamification.Commands.CreateAchievementDefinition.CreateAchievementDefinitionCommandHandler, FoodDiary.Application.Gamification")]
    [InlineData("FoodDiary.Application.Gamification.Commands.UpdateAchievementDefinition.UpdateAchievementDefinitionCommand, FoodDiary.Modules.Gamification.Contracts", "FoodDiary.Application.Gamification.Commands.UpdateAchievementDefinition.UpdateAchievementDefinitionCommandHandler, FoodDiary.Application.Gamification")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport.ReviewContentReportCommand, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport.ReviewContentReportCommandHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport.DismissContentReportCommand, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport.DismissContentReportCommandHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Queries.GetContentReportsForAdministration.GetContentReportsForAdministrationQuery, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Queries.GetContentReportsForAdministration.GetContentReportsForAdministrationQueryHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Modules.ContentReports.Contracts.Queries.CountContentReports.CountContentReportsQuery, FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Application.Queries.CountContentReports.CountContentReportsQueryHandler, FoodDiary.Modules.ContentReports.Application")]
    [InlineData("FoodDiary.Application.Abstractions.Email.Commands.UpsertEmailTemplate.UpsertEmailTemplateCommand, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Application.Identity.Email.Commands.UpsertEmailTemplate.UpsertEmailTemplateCommandHandler, FoodDiary.Application.Identity")]
    [InlineData("FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Application.Identity.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQueryHandler, FoodDiary.Application.Identity")]
    [InlineData("FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplates.GetEmailTemplatesQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Application.Identity.Email.Queries.GetEmailTemplates.GetEmailTemplatesQueryHandler, FoodDiary.Application.Identity")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Commands.CreateUserByAdministrator.CreateUserByAdministratorCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Commands.CreateUserByAdministrator.CreateUserByAdministratorCommandHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Commands.UpdateUserByAdministrator.UpdateUserByAdministratorCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Commands.UpdateUserByAdministrator.UpdateUserByAdministratorCommandHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Commands.SetUserPasswordByAdministrator.SetUserPasswordByAdministratorCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Commands.SetUserPasswordByAdministrator.SetUserPasswordByAdministratorCommandHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.GetFilteredUsersForAdministration.GetFilteredUsersForAdministrationQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.GetFilteredUsersForAdministration.GetFilteredUsersForAdministrationQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.GetUserForAdministration.GetUserForAdministrationQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.GetUserForAdministration.GetUserForAdministrationQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.GetUsersForAdministration.GetUsersForAdministrationQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.GetUsersForAdministration.GetUsersForAdministrationQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.GetUserAdministrationSummary.GetUserAdministrationSummaryQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.GetUserAdministrationSummary.GetUserAdministrationSummaryQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile.GetUserBillingProfileQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.GetUserBillingProfile.GetUserBillingProfileQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfileIncludingDeleted.GetUserBillingProfileIncludingDeletedQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.GetUserBillingProfileIncludingDeleted.GetUserBillingProfileIncludingDeletedQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Commands.StartUserPremiumTrial.StartUserPremiumTrialCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Commands.StartUserPremiumTrial.StartUserPremiumTrialCommandHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Commands.EnsureUserPremiumRole.EnsureUserPremiumRoleCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Commands.EnsureUserPremiumRole.EnsureUserPremiumRoleCommandHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Commands.RemoveUserPremiumRole.RemoveUserPremiumRoleCommand, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Commands.RemoveUserPremiumRole.RemoveUserPremiumRoleCommandHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Abstractions.Users.Queries.CheckUserAccess.CheckUserAccessQuery, FoodDiary.Modules.Users.Contracts", "FoodDiary.Application.Users.Queries.CheckUserAccess.CheckUserAccessQueryHandler, FoodDiary.Application.Users")]
    [InlineData("FoodDiary.Application.Marketing.Commands.RecordPremiumConversion.RecordPremiumConversionCommand, FoodDiary.Modules.Marketing.Contracts", "FoodDiary.Application.Marketing.Commands.RecordPremiumConversion.RecordPremiumConversionCommandHandler, FoodDiary.Application.Marketing")]
    [InlineData("FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginEvents.GetLoginEventsQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Application.Identity.Authentication.Queries.GetLoginEvents.GetLoginEventsQueryHandler, FoodDiary.Application.Identity")]
    [InlineData("FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginDeviceSummary.GetLoginDeviceSummaryQuery, FoodDiary.Modules.Identity.Contracts", "FoodDiary.Application.Identity.Authentication.Queries.GetLoginDeviceSummary.GetLoginDeviceSummaryQueryHandler, FoodDiary.Application.Identity")]
    [InlineData("FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle.GetCurrentCycleQuery, FoodDiary.Modules.Cycles.Contracts", "FoodDiary.Modules.Cycles.Application.Queries.GetCurrentCycle.GetCurrentCycleQueryHandler, FoodDiary.Modules.Cycles.Application")]
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
