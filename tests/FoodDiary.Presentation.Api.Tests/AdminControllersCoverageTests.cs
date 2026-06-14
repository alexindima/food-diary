using System.Net;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Commands.CreateAdminLesson;
using FoodDiary.Application.Admin.Commands.DeleteAdminLesson;
using FoodDiary.Application.Admin.Commands.DismissContentReport;
using FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;
using FoodDiary.Application.Admin.Commands.ReviewContentReport;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.UpdateAdminLesson;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;
using FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;
using FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;
using FoodDiary.Application.Admin.Queries.GetAdminContentReports;
using FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;
using FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;
using FoodDiary.Application.Admin.Queries.GetAdminLessons;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;
using FoodDiary.Application.Admin.Queries.GetAdminUser;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;
using FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminControllersCoverageTests {
    [Fact]
    public async Task AdminLessonsController_CoversCrudEndpoints() {
        AdminLessonModel lesson = CreateLesson();
        CapturedSender createSender = SubstituteSender.Capture(Result.Success(lesson));
        AdminLessonsController createController = CreateController(new AdminLessonsController(createSender));
        var lessonId = Guid.NewGuid();

        IActionResult create = await createController.Create(CreateLessonRequest());

        CreatedResult created = Assert.IsType<CreatedResult>(create);
        Assert.IsType<AdminLessonHttpResponse>(created.Value);
        Assert.IsType<CreateAdminLessonCommand>(createSender.Request);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(lesson));
        AdminLessonsController updateController = CreateController(new AdminLessonsController(updateSender));
        IActionResult update = await updateController.Update(lessonId, UpdateLessonRequest());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(update);
        Assert.IsType<AdminLessonHttpResponse>(ok.Value);
        UpdateAdminLessonCommand updateCommand = Assert.IsType<UpdateAdminLessonCommand>(updateSender.Request);
        Assert.Equal(lessonId, updateCommand.Id);

        CapturedSender deleteSender = SubstituteSender.Capture(Result.Success());
        AdminLessonsController deleteController = CreateController(new AdminLessonsController(deleteSender));
        IActionResult delete = await deleteController.Delete(lessonId);

        Assert.IsType<NoContentResult>(delete);
        Assert.Equal(lessonId, Assert.IsType<DeleteAdminLessonCommand>(deleteSender.Request).Id);
    }

    [Fact]
    public async Task AdminLessonsController_CoversListAndImportEndpoints() {
        CapturedSender listSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AdminLessonModel>>([CreateLesson()]));
        AdminLessonsController listController = CreateController(new AdminLessonsController(listSender));

        IActionResult list = await listController.GetAll();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(list);
        Assert.IsType<List<AdminLessonHttpResponse>>(ok.Value);
        Assert.IsType<GetAdminLessonsQuery>(listSender.Request);

        var importModel = new AdminLessonsImportModel(1, [CreateLesson()]);
        CapturedSender importSender = SubstituteSender.Capture(Result.Success(importModel));
        AdminLessonsController importController = CreateController(new AdminLessonsController(importSender));

        IActionResult import = await importController.Import(new AdminLessonsImportHttpRequest(1, [CreateImportLessonRequest()]));

        OkObjectResult importOk = Assert.IsType<OkObjectResult>(import);
        Assert.IsType<AdminLessonsImportHttpResponse>(importOk.Value);
    }

    [Fact]
    public async Task AdminUsersController_CoversUserReadAuditAndImpersonationEndpoints() {
        AdminUserModel user = CreateUser();
        CapturedSender sender = SubstituteSender.Capture(Result.Success(user));
        AdminUsersController controller = CreateController(new AdminUsersController(sender));
        var userId = Guid.NewGuid();

        IActionResult getUser = await controller.GetUser(userId);

        Assert.IsType<AdminUserHttpResponse>(Assert.IsType<OkObjectResult>(getUser).Value);
        Assert.Equal(userId, Assert.IsType<GetAdminUserQuery>(sender.Request).UserId);

        var audit = new AdminUserRoleAuditEventReadModel(Guid.NewGuid(), userId, "Admin", "Added", Guid.NewGuid(), "actor@example.com", "manual", DateTime.UtcNow);
        CapturedSender auditSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AdminUserRoleAuditEventReadModel>>([audit]));
        AdminUsersController auditController = CreateController(new AdminUsersController(auditSender));

        IActionResult roleAudit = await auditController.GetUserRoleAudit(userId, new GetAdminUserRoleAuditHttpQuery(12));

        Assert.IsType<List<AdminUserRoleAuditEventHttpResponse>>(Assert.IsType<OkObjectResult>(roleAudit).Value);
        Assert.Equal(12, Assert.IsType<GetAdminUserRoleAuditQuery>(auditSender.Request).Limit);

        var startModel = new AdminImpersonationStartModel("token", userId, "target@example.com", Guid.NewGuid(), "Support");
        CapturedSender impersonationSender = SubstituteSender.Capture(Result.Success(startModel));
        AdminUsersController impersonationController = CreateController(new AdminUsersController(impersonationSender));
        impersonationController.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1");
        impersonationController.Request.Headers.UserAgent = "Agent/1.0";
        var actorUserId = Guid.NewGuid();

        IActionResult start = await impersonationController.StartImpersonation(userId, actorUserId, new AdminImpersonationStartHttpRequest("Support"));

        Assert.IsType<AdminImpersonationStartHttpResponse>(Assert.IsType<OkObjectResult>(start).Value);
        StartAdminImpersonationCommand command = Assert.IsType<StartAdminImpersonationCommand>(impersonationSender.Request);
        Assert.Equal(actorUserId, command.ActorUserId);
        Assert.Equal(userId, command.TargetUserId);
        Assert.Equal("203.0.113.1", command.ActorIpAddress);
        Assert.Equal("Agent/1.0", command.ActorUserAgent);
    }

    [Fact]
    public async Task AdminUsersController_StartImpersonation_WhenRequestContextMissing_SendsNullContext() {
        var targetUserId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var model = new AdminImpersonationStartModel("token", targetUserId, "target@example.com", actorUserId, "Support");
        CapturedSender sender = SubstituteSender.Capture(Result.Success(model));
        AdminUsersController controller = CreateController(new AdminUsersController(sender));

        IActionResult result = await controller.StartImpersonation(targetUserId, actorUserId, new AdminImpersonationStartHttpRequest("Support"));

        Assert.IsType<AdminImpersonationStartHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        StartAdminImpersonationCommand command = Assert.IsType<StartAdminImpersonationCommand>(sender.Request);
        Assert.Null(command.ActorIpAddress);
        Assert.Equal(string.Empty, command.ActorUserAgent);
    }

    [Fact]
    public async Task AdminUsersController_CoversPagedAndUpdateEndpoints() {
        AdminUserModel user = CreateUser();
        var pagedUsers = new PagedResponse<AdminUserModel>([user], 2, 10, 3, 21);
        CapturedSender usersSender = SubstituteSender.Capture(Result.Success(pagedUsers));
        AdminUsersController usersController = CreateController(new AdminUsersController(usersSender));

        IActionResult users = await usersController.GetUsers(new GetAdminUsersHttpQuery(Page: 2, Limit: 10));

        Assert.IsType<PagedHttpResponse<AdminUserHttpResponse>>(Assert.IsType<OkObjectResult>(users).Value);

        var session = new AdminImpersonationSessionReadModel(Guid.NewGuid(), Guid.NewGuid(), "actor@example.com", Guid.NewGuid(), "target@example.com", "Support", "ip", "agent", DateTime.UtcNow);
        CapturedSender sessionSender = SubstituteSender.Capture(Result.Success(new PagedResponse<AdminImpersonationSessionReadModel>([session], 1, 10, 1, 1)));
        AdminUsersController sessionController = CreateController(new AdminUsersController(sessionSender));
        IActionResult sessions = await sessionController.GetImpersonationSessions(new GetAdminImpersonationSessionsHttpQuery(Search: "target"));
        Assert.IsType<PagedHttpResponse<AdminImpersonationSessionHttpResponse>>(Assert.IsType<OkObjectResult>(sessions).Value);
        Assert.Equal("target", Assert.IsType<GetAdminImpersonationSessionsQuery>(sessionSender.Request).Search);

        var login = new AdminUserLoginEventModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "password", "ip", "agent", "Chrome", "1", "Windows", "Desktop", DateTime.UtcNow);
        CapturedSender loginSender = SubstituteSender.Capture(Result.Success(new PagedResponse<AdminUserLoginEventModel>([login], 1, 10, 1, 1)));
        AdminUsersController loginController = CreateController(new AdminUsersController(loginSender));
        IActionResult loginEvents = await loginController.GetLoginEvents(new GetAdminUserLoginEventsHttpQuery(Search: "user"));
        Assert.IsType<PagedHttpResponse<AdminUserLoginEventHttpResponse>>(Assert.IsType<OkObjectResult>(loginEvents).Value);
        Assert.Equal("user", Assert.IsType<GetAdminUserLoginEventsQuery>(loginSender.Request).Search);

        var device = new AdminUserLoginDeviceSummaryModel("Chrome|Windows", 3, DateTime.UtcNow);
        CapturedSender summarySender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>([device]));
        AdminUsersController summaryController = CreateController(new AdminUsersController(summarySender));
        IActionResult summary = await summaryController.GetLoginSummary(new GetAdminUserLoginSummaryHttpQuery());
        Assert.IsType<List<AdminUserLoginDeviceSummaryHttpResponse>>(Assert.IsType<OkObjectResult>(summary).Value);
        Assert.IsType<GetAdminUserLoginSummaryQuery>(summarySender.Request);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(user));
        AdminUsersController updateController = CreateController(new AdminUsersController(updateSender));
        var targetUserId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var updateRequest = new AdminUserUpdateHttpRequest(IsActive: true, IsEmailConfirmed: true, Roles: ["Admin"], Language: "en", AiInputTokenLimit: 10, AiOutputTokenLimit: 20);

        IActionResult update = await updateController.UpdateUser(targetUserId, actorUserId, updateRequest);

        Assert.IsType<AdminUserHttpResponse>(Assert.IsType<OkObjectResult>(update).Value);
        UpdateAdminUserCommand updateCommand = Assert.IsType<UpdateAdminUserCommand>(updateSender.Request);
        Assert.Equal(targetUserId, updateCommand.UserId);
        Assert.Equal(actorUserId, updateCommand.ActorUserId);
    }

    [Fact]
    public async Task AdminAiPromptsAndUsageControllers_CoverEndpoints() {
        AdminAiPromptModel prompt = new(Guid.NewGuid(), "key", "en", "Prompt", 1, IsActive: true, DateTime.UtcNow, UpdatedOnUtc: null);
        CapturedSender promptsSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AdminAiPromptModel>>([prompt]));
        AdminAiPromptsController promptsController = CreateController(new AdminAiPromptsController(promptsSender));

        IActionResult prompts = await promptsController.GetAll();

        Assert.IsType<List<AdminAiPromptHttpResponse>>(Assert.IsType<OkObjectResult>(prompts).Value);
        Assert.IsType<GetAdminAiPromptsQuery>(promptsSender.Request);

        CapturedSender upsertSender = SubstituteSender.Capture(Result.Success(prompt));
        AdminAiPromptsController upsertController = CreateController(new AdminAiPromptsController(upsertSender));
        IActionResult upsert = await upsertController.Upsert("key", "en", new AdminAiPromptUpsertHttpRequest("Prompt", IsActive: true));
        Assert.IsType<AdminAiPromptHttpResponse>(Assert.IsType<OkObjectResult>(upsert).Value);
        Assert.IsType<UpsertAdminAiPromptCommand>(upsertSender.Request);

        AdminAiUsageSummaryModel usage = new(300, 100, 200, [], [], [], []);
        CapturedSender usageSender = SubstituteSender.Capture(Result.Success(usage));
        AdminAiUsageController usageController = CreateController(new AdminAiUsageController(usageSender));
        IActionResult summary = await usageController.GetSummary(new GetAdminAiUsageSummaryHttpQuery(From: null, To: null));
        Assert.IsType<AdminAiUsageSummaryHttpResponse>(Assert.IsType<OkObjectResult>(summary).Value);
        Assert.IsType<GetAdminAiUsageSummaryQuery>(usageSender.Request);
    }

    [Fact]
    public async Task AdminBillingController_CoversAllEndpoints() {
        DateTime now = DateTime.UtcNow;
        var subscription = new AdminBillingSubscriptionReadModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "stripe", "customer", "sub", "pm", "price", "premium", "active", now, now.AddDays(30), CancelAtPeriodEnd: false, NextBillingAttemptUtc: null, LastWebhookEventId: null, LastSyncedAtUtc: null, now, ModifiedOnUtc: null);
        var payment = new AdminBillingPaymentReadModel(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", BillingSubscriptionId: null, "stripe", "payment", ExternalCustomerId: null, ExternalSubscriptionId: null, ExternalPaymentMethodId: null, ExternalPriceId: null, Plan: null, "succeeded", "invoice", Amount: null, Currency: null, CurrentPeriodStartUtc: null, CurrentPeriodEndUtc: null, WebhookEventId: null, ProviderMetadataJson: null, now, ModifiedOnUtc: null);
        var webhook = new AdminBillingWebhookEventReadModel(Guid.NewGuid(), "stripe", "event", "invoice.paid", ExternalObjectId: null, "processed", now, PayloadJson: null, ErrorMessage: null, now, ModifiedOnUtc: null);

        CapturedSender subscriptionSender = SubstituteSender.Capture(Result.Success(new PagedResponse<AdminBillingSubscriptionReadModel>([subscription], 1, 10, 1, 1)));
        AdminBillingController subscriptionController = CreateController(new AdminBillingController(subscriptionSender));
        Assert.IsType<PagedHttpResponse<AdminBillingSubscriptionHttpResponse>>(Assert.IsType<OkObjectResult>(await subscriptionController.GetSubscriptions(new GetAdminBillingHttpQuery(Provider: "stripe"))).Value);
        Assert.Equal("stripe", Assert.IsType<GetAdminBillingSubscriptionsQuery>(subscriptionSender.Request).Provider);

        CapturedSender paymentSender = SubstituteSender.Capture(Result.Success(new PagedResponse<AdminBillingPaymentReadModel>([payment], 1, 10, 1, 1)));
        AdminBillingController paymentController = CreateController(new AdminBillingController(paymentSender));
        Assert.IsType<PagedHttpResponse<AdminBillingPaymentHttpResponse>>(Assert.IsType<OkObjectResult>(await paymentController.GetPayments(new GetAdminBillingHttpQuery(Kind: "invoice"))).Value);
        Assert.Equal("invoice", Assert.IsType<GetAdminBillingPaymentsQuery>(paymentSender.Request).Kind);

        CapturedSender webhookSender = SubstituteSender.Capture(Result.Success(new PagedResponse<AdminBillingWebhookEventReadModel>([webhook], 1, 10, 1, 1)));
        AdminBillingController webhookController = CreateController(new AdminBillingController(webhookSender));
        Assert.IsType<PagedHttpResponse<AdminBillingWebhookEventHttpResponse>>(Assert.IsType<OkObjectResult>(await webhookController.GetWebhookEvents(new GetAdminBillingHttpQuery(Status: "processed"))).Value);
        Assert.Equal("processed", Assert.IsType<GetAdminBillingWebhookEventsQuery>(webhookSender.Request).Status);
    }

    [Fact]
    public async Task AdminEmailTemplatesController_CoversAllEndpoints() {
        AdminEmailTemplateModel template = new(Guid.NewGuid(), "welcome", "en", "Subject", "<p>Body</p>", "Body", IsActive: true, DateTime.UtcNow, UpdatedOnUtc: null);
        CapturedSender listSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AdminEmailTemplateModel>>([template]));
        AdminEmailTemplatesController listController = CreateController(new AdminEmailTemplatesController(listSender));
        Assert.IsType<List<AdminEmailTemplateHttpResponse>>(Assert.IsType<OkObjectResult>(await listController.GetAll()).Value);
        Assert.IsType<GetAdminEmailTemplatesQuery>(listSender.Request);

        CapturedSender upsertSender = SubstituteSender.Capture(Result.Success(template));
        AdminEmailTemplatesController upsertController = CreateController(new AdminEmailTemplatesController(upsertSender));
        IActionResult upsert = await upsertController.Upsert("welcome", "en", new AdminEmailTemplateUpsertHttpRequest("Subject", "<p>Body</p>", "Body", IsActive: true));
        Assert.IsType<AdminEmailTemplateHttpResponse>(Assert.IsType<OkObjectResult>(upsert).Value);
        Assert.IsType<UpsertAdminEmailTemplateCommand>(upsertSender.Request);

        CapturedSender testSender = SubstituteSender.Capture(Result.Success());
        AdminEmailTemplatesController testController = CreateController(new AdminEmailTemplatesController(testSender));
        IActionResult sendTest = await testController.SendTest(new AdminEmailTemplateTestHttpRequest("user@example.com", "welcome", "Subject", "<p>Body</p>", "Body"));
        Assert.IsType<NoContentResult>(sendTest);
        Assert.IsType<SendAdminEmailTemplateTestCommand>(testSender.Request);
    }

    [Fact]
    public async Task AdminMailInboxController_CoversAllEndpoints() {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var summary = new AdminMailInboxMessageSummaryModel(Guid.NewGuid(), "from@example.com", ["to@example.com"], "Subject", "general", "received", ReadAtUtc: null, now);
        CapturedSender listSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AdminMailInboxMessageSummaryModel>>([summary]));
        AdminMailInboxController listController = CreateController(new AdminMailInboxController(listSender));
        IActionResult list = await listController.GetMessages(new GetAdminMailInboxMessagesHttpQuery(25));
        Assert.IsType<List<AdminMailInboxMessageSummaryHttpResponse>>(Assert.IsType<OkObjectResult>(list).Value);
        Assert.Equal(25, Assert.IsType<GetAdminMailInboxMessagesQuery>(listSender.Request).Limit);

        var details = new AdminMailInboxMessageDetailsModel(Guid.NewGuid(), "message-id", "from@example.com", ["to@example.com"], "Subject", "Text", "<p>Html</p>", "raw", "general", "received", ReadAtUtc: null, now);
        CapturedSender detailSender = SubstituteSender.Capture(Result.Success(details));
        AdminMailInboxController detailController = CreateController(new AdminMailInboxController(detailSender));
        IActionResult detail = await detailController.GetMessage(details.Id);
        Assert.IsType<AdminMailInboxMessageDetailsHttpResponse>(Assert.IsType<OkObjectResult>(detail).Value);
        Assert.Equal(details.Id, Assert.IsType<GetAdminMailInboxMessageDetailsQuery>(detailSender.Request).Id);

        CapturedSender markSender = SubstituteSender.Capture(Result.Success());
        AdminMailInboxController markController = CreateController(new AdminMailInboxController(markSender));
        IActionResult mark = await markController.MarkRead(details.Id);
        Assert.IsType<NoContentResult>(mark);
        Assert.Equal(details.Id, Assert.IsType<MarkAdminMailInboxMessageReadCommand>(markSender.Request).Id);
    }

    [Fact]
    public async Task AdminModerationController_CoversAllEndpoints() {
        var report = new AdminContentReportModel(Guid.NewGuid(), Guid.NewGuid(), "Recipe", Guid.NewGuid(), "Spam", "Pending", AdminNote: null, DateTime.UtcNow, ReviewedAtUtc: null);
        CapturedSender listSender = SubstituteSender.Capture(Result.Success(new PagedResponse<AdminContentReportModel>([report], 1, 10, 1, 1)));
        AdminModerationController listController = CreateController(new AdminModerationController(listSender));
        IActionResult list = await listController.GetReports(new GetAdminContentReportsHttpQuery("pending", 1, 10));
        Assert.IsType<PagedHttpResponse<AdminContentReportHttpResponse>>(Assert.IsType<OkObjectResult>(list).Value);
        Assert.Equal("pending", Assert.IsType<GetAdminContentReportsQuery>(listSender.Request).Status);

        var reportId = Guid.NewGuid();
        var action = new AdminReportActionHttpRequest("done");
        CapturedSender reviewSender = SubstituteSender.Capture(Result.Success());
        AdminModerationController reviewController = CreateController(new AdminModerationController(reviewSender));
        Assert.IsType<NoContentResult>(await reviewController.Review(reportId, action));
        Assert.Equal(reportId, Assert.IsType<ReviewContentReportCommand>(reviewSender.Request).ReportId);

        CapturedSender dismissSender = SubstituteSender.Capture(Result.Success());
        AdminModerationController dismissController = CreateController(new AdminModerationController(dismissSender));
        Assert.IsType<NoContentResult>(await dismissController.Dismiss(reportId, action));
        Assert.Equal(reportId, Assert.IsType<DismissContentReportCommand>(dismissSender.Request).ReportId);
    }

    [Fact]
    public async Task AdminTelemetryController_GetFastingSummary_ReturnsSummaryAndUsesRequestAborted() {
        var snapshot = new FastingTelemetrySummarySnapshot(24, DateTime.UtcNow, 1, 1, 1, 0, 0, 0, 0, 100, 100, 18, LastCheckInAtUtc: null, LastEventAtUtc: null, TopPresets: []);
        IFastingTelemetrySummaryService service = Substitute.For<IFastingTelemetrySummaryService>();
        var controller = new AdminTelemetryController(service) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        using var cts = new CancellationTokenSource();
        controller.HttpContext.RequestAborted = cts.Token;
        service.GetSummaryAsync(48, cts.Token).Returns(Task.FromResult(snapshot));

        IActionResult result = await controller.GetFastingSummary(new GetFastingTelemetrySummaryHttpQuery(48));

        FastingTelemetrySummaryHttpResponse response = Assert.IsType<FastingTelemetrySummaryHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(24, response.WindowHours);
        await service.Received(1).GetSummaryAsync(48, cts.Token);
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static AdminLessonCreateHttpRequest CreateLessonRequest() =>
        new("Title", "Content", "Summary", "en", "nutrition", "beginner", 4, 10);

    private static AdminLessonUpdateHttpRequest UpdateLessonRequest() =>
        new("Updated", "Content", "Summary", "en", "nutrition", "beginner", 5, 20);

    private static AdminLessonImportItemHttpRequest CreateImportLessonRequest() =>
        new("Title", "Content", "Summary", "en", "nutrition", "beginner", 4, 10);

    private static AdminLessonModel CreateLesson() =>
        new(Guid.NewGuid(), "Title", "Content", "Summary", "en", "nutrition", "beginner", 4, 10, DateTime.UtcNow, ModifiedOnUtc: null);

    private static AdminUserModel CreateUser() =>
        new(
            Guid.NewGuid(),
            "user@example.com",
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
            CreatedOnUtc: DateTime.UtcNow,
            DeletedAt: null,
            LastLoginAtUtc: null,
            Roles: ["Admin"],
            AiInputTokenLimit: 1000,
            AiOutputTokenLimit: 2000,
            AiConsentAcceptedAt: null);

}
