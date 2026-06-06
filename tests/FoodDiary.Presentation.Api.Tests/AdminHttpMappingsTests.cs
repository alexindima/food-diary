using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Services;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.ReviewContentReport;
using FoodDiary.Application.Admin.Commands.DismissContentReport;
using FoodDiary.Application.Admin.Commands.CreateAdminLesson;
using FoodDiary.Application.Admin.Commands.UpdateAdminLesson;
using FoodDiary.Application.Admin.Commands.DeleteAdminLesson;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;
using FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;
using FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Application.Admin.Commands.ImportAdminLessons;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminHttpMappingsTests {
    [Fact]
    public void AdminUserUpdateHttpRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var request = new AdminUserUpdateHttpRequest(
            IsActive: false,
            IsEmailConfirmed: true,
            Roles: ["Admin", "User"],
            Language: "ru",
            AiInputTokenLimit: 1000,
            AiOutputTokenLimit: 2000);

        UpdateAdminUserCommand command = request.ToCommand(userId, actorUserId);

        Assert.Equal(userId, command.UserId);
        Assert.False(command.IsActive);
        Assert.True(command.IsEmailConfirmed);
        Assert.Equal(["Admin", "User"], command.Roles);
        Assert.Equal("ru", command.Language);
        Assert.Equal(1000, command.AiInputTokenLimit);
        Assert.Equal(2000, command.AiOutputTokenLimit);
        Assert.Equal(actorUserId, command.ActorUserId);
    }

    [Fact]
    public void AdminUserUpdateHttpRequest_ToCommand_WithNullRoles_MapsEmptyRoles() {
        var request = new AdminUserUpdateHttpRequest(
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: null,
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        UpdateAdminUserCommand command = request.ToCommand(Guid.NewGuid(), Guid.NewGuid());

        Assert.NotNull(command.Roles);
        Assert.Empty(command.Roles);
    }

    [Fact]
    public void AdminEmailTemplateUpsertHttpRequest_ToCommand_MapsAllFields() {
        var request = new AdminEmailTemplateUpsertHttpRequest(
            Subject: "Welcome",
            HtmlBody: "<p>Hello</p>",
            TextBody: "Hello",
            IsActive: true);

        UpsertAdminEmailTemplateCommand command = request.ToCommand("welcome", "en");

        Assert.Equal("welcome", command.Key);
        Assert.Equal("en", command.Locale);
        Assert.Equal("Welcome", command.Subject);
        Assert.Equal("<p>Hello</p>", command.HtmlBody);
        Assert.Equal("Hello", command.TextBody);
        Assert.True(command.IsActive);
    }

    [Fact]
    public void AdminEmailTemplateTestHttpRequest_ToCommand_MapsAllFields() {
        var request = new AdminEmailTemplateTestHttpRequest(
            ToEmail: "user@example.com",
            Key: "welcome",
            Subject: "Welcome",
            HtmlBody: "<p>Hello</p>",
            TextBody: "Hello");

        SendAdminEmailTemplateTestCommand command = request.ToCommand();

        Assert.Equal(request.ToEmail, command.ToEmail);
        Assert.Equal(request.Key, command.Key);
        Assert.Equal(request.Subject, command.Subject);
        Assert.Equal(request.HtmlBody, command.HtmlBody);
        Assert.Equal(request.TextBody, command.TextBody);
    }

    [Fact]
    public void AdminAiPromptUpsertHttpRequest_ToCommand_MapsAllFields() {
        var request = new AdminAiPromptUpsertHttpRequest("Prompt text", true);

        UpsertAdminAiPromptCommand command = request.ToCommand("meal-analysis", "ru");

        Assert.Equal("meal-analysis", command.Key);
        Assert.Equal("ru", command.Locale);
        Assert.Equal("Prompt text", command.PromptText);
        Assert.True(command.IsActive);
    }

    [Fact]
    public void AdminImpersonationStartHttpRequest_ToCommand_MapsContext() {
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var request = new AdminImpersonationStartHttpRequest("Support case");

        StartAdminImpersonationCommand command = request.ToCommand(actorUserId, targetUserId, "203.0.113.1", "Agent/1.0");

        Assert.Equal(actorUserId, command.ActorUserId);
        Assert.Equal(targetUserId, command.TargetUserId);
        Assert.Equal("Support case", command.Reason);
        Assert.Equal("203.0.113.1", command.ActorIpAddress);
        Assert.Equal("Agent/1.0", command.ActorUserAgent);
    }

    [Fact]
    public void AdminReportActionHttpRequest_ToCommands_MapReportIdAndNote() {
        var reportId = Guid.NewGuid();
        var request = new AdminReportActionHttpRequest("Reviewed");

        ReviewContentReportCommand review = request.ToReviewCommand(reportId);
        DismissContentReportCommand dismiss = request.ToDismissCommand(reportId);

        Assert.Equal(reportId, review.ReportId);
        Assert.Equal("Reviewed", review.AdminNote);
        Assert.Equal(reportId, dismiss.ReportId);
        Assert.Equal("Reviewed", dismiss.AdminNote);
    }

    [Fact]
    public void AdminLessonRequests_ToCommands_MapAllFields() {
        var lessonId = Guid.NewGuid();
        var create = new AdminLessonCreateHttpRequest(
            "Title", "Content", "Summary", "en", "nutrition", "beginner", 4, 10);
        var update = new AdminLessonUpdateHttpRequest(
            "Updated", "Updated content", null, "ru", "fasting", "advanced", 6, 20);

        CreateAdminLessonCommand createCommand = create.ToCreateCommand();
        UpdateAdminLessonCommand updateCommand = update.ToUpdateCommand(lessonId);
        DeleteAdminLessonCommand deleteCommand = lessonId.ToDeleteCommand();

        Assert.Equal("Title", createCommand.Title);
        Assert.Equal("Content", createCommand.Content);
        Assert.Equal("Summary", createCommand.Summary);
        Assert.Equal("en", createCommand.Locale);
        Assert.Equal("nutrition", createCommand.Category);
        Assert.Equal("beginner", createCommand.Difficulty);
        Assert.Equal(4, createCommand.EstimatedReadMinutes);
        Assert.Equal(10, createCommand.SortOrder);
        Assert.Equal(lessonId, updateCommand.Id);
        Assert.Equal("Updated", updateCommand.Title);
        Assert.Null(updateCommand.Summary);
        Assert.Equal("ru", updateCommand.Locale);
        Assert.Equal(lessonId, deleteCommand.Id);
    }

    [Fact]
    public void GetAdminUsersHttpQuery_ToQuery_ParsesExplicitStatusCaseInsensitive() {
        var httpQuery = new GetAdminUsersHttpQuery(
            Page: 2,
            Limit: 30,
            Search: "alex",
            Status: "deleted",
            IncludeDeleted: false);

        GetAdminUsersQuery query = httpQuery.ToQuery();

        Assert.Equal(2, query.Page);
        Assert.Equal(30, query.Limit);
        Assert.Equal("alex", query.Search);
        Assert.Equal(UserAccountStatusFilter.Deleted, query.Status);
    }

    [Fact]
    public void GetAdminUsersHttpQuery_ToQuery_FallsBackToIncludeDeletedFlag() {
        var includeDeletedQuery = new GetAdminUsersHttpQuery(IncludeDeleted: true);
        var activeOnlyQuery = new GetAdminUsersHttpQuery(IncludeDeleted: false);

        Assert.Equal(UserAccountStatusFilter.All, includeDeletedQuery.ToQuery().Status);
        Assert.Equal(UserAccountStatusFilter.Active, activeOnlyQuery.ToQuery().Status);
    }

    [Fact]
    public void AdminQueryMappings_MapSimpleQueries() {
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-1);
        DateTime to = DateTime.UtcNow;
        var fromDate = new DateOnly(2026, 4, 1);
        var toDate = new DateOnly(2026, 4, 30);

        Assert.NotNull(AdminHttpQueryMappings.ToEmailTemplatesQuery());
        Assert.NotNull(AdminHttpQueryMappings.ToAiPromptsQuery());
        Assert.NotNull(AdminHttpQueryMappings.ToLessonsQuery());
        Assert.Equal(userId, userId.ToAdminUserQuery().UserId);
        Assert.Equal(userId, new GetAdminUserRoleAuditHttpQuery(12).ToRoleAuditQuery(userId).UserId);
        Assert.Equal(12, new GetAdminUserRoleAuditHttpQuery(12).ToRoleAuditQuery(userId).Limit);
        Assert.Equal(messageId, messageId.ToMailInboxMessageDetailsQuery().Id);

        GetAdminUserLoginEventsQuery loginEvents = new GetAdminUserLoginEventsHttpQuery(2, 30, userId, "mail").ToQuery();
        Assert.Equal(2, loginEvents.Page);
        Assert.Equal(30, loginEvents.Limit);
        Assert.Equal(userId, loginEvents.UserId);
        Assert.Equal("mail", loginEvents.Search);

        GetAdminUserLoginSummaryQuery loginSummary = new GetAdminUserLoginSummaryHttpQuery(from, to).ToQuery();
        Assert.Equal(from, loginSummary.FromUtc);
        Assert.Equal(to, loginSummary.ToUtc);

        GetAdminAiUsageSummaryQuery aiUsage = new GetAdminAiUsageSummaryHttpQuery(fromDate, toDate).ToQuery();
        Assert.Equal(fromDate, aiUsage.From);
        Assert.Equal(toDate, aiUsage.To);

        Assert.Equal("pending", new GetAdminContentReportsHttpQuery("pending", 3, 40).ToQuery().Status);
        Assert.Equal(25, new GetAdminMailInboxMessagesHttpQuery(25).ToQuery().Limit);
    }

    [Fact]
    public void AdminBillingHttpQuery_ToQueries_MapsFilters() {
        DateTime from = DateTime.UtcNow.AddDays(-7);
        DateTime to = DateTime.UtcNow;
        var httpQuery = new GetAdminBillingHttpQuery(
            Page: 3,
            Limit: 50,
            Provider: "stripe",
            Status: "active",
            Kind: "invoice",
            Search: "user@example.com",
            FromUtc: from,
            ToUtc: to);

        GetAdminBillingSubscriptionsQuery subscriptions = httpQuery.ToSubscriptionsQuery();
        GetAdminBillingPaymentsQuery payments = httpQuery.ToPaymentsQuery();
        GetAdminBillingWebhookEventsQuery webhookEvents = httpQuery.ToWebhookEventsQuery();

        Assert.Equal(3, subscriptions.Page);
        Assert.Equal(50, subscriptions.Limit);
        Assert.Equal("stripe", subscriptions.Provider);
        Assert.Equal("active", subscriptions.Status);
        Assert.Equal("user@example.com", subscriptions.Search);
        Assert.Equal(from, subscriptions.FromUtc);
        Assert.Equal(to, subscriptions.ToUtc);
        Assert.Equal("invoice", payments.Kind);
        Assert.Equal("stripe", webhookEvents.Provider);
        Assert.Equal("active", webhookEvents.Status);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(7, 7)]
    [InlineData(100, 20)]
    public void GetAdminDashboardHttpQuery_ToQuery_ClampsRecentLimit(int recent, int expectedLimit) {
        GetAdminDashboardSummaryQuery query = new GetAdminDashboardHttpQuery(recent).ToQuery();

        Assert.Equal(expectedLimit, query.RecentLimit);
    }

    [Fact]
    public void AdminLessonsImportHttpRequest_ToImportCommand_MapsNestedLessons() {
        var request = new AdminLessonsImportHttpRequest(
            Version: 3,
            Lessons: [
                new AdminLessonImportItemHttpRequest(
                    Title: "Protein basics",
                    Content: "Content",
                    Summary: null,
                    Locale: "en",
                    Category: "nutrition",
                    Difficulty: "beginner",
                    EstimatedReadMinutes: 4,
                    SortOrder: 10),
            ]);

        ImportAdminLessonsCommand command = request.ToImportCommand();

        Assert.Equal(3, command.Version);
        ImportAdminLessonItem lesson = Assert.Single(command.Lessons);
        Assert.Equal("Protein basics", lesson.Title);
        Assert.Equal("Content", lesson.Content);
        Assert.Null(lesson.Summary);
        Assert.Equal("en", lesson.Locale);
        Assert.Equal("nutrition", lesson.Category);
        Assert.Equal("beginner", lesson.Difficulty);
        Assert.Equal(4, lesson.EstimatedReadMinutes);
        Assert.Equal(10, lesson.SortOrder);
    }

    [Fact]
    public void AdminDashboardSummaryModel_ToHttpResponse_MapsRecentUsers() {
        var userId = Guid.NewGuid();
        DateTime createdOnUtc = DateTime.UtcNow;
        var model = new AdminDashboardSummaryModel(
            TotalUsers: 10,
            ActiveUsers: 8,
            PremiumUsers: 2,
            DeletedUsers: 1,
            PendingReportsCount: 3,
            RecentUsers: [CreateUser(userId, createdOnUtc)]);

        AdminDashboardSummaryHttpResponse response = model.ToHttpResponse();

        Assert.Equal(10, response.TotalUsers);
        Assert.Equal(8, response.ActiveUsers);
        Assert.Equal(2, response.PremiumUsers);
        Assert.Equal(1, response.DeletedUsers);
        Assert.Equal(3, response.PendingReportsCount);
        AdminUserHttpResponse user = Assert.Single(response.RecentUsers);
        Assert.Equal(userId, user.Id);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(["Admin"], user.Roles);
        Assert.Equal(createdOnUtc, user.CreatedOnUtc);
    }

    [Fact]
    public void AdminAiUsageSummaryModel_ToHttpResponse_MapsNestedBreakdowns() {
        var userId = Guid.NewGuid();
        var model = new AdminAiUsageSummaryModel(
            TotalTokens: 300,
            InputTokens: 100,
            OutputTokens: 200,
            ByDay: [new AdminAiUsageDailyModel(new DateOnly(2026, 4, 6), 30, 10, 20)],
            ByOperation: [new AdminAiUsageBreakdownModel("meal-analysis", 90, 30, 60)],
            ByModel: [new AdminAiUsageBreakdownModel("gpt-test", 120, 40, 80)],
            ByUser: [new AdminAiUsageUserModel(userId, "user@example.com", 150, 50, 100)]);

        AdminAiUsageSummaryHttpResponse response = model.ToHttpResponse();

        Assert.Equal(300, response.TotalTokens);
        Assert.Equal(100, response.InputTokens);
        Assert.Equal(200, response.OutputTokens);
        Assert.Equal(new DateOnly(2026, 4, 6), Assert.Single(response.ByDay).Date);
        Assert.Equal("meal-analysis", Assert.Single(response.ByOperation).Key);
        Assert.Equal("gpt-test", Assert.Single(response.ByModel).Key);
        AdminAiUsageUserHttpResponse user = Assert.Single(response.ByUser);
        Assert.Equal(userId, user.Id);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(150, user.TotalTokens);
    }

    [Fact]
    public void AdminSingleModels_ToHttpResponse_MapAllFields() {
        DateTime now = DateTime.UtcNow;
        DateTimeOffset offsetNow = DateTimeOffset.UtcNow;
        var prompt = new AdminAiPromptModel(Guid.NewGuid(), "key", "en", "Prompt", 2, true, now, now.AddMinutes(1));
        var lesson = new AdminLessonModel(Guid.NewGuid(), "Title", "Content", "Summary", "en", "nutrition", "beginner", 4, 10, now, now.AddMinutes(2));
        var template = new AdminEmailTemplateModel(Guid.NewGuid(), "welcome", "en", "Subject", "<p>Body</p>", "Body", true, now, null);
        var impersonationStart = new AdminImpersonationStartModel("token", Guid.NewGuid(), "target@example.com", Guid.NewGuid(), "Support");
        var impersonationSession = new AdminImpersonationSessionReadModel(Guid.NewGuid(), Guid.NewGuid(), "actor@example.com", Guid.NewGuid(), "target@example.com", "Support", "ip", "agent", now);
        var loginEvent = new AdminUserLoginEventModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "password", "ip", "agent", "Chrome", "1", "Windows", "Desktop", now);
        var device = new AdminUserLoginDeviceSummaryModel("Chrome|Windows", 3, now);
        var audit = new AdminUserRoleAuditEventReadModel(Guid.NewGuid(), Guid.NewGuid(), "Admin", "Added", Guid.NewGuid(), "actor@example.com", "manual", now);
        var report = new AdminContentReportModel(Guid.NewGuid(), Guid.NewGuid(), "Recipe", Guid.NewGuid(), "Spam", "Pending", null, now, null);
        var mailSummary = new AdminMailInboxMessageSummaryModel(Guid.NewGuid(), "from@example.com", ["to@example.com"], "Subject", "received", offsetNow);
        var mailDetails = new AdminMailInboxMessageDetailsModel(Guid.NewGuid(), "message-id", "from@example.com", ["to@example.com"], "Subject", "Text", "<p>Html</p>", "raw", "received", offsetNow);

        Assert.Equal("key", prompt.ToAiPromptHttpResponse().Key);
        Assert.Equal("Title", lesson.ToLessonHttpResponse().Title);
        Assert.Equal(1, new AdminLessonsImportModel(1, [lesson]).ToLessonsImportHttpResponse().ImportedCount);
        Assert.Equal("welcome", template.ToHttpResponse().Key);
        Assert.Equal("token", impersonationStart.ToHttpResponse().AccessToken);
        Assert.Equal("actor@example.com", impersonationSession.ToHttpResponse().ActorEmail);
        Assert.Equal("Chrome", loginEvent.ToHttpResponse().BrowserName);
        Assert.Equal(3, device.ToHttpResponse().Count);
        Assert.Equal("Admin", audit.ToHttpResponse().RoleName);
        Assert.Equal("Spam", report.ToHttpResponse().Reason);
        Assert.Equal("from@example.com", mailSummary.ToHttpResponse().FromAddress);
        Assert.Equal("raw", mailDetails.ToHttpResponse().RawMime);
    }

    [Fact]
    public void AdminBillingModels_ToHttpResponse_MapAllFields() {
        DateTime now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var subscription = new AdminBillingSubscriptionReadModel(
            subscriptionId, userId, "user@example.com", "stripe", "customer", "subscription", "payment-method", "price",
            "premium", "active", now.AddDays(-1), now.AddDays(30), true, now.AddDays(29), "event-1", now, now, null);
        var payment = new AdminBillingPaymentReadModel(
            Guid.NewGuid(), userId, "user@example.com", subscriptionId, "stripe", "payment", "customer", "subscription",
            "payment-method", "price", "premium", "succeeded", "invoice", 12.5m, "USD", now.AddDays(-1), now.AddDays(30),
            "event-2", "{}", now, null);
        var webhook = new AdminBillingWebhookEventReadModel(
            Guid.NewGuid(), "stripe", "event-3", "invoice.paid", "invoice-1", "processed", now, "{}", null, now, null);

        AdminBillingSubscriptionHttpResponse subscriptionResponse = subscription.ToHttpResponse();
        AdminBillingPaymentHttpResponse paymentResponse = payment.ToHttpResponse();
        AdminBillingWebhookEventHttpResponse webhookResponse = webhook.ToHttpResponse();

        Assert.Equal(subscriptionId, subscriptionResponse.Id);
        Assert.Equal("customer", subscriptionResponse.ExternalCustomerId);
        Assert.True(subscriptionResponse.CancelAtPeriodEnd);
        Assert.Equal(12.5m, paymentResponse.Amount);
        Assert.Equal("invoice", paymentResponse.Kind);
        Assert.Equal("invoice.paid", webhookResponse.EventType);
        Assert.Equal("processed", webhookResponse.Status);
    }

    [Fact]
    public void FastingTelemetrySummarySnapshot_ToHttpResponse_MapsSummaryAndPresets() {
        DateTime generatedAtUtc = DateTime.UtcNow;
        DateTime lastCheckInAtUtc = generatedAtUtc.AddMinutes(-10);
        DateTime lastEventAtUtc = generatedAtUtc.AddMinutes(-5);
        var summary = new FastingTelemetrySummarySnapshot(
            WindowHours: 168,
            GeneratedAtUtc: generatedAtUtc,
            StartedSessions: 10,
            CompletedSessions: 7,
            SavedCheckIns: 5,
            ReminderPresetSelections: 4,
            ReminderTimingSaves: 3,
            PresetReminderTimingSaves: 2,
            ManualReminderTimingSaves: 1,
            CompletionRatePercent: 70,
            CheckInRatePercent: 50,
            AverageCompletedDurationHours: 18.5,
            LastCheckInAtUtc: lastCheckInAtUtc,
            LastEventAtUtc: lastEventAtUtc,
            TopPresets: [
                new FastingTelemetryPresetSnapshot(
                    PresetId: "morning",
                    SelectionCount: 4,
                    TimingSaveCount: 3,
                    FirstReminderHours: 8,
                    FollowUpReminderHours: 2,
                    StartedSessions: 6,
                    CompletedSessions: 5,
                    SavedCheckIns: 4,
                    CompletionRatePercent: 83.3,
                    CheckInRatePercent: 66.7),
            ]);

        FastingTelemetrySummaryHttpResponse response = summary.ToHttpResponse();

        Assert.Equal(168, response.WindowHours);
        Assert.Equal(generatedAtUtc, response.GeneratedAtUtc);
        Assert.Equal(10, response.StartedSessions);
        Assert.Equal(7, response.CompletedSessions);
        Assert.Equal(5, response.SavedCheckIns);
        Assert.Equal(4, response.ReminderPresetSelections);
        Assert.Equal(3, response.ReminderTimingSaves);
        Assert.Equal(2, response.PresetReminderTimingSaves);
        Assert.Equal(1, response.ManualReminderTimingSaves);
        Assert.Equal(70, response.CompletionRatePercent);
        Assert.Equal(50, response.CheckInRatePercent);
        Assert.Equal(18.5, response.AverageCompletedDurationHours);
        Assert.Equal(lastCheckInAtUtc, response.LastCheckInAtUtc);
        Assert.Equal(lastEventAtUtc, response.LastEventAtUtc);
        FastingTelemetryPresetHttpResponse preset = Assert.Single(response.TopPresets);
        Assert.Equal("morning", preset.PresetId);
        Assert.Equal(4, preset.SelectionCount);
        Assert.Equal(3, preset.TimingSaveCount);
        Assert.Equal(8, preset.FirstReminderHours);
        Assert.Equal(2, preset.FollowUpReminderHours);
        Assert.Equal(6, preset.StartedSessions);
        Assert.Equal(5, preset.CompletedSessions);
        Assert.Equal(4, preset.SavedCheckIns);
        Assert.Equal(83.3, preset.CompletionRatePercent);
        Assert.Equal(66.7, preset.CheckInRatePercent);
    }

    [Fact]
    public void AdminPagedResponses_ToHttpResponse_MapItemsAndMetadata() {
        AdminUserModel user = CreateUser(Guid.NewGuid(), DateTime.UtcNow);
        var session = new AdminImpersonationSessionReadModel(Guid.NewGuid(), Guid.NewGuid(), "actor@example.com", Guid.NewGuid(), "target@example.com", "Support", null, null, DateTime.UtcNow);
        var loginEvent = new AdminUserLoginEventModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "password", null, null, null, null, null, null, DateTime.UtcNow);
        var report = new AdminContentReportModel(Guid.NewGuid(), Guid.NewGuid(), "Recipe", Guid.NewGuid(), "Spam", "Pending", null, DateTime.UtcNow, null);
        var subscription = new AdminBillingSubscriptionReadModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "stripe", "customer", null, null, null, null, "active", null, null, false, null, null, null, DateTime.UtcNow, null);
        var payment = new AdminBillingPaymentReadModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", null, "stripe", "payment", null, null, null, null, null, "succeeded", "invoice", null, null, null, null, null, null, DateTime.UtcNow, null);
        var webhook = new AdminBillingWebhookEventReadModel(Guid.NewGuid(), "stripe", "event", "invoice.paid", null, "processed", DateTime.UtcNow, null, null, DateTime.UtcNow, null);

        Assert.Single(new PagedResponse<AdminUserModel>([user], 2, 10, 3, 21).ToHttpResponse().Data);
        Assert.Single(new PagedResponse<AdminImpersonationSessionReadModel>([session], 1, 10, 1, 1).ToImpersonationSessionsHttpResponse().Data);
        Assert.Single(new PagedResponse<AdminUserLoginEventModel>([loginEvent], 1, 10, 1, 1).ToLoginEventsHttpResponse().Data);
        Assert.Single(new PagedResponse<AdminContentReportModel>([report], 1, 10, 1, 1).ToHttpResponse().Data);
        Assert.Single(new PagedResponse<AdminBillingSubscriptionReadModel>([subscription], 1, 10, 1, 1).ToBillingSubscriptionsHttpResponse().Data);
        Assert.Single(new PagedResponse<AdminBillingPaymentReadModel>([payment], 1, 10, 1, 1).ToBillingPaymentsHttpResponse().Data);
        Assert.Single(new PagedResponse<AdminBillingWebhookEventReadModel>([webhook], 1, 10, 1, 1).ToBillingWebhookEventsHttpResponse().Data);
    }

    private static AdminUserModel CreateUser(Guid id, DateTime createdOnUtc) {
        return new AdminUserModel(
            Id: id,
            Email: "user@example.com",
            HasPassword: true,
            Username: "user",
            FirstName: "Alex",
            LastName: "Tester",
            BirthDate: null,
            Gender: null,
            Weight: null,
            DesiredWeight: null,
            DesiredWaist: null,
            Height: null,
            ActivityLevel: "moderate",
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            StepGoal: null,
            WaterGoal: null,
            HydrationGoal: null,
            CalorieCyclingEnabled: false,
            MondayCalories: null,
            TuesdayCalories: null,
            WednesdayCalories: null,
            ThursdayCalories: null,
            FridayCalories: null,
            SaturdayCalories: null,
            SundayCalories: null,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayoutJson: null,
            Language: "en",
            Theme: "light",
            UiStyle: null,
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 2,
            TelegramUserId: null,
            IsActive: true,
            IsEmailConfirmed: true,
            CreatedOnUtc: createdOnUtc,
            DeletedAt: null,
            LastLoginAtUtc: null,
            Roles: ["Admin"],
            AiInputTokenLimit: 1000,
            AiOutputTokenLimit: 2000,
            AiConsentAcceptedAt: null);
    }
}
