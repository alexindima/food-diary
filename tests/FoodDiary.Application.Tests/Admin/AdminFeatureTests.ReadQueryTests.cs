using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;
using FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;
using FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;
using FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;
using FoodDiary.Application.Admin.Queries.GetAdminContentReports;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;
using FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;
using FoodDiary.Application.Admin.Queries.GetAdminUser;
using FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FluentValidation.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

public partial class AdminFeatureTests {

    [Fact]
    public async Task GetAdminBillingPaymentsHandler_NormalizesFiltersAndReturnsPagedPayments() {
        var repository = new RecordingAdminBillingRepository();
        var payment = new AdminBillingPaymentReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "buyer@example.com",
            Guid.NewGuid(),
            "Paddle",
            "pay_123",
            "cus_123",
            "sub_123",
            "pm_123",
            "price_monthly",
            "monthly",
            "paid",
            "webhook",
            7.99m,
            "USD",
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow,
            "evt_123",
            "{\"ok\":true}",
            DateTime.UtcNow,
            ModifiedOnUtc: null);
        repository.PaymentsResponse = ([payment], 41);
        var handler = new GetAdminBillingPaymentsQueryHandler(new AdminBillingReadService(repository));

        Result<PagedResponse<AdminBillingPaymentReadModel>> result = await handler.Handle(
            new GetAdminBillingPaymentsQuery(
                Page: 0,
                Limit: 999,
                Provider: " paddle ",
                Status: " Paid ",
                Kind: " Webhook ",
                Search: " buyer@example.com ",
                FromUtc: new DateTime(2026, 4, 1),
                ToUtc: new DateTime(2026, 4, 30, 23, 59, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(payment, Assert.Single(result.Value.Data));
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.Limit);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal("Paddle", repository.LastPaymentsFilter?.Provider);
        Assert.Equal("paid", repository.LastPaymentsFilter?.Status);
        Assert.Equal("webhook", repository.LastPaymentsFilter?.Kind);
        Assert.Equal("buyer@example.com", repository.LastPaymentsFilter?.Search);
        Assert.Equal(DateTimeKind.Utc, repository.LastPaymentsFilter?.FromUtc?.Kind);
    }


    [Theory]
    [InlineData(" ", null)]
    [InlineData(" stripe ", "Stripe")]
    [InlineData(" custom-provider ", "custom-provider")]
    public async Task GetAdminBillingPaymentsHandler_NormalizesProviderFilter(string? provider, string? expectedProvider) {
        var repository = new RecordingAdminBillingRepository();
        var handler = new GetAdminBillingPaymentsQueryHandler(new AdminBillingReadService(repository));

        Result<PagedResponse<AdminBillingPaymentReadModel>> result = await handler.Handle(
            new GetAdminBillingPaymentsQuery(1, 20, provider, Status: null, Kind: null, Search: null, FromUtc: null, ToUtc: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedProvider, repository.LastPaymentsFilter?.Provider);
    }


    [Fact]
    public async Task GetAdminBillingSubscriptionsHandler_UsesSubscriptionRepositoryPath() {
        var repository = new RecordingAdminBillingRepository();
        var subscription = new AdminBillingSubscriptionReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "premium@example.com",
            "YooKassa",
            "customer_123",
            "payment_123",
            "pm_123",
            "price_yearly",
            "yearly",
            "active",
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow,
            CancelAtPeriodEnd: false,
            DateTime.UtcNow,
            "evt_123",
            DateTime.UtcNow,
            DateTime.UtcNow,
            ModifiedOnUtc: null);
        repository.SubscriptionsResponse = ([subscription], 1);
        var handler = new GetAdminBillingSubscriptionsQueryHandler(new AdminBillingReadService(repository));

        Result<PagedResponse<AdminBillingSubscriptionReadModel>> result = await handler.Handle(
            new GetAdminBillingSubscriptionsQuery(1, 20, "yookassa", "Active", Search: null, FromUtc: null, ToUtc: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(subscription, Assert.Single(result.Value.Data));
        Assert.Equal("YooKassa", repository.LastSubscriptionsFilter?.Provider);
        Assert.Equal("active", repository.LastSubscriptionsFilter?.Status);
    }


    [Fact]
    public async Task GetAdminBillingWebhookEventsHandler_UsesWebhookRepositoryPath() {
        var repository = new RecordingAdminBillingRepository();
        var webhookEvent = new AdminBillingWebhookEventReadModel(
            Guid.NewGuid(),
            "Paddle",
            "evt_123",
            "transaction.completed",
            "pay_123",
            "processed",
            DateTime.UtcNow,
            "{\"event_id\":\"evt_123\"}",
            ErrorMessage: null,
            DateTime.UtcNow,
            ModifiedOnUtc: null);
        repository.WebhookEventsResponse = ([webhookEvent], 1);
        var handler = new GetAdminBillingWebhookEventsQueryHandler(new AdminBillingReadService(repository));

        Result<PagedResponse<AdminBillingWebhookEventReadModel>> result = await handler.Handle(
            new GetAdminBillingWebhookEventsQuery(1, 20, "paddle", "Processed", "evt_123", FromUtc: null, ToUtc: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(webhookEvent, Assert.Single(result.Value.Data));
        Assert.Equal("Paddle", repository.LastWebhookEventsFilter?.Provider);
        Assert.Equal("processed", repository.LastWebhookEventsFilter?.Status);
        Assert.Equal("evt_123", repository.LastWebhookEventsFilter?.Search);
    }


    [Fact]
    public async Task AdminUserReadService_ExistsIncludingDeletedAsync_UsesLookupRepository() {
        User user = CreateUserWithRoles("admin-read-exists@example.com", [RoleNames.Admin]);
        IUserLookupRepository lookupRepository = Substitute.For<IUserLookupRepository>();
        lookupRepository.GetByIdIncludingDeletedAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        var service = new AdminUserReadService(lookupRepository, Substitute.For<IUserAdminReadModelRepository>());

        bool exists = await service.ExistsIncludingDeletedAsync(user.Id, CancellationToken.None);

        Assert.True(exists);
    }


    [Fact]
    public async Task GetAdminAiPromptsQueryHandler_ReturnsTemplates() {
        var template = AiPromptTemplate.Create("meal_summary", "en", "Prompt text", isActive: true);
        GetAdminAiPromptsQueryHandler handler = new(CreateAdminContentReadService(
            aiPromptTemplateRepository: new InMemoryAiPromptTemplateRepository(template)));

        Result<IReadOnlyList<AdminAiPromptModel>> result = await handler.Handle(new GetAdminAiPromptsQuery(), CancellationToken.None);

        ResultAssert.Success(result);
        AdminAiPromptModel model = Assert.Single(result.Value);
        Assert.Equal(template.Id.Value, model.Id);
        Assert.Equal("meal_summary", model.Key);
        Assert.Equal("en", model.Locale);
        Assert.Equal("Prompt text", model.PromptText);
        Assert.Equal(1, model.Version);
        Assert.True(model.IsActive);
    }


    [Fact]
    public async Task GetAdminAiUsageSummaryQueryValidator_WithInvalidRange_Fails() {
        var validator = new GetAdminAiUsageSummaryQueryValidator();

        ValidationResult result = await validator.ValidateAsync(
            new GetAdminAiUsageSummaryQuery(
                From: new DateOnly(2026, 2, 10),
                To: new DateOnly(2026, 2, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("From", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public async Task GetAdminAiUsageSummaryQueryValidator_WithValidRange_Passes() {
        var validator = new GetAdminAiUsageSummaryQueryValidator();

        ValidationResult result = await validator.ValidateAsync(
            new GetAdminAiUsageSummaryQuery(
                From: new DateOnly(2026, 2, 1),
                To: new DateOnly(2026, 2, 10)));

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task GetAdminAiUsageSummaryQueryHandler_WithInvertedRange_ReturnsValidationFailure() {
        var repository = new RecordingAiUsageRepository();
        var handler = new GetAdminAiUsageSummaryQueryHandler(new AdminAiUsageReadService(
            repository,
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc))));

        Result<AdminAiUsageSummaryModel> result = await handler.Handle(
            new GetAdminAiUsageSummaryQuery(new DateOnly(2026, 4, 1), new DateOnly(2026, 3, 1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal(default, repository.LastFromUtc);
        Assert.Equal(default, repository.LastToUtc);
    }


    [Fact]
    public async Task GetAdminAiUsageSummaryQueryHandler_UsesDateTimeProviderForDefaultRange() {
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc));
        var aiUsageRepository = new RecordingAiUsageRepository();
        var handler = new GetAdminAiUsageSummaryQueryHandler(new AdminAiUsageReadService(aiUsageRepository, dateTimeProvider));

        Result<AdminAiUsageSummaryModel> result = await handler.Handle(new GetAdminAiUsageSummaryQuery(From: null, To: null), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), aiUsageRepository.LastFromUtc);
        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), aiUsageRepository.LastToUtc);
    }


    [Fact]
    public async Task GetAdminAiUsageSummaryQueryHandler_MapsDailyBreakdownAndUserSummaries() {
        var userId = UserId.New();
        var summary = new AiUsageSummary(
            TotalTokens: 100,
            InputTokens: 40,
            OutputTokens: 60,
            ByDay: [new AiUsageDailySummary(new DateOnly(2026, 3, 26), 10, 4, 6)],
            ByOperation: [new AiUsageBreakdown("vision", 20, 8, 12)],
            ByModel: [new AiUsageBreakdown("gpt-test", 30, 12, 18)],
            ByUser: [new AiUsageUserSummary(userId, "user@example.com", 40, 16, 24)]);
        var handler = new GetAdminAiUsageSummaryQueryHandler(new AdminAiUsageReadService(
            new RecordingAiUsageRepository(summary),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc))));

        Result<AdminAiUsageSummaryModel> result = await handler.Handle(
            new GetAdminAiUsageSummaryQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(100, result.Value.TotalTokens);
        AdminAiUsageDailyModel daily = Assert.Single(result.Value.ByDay);
        Assert.Equal(new DateOnly(2026, 3, 26), daily.Date);
        Assert.Equal(10, daily.TotalTokens);
        Assert.Equal("vision", Assert.Single(result.Value.ByOperation).Key);
        Assert.Equal("gpt-test", Assert.Single(result.Value.ByModel).Key);
        AdminAiUsageUserModel user = Assert.Single(result.Value.ByUser);
        Assert.Equal(userId.Value, user.Id);
        Assert.Equal("user@example.com", user.Email);
    }


    [Fact]
    public async Task GetAdminUserQueryHandler_WithEmptyUserId_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var handler = new GetAdminUserQueryHandler(new InMemoryUserRepository(user, [RoleNames.Admin]));

        Result<AdminUserModel> result = await handler.Handle(new GetAdminUserQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }


    [Fact]
    public async Task GetAdminUserQueryHandler_WhenUserMissing_ReturnsNotFound() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var handler = new GetAdminUserQueryHandler(new InMemoryUserRepository(user, [RoleNames.Admin]));

        Result<AdminUserModel> result = await handler.Handle(new GetAdminUserQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }


    [Fact]
    public async Task GetAdminUserQueryHandler_WithExistingUser_ReturnsAdminModel() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var handler = new GetAdminUserQueryHandler(new InMemoryUserRepository(user, [RoleNames.Admin]));

        Result<AdminUserModel> result = await handler.Handle(new GetAdminUserQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(user.Id.Value, result.Value.Id);
        Assert.Equal("admin@example.com", result.Value.Email);
        Assert.Contains(RoleNames.Admin, result.Value.Roles);
    }


    [Fact]
    public async Task GetAdminMailInboxMessagesQueryHandler_ReturnsReaderMessages() {
        var message = new AdminMailInboxMessageSummaryModel(
            Guid.NewGuid(),
            "sender@example.com",
            ["recipient@example.com"],
            "Subject",
            "general",
            "Received",
            ReadAtUtc: null,
            DateTimeOffset.UtcNow);
        var reader = new RecordingAdminMailInboxReader { Messages = [message] };
        var handler = new GetAdminMailInboxMessagesQueryHandler(reader);

        Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>> result = await handler.Handle(new GetAdminMailInboxMessagesQuery(25), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(message, Assert.Single(result.Value));
        Assert.Equal(25, reader.LastLimit);
    }


    [Fact]
    public async Task GetAdminMailInboxMessageDetailsQueryHandler_WhenMessageMissing_ReturnsNotFound() {
        var messageId = Guid.NewGuid();
        var handler = new GetAdminMailInboxMessageDetailsQueryHandler(new RecordingAdminMailInboxReader());

        Result<AdminMailInboxMessageDetailsModel> result = await handler.Handle(new GetAdminMailInboxMessageDetailsQuery(messageId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("MailInbox.MessageNotFound", result.Error.Code);
    }


    [Fact]
    public async Task GetAdminMailInboxMessageDetailsQueryHandler_WithExistingMessage_ReturnsDetails() {
        var message = new AdminMailInboxMessageDetailsModel(
            Guid.NewGuid(),
            "mail-message-id",
            "sender@example.com",
            ["recipient@example.com"],
            "Subject",
            "Text",
            "<p>Text</p>",
            "raw",
            "general",
            "Received",
            ReadAtUtc: null,
            DateTimeOffset.UtcNow);
        var reader = new RecordingAdminMailInboxReader { Message = message };
        var handler = new GetAdminMailInboxMessageDetailsQueryHandler(reader);

        Result<AdminMailInboxMessageDetailsModel> result = await handler.Handle(new GetAdminMailInboxMessageDetailsQuery(message.Id), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(message, result.Value);
        Assert.Equal(message.Id, reader.LastMessageId);
    }


    [Fact]
    public async Task GetAdminDashboardSummaryQueryHandler_ReturnsSummaryWithPendingReports() {
        User recentUser = CreateUserWithRoles("recent@example.com", [RoleNames.Premium]);
        var userRepository = new SummaryUserRepository((12, 10, 3, 1, [recentUser]));
        var contentReportRepository = new CountingContentReportRepository(4);
        var handler = new GetAdminDashboardSummaryQueryHandler(new AdminDashboardReadService(userRepository, contentReportRepository));

        Result<AdminDashboardSummaryModel> result = await handler.Handle(new GetAdminDashboardSummaryQuery(2), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(12, result.Value.TotalUsers);
        Assert.Equal(10, result.Value.ActiveUsers);
        Assert.Equal(3, result.Value.PremiumUsers);
        Assert.Equal(1, result.Value.DeletedUsers);
        Assert.Equal(4, result.Value.PendingReportsCount);
        Assert.Equal("recent@example.com", Assert.Single(result.Value.RecentUsers).Email);
        Assert.Equal(2, userRepository.LastRecentLimit);
    }


    [Fact]
    public async Task GetAdminContentReportsQueryHandler_NormalizesPagingAndMapsReports() {
        var report = ContentReport.Create(
            UserId.New(),
            ReportTargetType.Recipe,
            Guid.NewGuid(),
            "Incorrect content");
        report.MarkDismissed("  resolved  ");
        var repository = new CountingContentReportRepository(0, [report]);
        GetAdminContentReportsQueryHandler handler = new(CreateAdminContentReadService(contentReportRepository: repository));

        Result<PagedResponse<AdminContentReportModel>> result = await handler.Handle(new GetAdminContentReportsQuery("dismissed", 0, 0), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(ReportStatus.Dismissed, repository.LastStatus);
        AdminContentReportModel model = Assert.Single(result.Value.Data);
        Assert.Equal(report.Id.Value, model.Id);
        Assert.Equal(nameof(ReportStatus.Dismissed), model.Status);
        Assert.Equal("resolved", model.AdminNote);
    }


    [Fact]
    public async Task GetAdminEmailTemplatesQueryHandler_ReturnsTemplates() {
        var template = EmailTemplate.Create(
            "verify_email",
            "en",
            "Subject",
            "<b>Body</b>",
            "Body",
            isActive: true);
        GetAdminEmailTemplatesQueryHandler handler = new(CreateAdminContentReadService(
            emailTemplateRepository: new InMemoryEmailTemplateRepository(template)));

        Result<IReadOnlyList<AdminEmailTemplateModel>> result = await handler.Handle(new GetAdminEmailTemplatesQuery(), CancellationToken.None);

        ResultAssert.Success(result);
        AdminEmailTemplateModel model = Assert.Single(result.Value);
        Assert.Equal(template.Id, model.Id);
        Assert.Equal("verify_email", model.Key);
        Assert.Equal("Subject", model.Subject);
        Assert.True(model.IsActive);
    }


    [Fact]
    public async Task GetAdminImpersonationSessionsQueryHandler_NormalizesPagingAndReturnsPagedResponse() {
        var session = new AdminImpersonationSessionReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "admin@example.com",
            Guid.NewGuid(),
            "target@example.com",
            "Support",
            "127.0.0.1",
            "Test",
            DateTime.UtcNow);
        var repository = new RecordingImpersonationSessionRepository {
            PagedResponse = ([session], 45),
        };
        var handler = new GetAdminImpersonationSessionsQueryHandler(new AdminAuditReadService(
            Substitute.For<IAdminUserReadService>(),
            Substitute.For<IAdminUserRoleAuditReadRepository>(),
            repository));

        Result<PagedResponse<AdminImpersonationSessionReadModel>> result = await handler.Handle(new GetAdminImpersonationSessionsQuery(0, 999, " target "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(session, Assert.Single(result.Value.Data));
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.Limit);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(" target ", repository.LastSearch);
    }


    [Fact]
    public async Task GetAdminUserRoleAuditQueryHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new GetAdminUserRoleAuditQueryHandler(new AdminAuditReadService(
            new InMemoryUserRepository(CreateUserWithRoles("admin@example.com", []), []),
            new RecordingUserRoleAuditRepository(),
            Substitute.For<IAdminImpersonationSessionReadRepository>()));

        Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>> result = await handler.Handle(new GetAdminUserRoleAuditQuery(Guid.Empty, 10), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }


    [Fact]
    public async Task GetAdminUserRoleAuditQueryHandler_WhenUserMissing_ReturnsNotFound() {
        var handler = new GetAdminUserRoleAuditQueryHandler(new AdminAuditReadService(
            new InMemoryUserRepository(CreateUserWithRoles("admin@example.com", []), []),
            new RecordingUserRoleAuditRepository(),
            Substitute.For<IAdminImpersonationSessionReadRepository>()));

        Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>> result = await handler.Handle(new GetAdminUserRoleAuditQuery(Guid.NewGuid(), 10), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }


    [Fact]
    public async Task GetAdminUserRoleAuditQueryHandler_WithExistingUser_ClampsLimitAndReturnsEvents() {
        User user = CreateUserWithRoles("role-audit@example.com", [RoleNames.Admin]);
        var auditEvent = new AdminUserRoleAuditEventReadModel(
            Guid.NewGuid(),
            user.Id.Value,
            RoleNames.Admin,
            UserRoleAuditAction.Added.ToString(),
            ActorUserId: null,
            ActorEmail: null,
            "test",
            DateTime.UtcNow);
        var repository = new RecordingUserRoleAuditRepository([auditEvent]);
        var handler = new GetAdminUserRoleAuditQueryHandler(new AdminAuditReadService(
            new InMemoryUserRepository(user, [RoleNames.Admin]),
            repository,
            Substitute.For<IAdminImpersonationSessionReadRepository>()));

        Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>> result = await handler.Handle(new GetAdminUserRoleAuditQuery(user.Id.Value, 999), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(auditEvent, Assert.Single(result.Value));
        Assert.Equal(50, repository.LastLimit);
    }

}
