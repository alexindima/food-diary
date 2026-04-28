using System.Net.Mail;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;
using FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;
using FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Application.Tests.Admin;

public class AdminFeatureTests {
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
            null);
        repository.PaymentsResponse = ([payment], 41);
        var handler = new GetAdminBillingPaymentsQueryHandler(repository);

        var result = await handler.Handle(
            new GetAdminBillingPaymentsQuery(
                Page: 0,
                Limit: 999,
                Provider: " Paddle ",
                Status: " paid ",
                Kind: " webhook ",
                Search: " buyer@example.com ",
                FromUtc: new DateTime(2026, 4, 1),
                ToUtc: new DateTime(2026, 4, 30, 23, 59, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            false,
            DateTime.UtcNow,
            "evt_123",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null);
        repository.SubscriptionsResponse = ([subscription], 1);
        var handler = new GetAdminBillingSubscriptionsQueryHandler(repository);

        var result = await handler.Handle(
            new GetAdminBillingSubscriptionsQuery(1, 20, "YooKassa", "active", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            null,
            DateTime.UtcNow,
            null);
        repository.WebhookEventsResponse = ([webhookEvent], 1);
        var handler = new GetAdminBillingWebhookEventsQueryHandler(repository);

        var result = await handler.Handle(
            new GetAdminBillingWebhookEventsQuery(1, 20, "Paddle", "processed", "evt_123", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(webhookEvent, Assert.Single(result.Value.Data));
        Assert.Equal("Paddle", repository.LastWebhookEventsFilter?.Provider);
        Assert.Equal("processed", repository.LastWebhookEventsFilter?.Status);
        Assert.Equal("evt_123", repository.LastWebhookEventsFilter?.Search);
    }

    [Fact]
    public async Task UpdateAdminUserValidator_WithInvalidRole_Fails() {
        var validator = new UpdateAdminUserCommandValidator();
        var command = new UpdateAdminUserCommand(
            Guid.NewGuid(),
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: ["Unknown"],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Unknown role.");
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithUnknownRoleFromRepository_Fails() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());
        var command = new UpdateAdminUserCommand(
            user.Id.Value,
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: [RoleNames.Admin, RoleNames.Support],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("roles", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                Guid.Empty,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithNullRoles_DoesNotChangeRoles() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());
        var beforeRoles = user.UserRoles.Select(r => r.Role.Name).OrderBy(x => x).ToArray();

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        var afterRoles = user.UserRoles.Select(r => r.Role.Name).OrderBy(x => x).ToArray();

        Assert.True(result.IsSuccess);
        Assert.Equal(beforeRoles, afterRoles);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyRoles_ClearsRoles() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: Array.Empty<string>(),
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithSameRoles_DoesNotSetModifiedOnUtc() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());
        var modifiedBefore = user.ModifiedOnUtc;

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Premium, RoleNames.Admin],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(modifiedBefore, user.ModifiedOnUtc);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithUnchangedAdminAccountFields_DoesNotSetModifiedOnUtc() {
        var user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        user.SetEmailConfirmed(true);
        user.SetLanguage("en");
        user.UpdateAiTokenLimits(new FoodDiary.Domain.ValueObjects.UserAiTokenLimitUpdate(
            InputLimit: 123,
            OutputLimit: 456));
        var modifiedBefore = user.ModifiedOnUtc;
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: true,
                Roles: null,
                Language: "en",
                AiInputTokenLimit: 123,
                AiOutputTokenLimit: 456),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(modifiedBefore, user.ModifiedOnUtc);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithDeletedUserAndActiveToggle_ReturnsValidationFailure() {
        var user = CreateUserWithRoles("deleted-admin@example.com", [RoleNames.Admin]);
        user.DeleteAccount(DateTime.UtcNow);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(userRepository, new NullAuditLogger());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: true,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("restore flow", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpsertAdminEmailTemplateValidator_WithInvalidLocale_Fails() {
        var validator = new UpsertAdminEmailTemplateCommandValidator();
        var command = new UpsertAdminEmailTemplateCommand(
            Key: "verify_email",
            Locale: "de",
            Subject: "Subject",
            HtmlBody: "<b>Body</b>",
            TextBody: "Body",
            IsActive: true);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("supported codes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpsertAdminEmailTemplateHandler_WithInvalidLocale_ReturnsValidationFailure() {
        var handler = new UpsertAdminEmailTemplateCommandHandler(new InMemoryEmailTemplateRepository());

        var result = await handler.Handle(
            new UpsertAdminEmailTemplateCommand(
                Key: "verify_email",
                Locale: "de",
                Subject: "Subject",
                HtmlBody: "<b>Body</b>",
                TextBody: "Body",
                IsActive: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("supported codes", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAdminEmailTemplateTestHandler_SendsCurrentTemplateToRequestedRecipient() {
        var transport = new RecordingEmailTransport();
        var handler = new SendAdminEmailTemplateTestCommandHandler(
            new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary"
            },
            transport);

        var result = await handler.Handle(
            new SendAdminEmailTemplateTestCommand(
                ToEmail: "admin@example.com",
                Key: "dietologist_invitation",
                Subject: "Hello {{clientName}}",
                HtmlBody: "<a href=\"{{link}}\">{{brand}}</a>",
                TextBody: "{{clientName}} on {{brand}}: {{link}}"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("admin@example.com", transport.ToEmail);
        Assert.Equal("Hello Alex Johnson", transport.Subject);
        Assert.Equal("<a href=\"https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo\">FoodDiary</a>", transport.Body);
        Assert.Contains(
            "Alex Johnson on FoodDiary: https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo",
            transport.AlternateViewBodies);
    }

    [Fact]
    public async Task GetAdminAiUsageSummaryQueryValidator_WithInvalidRange_Fails() {
        var validator = new GetAdminAiUsageSummaryQueryValidator();

        var result = await validator.ValidateAsync(
            new GetAdminAiUsageSummaryQuery(
                From: new DateOnly(2026, 2, 10),
                To: new DateOnly(2026, 2, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("From", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAdminAiUsageSummaryQueryValidator_WithValidRange_Passes() {
        var validator = new GetAdminAiUsageSummaryQueryValidator();

        var result = await validator.ValidateAsync(
            new GetAdminAiUsageSummaryQuery(
                From: new DateOnly(2026, 2, 1),
                To: new DateOnly(2026, 2, 10)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetAdminAiUsageSummaryQueryHandler_UsesDateTimeProviderForDefaultRange() {
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc));
        var aiUsageRepository = new RecordingAiUsageRepository();
        var handler = new GetAdminAiUsageSummaryQueryHandler(aiUsageRepository, dateTimeProvider);

        var result = await handler.Handle(new GetAdminAiUsageSummaryQuery(null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc), aiUsageRepository.LastFromUtc);
        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), aiUsageRepository.LastToUtc);
    }

    private static User CreateUserWithRoles(string email, IReadOnlyList<string> roleNames) {
        var user = User.Create(email, "hash");
        var roles = roleNames.Select(name => Role.Create(name)).ToArray();
        user.ReplaceRoles(roles);
        return user;
    }

    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType, string? targetId, string? details) { }
    }

    private sealed class InMemoryUserRepository : IUserRepository {
        private readonly User _user;
        private readonly Dictionary<string, Role> _roles;

        public InMemoryUserRepository(User user, IEnumerable<string> availableRoles) {
            _user = user;
            _roles = availableRoles.ToDictionary(
                name => name,
                name => user.UserRoles
                    .Select(userRole => userRole.Role)
                    .FirstOrDefault(role => string.Equals(role.Name, name, StringComparison.Ordinal))
                    ?? Role.Create(name),
                StringComparer.Ordinal);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(_user.Id == id ? _user : null);

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(_user.Id == id ? _user : null);

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
            var found = names.Where(name => _roles.ContainsKey(name)).Select(name => _roles[name]).ToList();
            return Task.FromResult<IReadOnlyList<Role>>(found);
        }

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingAiUsageRepository : IAiUsageRepository {
        public DateTime LastFromUtc { get; private set; }
        public DateTime LastToUtc { get; private set; }

        public Task AddAsync(FoodDiary.Domain.Entities.Ai.AiUsage usage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FoodDiary.Application.Abstractions.Admin.Models.AiUsageSummary> GetSummaryAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) {
            LastFromUtc = fromUtc;
            LastToUtc = toUtc;

            return Task.FromResult(new FoodDiary.Application.Abstractions.Admin.Models.AiUsageSummary(
                0,
                0,
                0,
                [],
                [],
                [],
                []));
        }

        public Task<AiUsageTotals> GetUserTotalsAsync(
            UserId userId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow => utcNow;
    }

    private sealed class RecordingEmailTransport : IEmailTransport {
        public string? ToEmail { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public List<string> AlternateViewBodies { get; } = [];

        public async Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
            ToEmail = message.To.Single().Address;
            Subject = message.Subject;
            Body = message.Body;

            foreach (var view in message.AlternateViews) {
                using var reader = new StreamReader(view.ContentStream);
                AlternateViewBodies.Add(await reader.ReadToEndAsync(cancellationToken));
            }
        }
    }

    private sealed class InMemoryEmailTemplateRepository : IEmailTemplateRepository {
        public Task<EmailTemplate> UpsertAsync(
            string key,
            string locale,
            string subject,
            string htmlBody,
            string textBody,
            bool isActive,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(EmailTemplate.Create(
                key,
                locale,
                subject,
                htmlBody,
                textBody,
                isActive));

        public Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmailTemplate>>([]);

        public Task<EmailTemplate?> GetByKeyAsync(
            string key,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmailTemplate?>(null);
    }

    private sealed class RecordingAdminBillingRepository : IAdminBillingRepository {
        public AdminBillingListFilter? LastSubscriptionsFilter { get; private set; }
        public AdminBillingListFilter? LastPaymentsFilter { get; private set; }
        public AdminBillingListFilter? LastWebhookEventsFilter { get; private set; }

        public (IReadOnlyList<AdminBillingSubscriptionReadModel> Items, int TotalItems) SubscriptionsResponse { get; set; } = ([], 0);
        public (IReadOnlyList<AdminBillingPaymentReadModel> Items, int TotalItems) PaymentsResponse { get; set; } = ([], 0);
        public (IReadOnlyList<AdminBillingWebhookEventReadModel> Items, int TotalItems) WebhookEventsResponse { get; set; } = ([], 0);

        public Task<(IReadOnlyList<AdminBillingSubscriptionReadModel> Items, int TotalItems)> GetSubscriptionsAsync(
            AdminBillingListFilter filter,
            CancellationToken cancellationToken = default) {
            LastSubscriptionsFilter = filter;
            return Task.FromResult(SubscriptionsResponse);
        }

        public Task<(IReadOnlyList<AdminBillingPaymentReadModel> Items, int TotalItems)> GetPaymentsAsync(
            AdminBillingListFilter filter,
            CancellationToken cancellationToken = default) {
            LastPaymentsFilter = filter;
            return Task.FromResult(PaymentsResponse);
        }

        public Task<(IReadOnlyList<AdminBillingWebhookEventReadModel> Items, int TotalItems)> GetWebhookEventsAsync(
            AdminBillingListFilter filter,
            CancellationToken cancellationToken = default) {
            LastWebhookEventsFilter = filter;
            return Task.FromResult(WebhookEventsResponse);
        }
    }
}
