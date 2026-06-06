using System.Net.Mail;
using FoodDiary.Application.Admin.Commands.DismissContentReport;
using FoodDiary.Application.Admin.Commands.ReviewContentReport;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
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
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FluentValidation.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
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

    [Theory]
    [InlineData(" ", null)]
    [InlineData(" stripe ", "Stripe")]
    [InlineData(" custom-provider ", "custom-provider")]
    public async Task GetAdminBillingPaymentsHandler_NormalizesProviderFilter(string? provider, string? expectedProvider) {
        var repository = new RecordingAdminBillingRepository();
        var handler = new GetAdminBillingPaymentsQueryHandler(repository);

        Result<PagedResponse<AdminBillingPaymentReadModel>> result = await handler.Handle(
            new GetAdminBillingPaymentsQuery(1, 20, provider, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            false,
            DateTime.UtcNow,
            "evt_123",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null);
        repository.SubscriptionsResponse = ([subscription], 1);
        var handler = new GetAdminBillingSubscriptionsQueryHandler(repository);

        Result<PagedResponse<AdminBillingSubscriptionReadModel>> result = await handler.Handle(
            new GetAdminBillingSubscriptionsQuery(1, 20, "yookassa", "Active", null, null, null),
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

        Result<PagedResponse<AdminBillingWebhookEventReadModel>> result = await handler.Handle(
            new GetAdminBillingWebhookEventsQuery(1, 20, "paddle", "Processed", "evt_123", null, null),
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

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Unknown role.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithUnknownRoleFromRepository_Fails() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);
        var command = new UpdateAdminUserCommand(
            user.Id.Value,
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: [RoleNames.Admin, RoleNames.Support],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        Result<AdminUserModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("roles", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyUserId_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
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
    public async Task UpdateAdminUserHandler_WhenUserMissing_ReturnsNotFound() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                Guid.NewGuid(),
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithInvalidLanguage_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: "klingon",
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("language", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithUnknownRequestedRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: ["MysteryRole"],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown role", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithNullRoles_DoesNotChangeRoles() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);
        string[] beforeRoles = user.UserRoles.Select(r => r.Role.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        string[] afterRoles = user.UserRoles.Select(r => r.Role.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.True(result.IsSuccess);
        Assert.Equal(beforeRoles, afterRoles);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyRoles_ClearsRoles() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
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
    public async Task UpdateAdminUserHandler_WithOwnerRoleForNonOwner_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Owner, RoleNames.Admin],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner role", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin], user.GetRoleNames().OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserWithoutOwnerRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Admin],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner and Admin", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin, RoleNames.Owner], user.GetRoleNames().OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserWithoutAdminRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Owner],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner and Admin", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin, RoleNames.Owner], user.GetRoleNames().OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserKeepingOwnerAndAdmin_UpdatesRoles() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Support],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [RoleNames.Admin, RoleNames.Owner, RoleNames.Support],
            user.GetRoleNames().OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserDeactivation_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: false,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner user", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WhenActorRemovesOwnAdminRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Premium],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: user.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("own Admin role", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin, RoleNames.Premium], user.GetRoleNames().OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WhenActorDeactivatesOwnAccount_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: false,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: user.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("own account", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithSameRoles_DoesNotSetModifiedOnUtc() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);
        DateTime? modifiedBefore = user.ModifiedOnUtc;

        Result<AdminUserModel> result = await handler.Handle(
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
    public async Task UpdateAdminUserHandler_WithRoleChanges_StoresRoleAuditEvents() {
        var actorUserId = UserId.New();
        var timestamp = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc);
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            new NullAuditLogger(),
            new FixedDateTimeProvider(timestamp));

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Admin, RoleNames.Support],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: actorUserId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            userRepository.RoleAuditEvents.OrderBy(auditEvent => auditEvent.RoleName, StringComparer.Ordinal),
            auditEvent => {
                Assert.Equal(UserRoleAuditAction.Removed, auditEvent.Action);
                Assert.Equal(RoleNames.Premium, auditEvent.RoleName);
                Assert.Equal(actorUserId, auditEvent.ActorUserId);
                Assert.Equal(timestamp, auditEvent.OccurredAtUtc);
            },
            auditEvent => {
                Assert.Equal(UserRoleAuditAction.Added, auditEvent.Action);
                Assert.Equal(RoleNames.Support, auditEvent.RoleName);
                Assert.Equal(actorUserId, auditEvent.ActorUserId);
                Assert.Equal(timestamp, auditEvent.OccurredAtUtc);
            });
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithUnchangedAdminAccountFields_DoesNotSetModifiedOnUtc() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        user.SetEmailConfirmed(true);
        user.SetLanguage("en");
        user.UpdateAiTokenLimits(new FoodDiary.Domain.ValueObjects.UserAiTokenLimitUpdate(
            InputLimit: 123,
            OutputLimit: 456));
        DateTime? modifiedBefore = user.ModifiedOnUtc;
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
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
        User user = CreateUserWithRoles("deleted-admin@example.com", [RoleNames.Admin]);
        user.DeleteAccount(DateTime.UtcNow);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
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
    public async Task UpdateAdminUserHandler_WithActiveToggleTrue_ActivatesUser() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        user.Deactivate();
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: true,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task UpdateAdminUserHandler_WithActiveToggleFalse_DeactivatesUser() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: false,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithInactiveActor_ReturnsForbidden() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        actor.Deactivate();
        User target = CreateUserWithRoles("client@example.com", []);
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                target.Id.Value,
                "Support case",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.ImpersonationForbidden", result.Error.Code);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithEmptyActorUserId_ReturnsValidationFailure() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("client@example.com", []);
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                Guid.Empty,
                target.Id.Value,
                "Support case",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ActorUserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithEmptyTargetUserId_ReturnsValidationFailure() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("client@example.com", []);
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                Guid.Empty,
                "Support case",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("TargetUserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithInactiveTarget_ReturnsForbiddenWithoutSession() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("client@example.com", []);
        target.Deactivate();
        var sessionRepository = new RecordingImpersonationSessionRepository();
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target, sessionRepository);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                target.Id.Value,
                "Support case",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.ImpersonationForbidden", result.Error.Code);
        Assert.Equal(0, sessionRepository.AddCallCount);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithSameActorAndTarget_ReturnsValidationFailure() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, actor);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                actor.Id.Value,
                "Support case",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WhenTargetMissing_ReturnsNotFound() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("client@example.com", []);
        var missingTargetId = Guid.NewGuid();
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                missingTargetId,
                "Support case",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithValidRequest_CreatesSessionAndReturnsToken() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("client@example.com", [RoleNames.Premium]);
        var sessionRepository = new RecordingImpersonationSessionRepository();
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target, sessionRepository);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                target.Id.Value,
                "  Support case with billing issue  ",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("impersonation-token", result.Value.AccessToken);
        Assert.Equal(target.Id.Value, result.Value.TargetUserId);
        Assert.Equal(actor.Id.Value, result.Value.ActorUserId);
        Assert.Equal("Support case with billing issue", result.Value.Reason);
        Assert.Equal(1, sessionRepository.AddCallCount);
        Assert.Equal(target.Id, sessionRepository.LastSession?.TargetUserId);
        Assert.Equal(actor.Id, sessionRepository.LastSession?.ActorUserId);
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

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("supported codes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpsertAdminEmailTemplateHandler_WithInvalidLocale_ReturnsValidationFailure() {
        var handler = new UpsertAdminEmailTemplateCommandHandler(new InMemoryEmailTemplateRepository());

        Result<AdminEmailTemplateModel> result = await handler.Handle(
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
    public async Task UpsertAdminEmailTemplateHandler_WithValidCommand_UpsertsTemplate() {
        var handler = new UpsertAdminEmailTemplateCommandHandler(new InMemoryEmailTemplateRepository());

        Result<AdminEmailTemplateModel> result = await handler.Handle(
            new UpsertAdminEmailTemplateCommand(
                Key: " Verify_Email ",
                Locale: " EN ",
                Subject: "Subject",
                HtmlBody: "<b>Body</b>",
                TextBody: "Body",
                IsActive: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("verify_email", result.Value.Key);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal("Subject", result.Value.Subject);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task SendAdminEmailTemplateTestHandler_SendsCurrentTemplateToRequestedRecipient() {
        var transport = new RecordingEmailTransport();
        var handler = new SendAdminEmailTemplateTestCommandHandler(
            new EmailOptions {
                FromAddress = "noreply@example.com",
                FromName = "FoodDiary",
            },
            transport);

        Result result = await handler.Handle(
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
    public async Task UpsertAdminAiPromptHandler_WhenPromptMissing_CreatesTemplate() {
        var repository = new InMemoryAiPromptTemplateRepository();
        var handler = new UpsertAdminAiPromptCommandHandler(repository);

        Result<AdminAiPromptModel> result = await handler.Handle(
            new UpsertAdminAiPromptCommand(" Meal_Summary ", " EN ", " Prompt text ", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("meal_summary", result.Value.Key);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal("Prompt text", result.Value.PromptText);
        Assert.True(result.Value.IsActive);
        Assert.Single(repository.Templates);
    }

    [Fact]
    public async Task UpsertAdminAiPromptHandler_WhenPromptExists_UpdatesTrackedTemplate() {
        var template = AiPromptTemplate.Create("meal_summary", "en", "Old prompt", true);
        var repository = new InMemoryAiPromptTemplateRepository(template);
        var handler = new UpsertAdminAiPromptCommandHandler(repository);

        Result<AdminAiPromptModel> result = await handler.Handle(
            new UpsertAdminAiPromptCommand("MEAL_SUMMARY", "EN", "New prompt", false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(template.Id.Value, result.Value.Id);
        Assert.Equal("New prompt", result.Value.PromptText);
        Assert.False(result.Value.IsActive);
        Assert.Equal(2, result.Value.Version);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task GetAdminAiPromptsQueryHandler_ReturnsTemplates() {
        var template = AiPromptTemplate.Create("meal_summary", "en", "Prompt text", true);
        var handler = new GetAdminAiPromptsQueryHandler(new InMemoryAiPromptTemplateRepository(template));

        Result<IReadOnlyList<AdminAiPromptModel>> result = await handler.Handle(new GetAdminAiPromptsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        var handler = new GetAdminAiUsageSummaryQueryHandler(
            repository,
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)));

        Result<AdminAiUsageSummaryModel> result = await handler.Handle(
            new GetAdminAiUsageSummaryQuery(new DateOnly(2026, 4, 1), new DateOnly(2026, 3, 1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal(default, repository.LastFromUtc);
        Assert.Equal(default, repository.LastToUtc);
    }

    [Fact]
    public async Task GetAdminAiUsageSummaryQueryHandler_UsesDateTimeProviderForDefaultRange() {
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc));
        var aiUsageRepository = new RecordingAiUsageRepository();
        var handler = new GetAdminAiUsageSummaryQueryHandler(aiUsageRepository, dateTimeProvider);

        Result<AdminAiUsageSummaryModel> result = await handler.Handle(new GetAdminAiUsageSummaryQuery(null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        var handler = new GetAdminAiUsageSummaryQueryHandler(
            new RecordingAiUsageRepository(summary),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)));

        Result<AdminAiUsageSummaryModel> result = await handler.Handle(
            new GetAdminAiUsageSummaryQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetAdminUserQueryHandler_WhenUserMissing_ReturnsNotFound() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var handler = new GetAdminUserQueryHandler(new InMemoryUserRepository(user, [RoleNames.Admin]));

        Result<AdminUserModel> result = await handler.Handle(new GetAdminUserQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetAdminUserQueryHandler_WithExistingUser_ReturnsAdminModel() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var handler = new GetAdminUserQueryHandler(new InMemoryUserRepository(user, [RoleNames.Admin]));

        Result<AdminUserModel> result = await handler.Handle(new GetAdminUserQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            "Received",
            DateTimeOffset.UtcNow);
        var reader = new RecordingAdminMailInboxReader { Messages = [message] };
        var handler = new GetAdminMailInboxMessagesQueryHandler(reader);

        Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>> result = await handler.Handle(new GetAdminMailInboxMessagesQuery(25), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(message, Assert.Single(result.Value));
        Assert.Equal(25, reader.LastLimit);
    }

    [Fact]
    public async Task GetAdminMailInboxMessageDetailsQueryHandler_WhenMessageMissing_ReturnsNotFound() {
        var messageId = Guid.NewGuid();
        var handler = new GetAdminMailInboxMessageDetailsQueryHandler(new RecordingAdminMailInboxReader());

        Result<AdminMailInboxMessageDetailsModel> result = await handler.Handle(new GetAdminMailInboxMessageDetailsQuery(messageId), CancellationToken.None);

        Assert.True(result.IsFailure);
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
            "Received",
            DateTimeOffset.UtcNow);
        var reader = new RecordingAdminMailInboxReader { Message = message };
        var handler = new GetAdminMailInboxMessageDetailsQueryHandler(reader);

        Result<AdminMailInboxMessageDetailsModel> result = await handler.Handle(new GetAdminMailInboxMessageDetailsQuery(message.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(message, result.Value);
        Assert.Equal(message.Id, reader.LastMessageId);
    }

    [Fact]
    public async Task GetAdminDashboardSummaryQueryHandler_ReturnsSummaryWithPendingReports() {
        User recentUser = CreateUserWithRoles("recent@example.com", [RoleNames.Premium]);
        var userRepository = new SummaryUserRepository((12, 10, 3, 1, [recentUser]));
        var contentReportRepository = new CountingContentReportRepository(4);
        var handler = new GetAdminDashboardSummaryQueryHandler(userRepository, contentReportRepository);

        Result<AdminDashboardSummaryModel> result = await handler.Handle(new GetAdminDashboardSummaryQuery(2), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        var handler = new GetAdminContentReportsQueryHandler(repository);

        Result<PagedResponse<AdminContentReportModel>> result = await handler.Handle(new GetAdminContentReportsQuery("Dismissed", 0, 0), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(ReportStatus.Dismissed, repository.LastStatus);
        AdminContentReportModel model = Assert.Single(result.Value.Data);
        Assert.Equal(report.Id.Value, model.Id);
        Assert.Equal(nameof(ReportStatus.Dismissed), model.Status);
        Assert.Equal("resolved", model.AdminNote);
    }

    [Fact]
    public async Task ReviewContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new ReviewContentReportCommandHandler(new CountingContentReportRepository(0));
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new ReviewContentReportCommand(reportId, "handled"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ContentReport.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ReviewContentReportHandler_WithExistingReport_MarksReviewed() {
        var report = ContentReport.Create(
            UserId.New(),
            ReportTargetType.Recipe,
            Guid.NewGuid(),
            "Incorrect content");
        var repository = new CountingContentReportRepository(0, [report]);
        var handler = new ReviewContentReportCommandHandler(repository);

        Result result = await handler.Handle(new ReviewContentReportCommand(report.Id.Value, "  verified  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Reviewed, report.Status);
        Assert.Equal("verified", report.AdminNote);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DismissContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new DismissContentReportCommandHandler(new CountingContentReportRepository(0));
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new DismissContentReportCommand(reportId, "duplicate"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ContentReport.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DismissContentReportHandler_WithExistingReport_MarksDismissed() {
        var report = ContentReport.Create(
            UserId.New(),
            ReportTargetType.Recipe,
            Guid.NewGuid(),
            "Incorrect content");
        var repository = new CountingContentReportRepository(0, [report]);
        var handler = new DismissContentReportCommandHandler(repository);

        Result result = await handler.Handle(new DismissContentReportCommand(report.Id.Value, "  duplicate  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal("duplicate", report.AdminNote);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task GetAdminEmailTemplatesQueryHandler_ReturnsTemplates() {
        var template = EmailTemplate.Create(
            "verify_email",
            "en",
            "Subject",
            "<b>Body</b>",
            "Body",
            true);
        var handler = new GetAdminEmailTemplatesQueryHandler(new InMemoryEmailTemplateRepository(template));

        Result<IReadOnlyList<AdminEmailTemplateModel>> result = await handler.Handle(new GetAdminEmailTemplatesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        var handler = new GetAdminImpersonationSessionsQueryHandler(repository);

        Result<PagedResponse<AdminImpersonationSessionReadModel>> result = await handler.Handle(new GetAdminImpersonationSessionsQuery(0, 999, " target "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(session, Assert.Single(result.Value.Data));
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.Limit);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(" target ", repository.LastSearch);
    }

    [Fact]
    public async Task GetAdminUserRoleAuditQueryHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new GetAdminUserRoleAuditQueryHandler(
            new InMemoryUserRepository(CreateUserWithRoles("admin@example.com", []), []),
            new RecordingUserRoleAuditRepository());

        Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>> result = await handler.Handle(new GetAdminUserRoleAuditQuery(Guid.Empty, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetAdminUserRoleAuditQueryHandler_WhenUserMissing_ReturnsNotFound() {
        var handler = new GetAdminUserRoleAuditQueryHandler(
            new InMemoryUserRepository(CreateUserWithRoles("admin@example.com", []), []),
            new RecordingUserRoleAuditRepository());

        Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>> result = await handler.Handle(new GetAdminUserRoleAuditQuery(Guid.NewGuid(), 10), CancellationToken.None);

        Assert.True(result.IsFailure);
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
            null,
            null,
            "test",
            DateTime.UtcNow);
        var repository = new RecordingUserRoleAuditRepository([auditEvent]);
        var handler = new GetAdminUserRoleAuditQueryHandler(
            new InMemoryUserRepository(user, [RoleNames.Admin]),
            repository);

        Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>> result = await handler.Handle(new GetAdminUserRoleAuditQuery(user.Id.Value, 999), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(auditEvent, Assert.Single(result.Value));
        Assert.Equal(50, repository.LastLimit);
    }

    private static User CreateUserWithRoles(string email, IReadOnlyList<string> roleNames) {
        var user = User.Create(email, "hash");
        Role[] roles = roleNames.Select(name => Role.Create(name)).ToArray();
        user.ReplaceRoles(roles);
        return user;
    }

    private static UpdateAdminUserCommandHandler CreateUpdateAdminUserHandler(InMemoryUserRepository userRepository) =>
        new(userRepository, new NullAuditLogger(), new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)));

    private static StartAdminImpersonationCommandHandler CreateStartImpersonationHandler(
        User actor,
        User target,
        RecordingImpersonationSessionRepository? sessionRepository = null) =>
        new(
            new MultipleUserRepository([actor, target]),
            sessionRepository ?? new RecordingImpersonationSessionRepository(),
            new StubJwtTokenGenerator(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)),
            new NullAuditLogger());

    [ExcludeFromCodeCoverage]
    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) { }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserRepository(User user, IEnumerable<string> availableRoles) : IUserRepository {
        private readonly User _user = user;
        private readonly Dictionary<string, Role> _roles = availableRoles.ToDictionary(
                name => name,
                name => user.UserRoles
                    .Select(userRole => userRole.Role)
                    .FirstOrDefault(role => string.Equals(role.Name, name, StringComparison.Ordinal))
                    ?? Role.Create(name),
                StringComparer.Ordinal);

        public List<UserRoleAuditEvent> RoleAuditEvents { get; } = [];

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

        public Task UpdateAsync(
            User user,
            IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
            CancellationToken cancellationToken = default) {
            RoleAuditEvents.AddRange(roleAuditEvents);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class MultipleUserRepository(IReadOnlyList<User> users) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(users.FirstOrDefault(user => user.Id == id));
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImpersonationSessionRepository : IAdminImpersonationSessionRepository {
        public int AddCallCount { get; private set; }
        public FoodDiary.Domain.Entities.Admin.AdminImpersonationSession? LastSession { get; private set; }
        public (IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems) PagedResponse { get; set; } = ([], 0);
        public int LastPage { get; private set; }
        public int LastLimit { get; private set; }
        public string? LastSearch { get; private set; }

        public Task AddAsync(FoodDiary.Domain.Entities.Admin.AdminImpersonationSession session, CancellationToken cancellationToken = default) {
            AddCallCount++;
            LastSession = session;
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
            int page,
            int limit,
            string? search,
            CancellationToken cancellationToken = default) {
            LastPage = page;
            LastLimit = limit;
            LastSearch = search;
            return Task.FromResult(PagedResponse);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubJwtTokenGenerator : IJwtTokenGenerator {
        public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles) => "access-token";
        public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles, DateTime? expiresAtUtc) => "access-token";
        public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles, JwtImpersonationContext impersonation) => "impersonation-token";
        public string GenerateRefreshToken(
            UserId userId,
            string email,
            IReadOnlyCollection<string> roles,
            bool rememberMe = false,
            Guid? refreshSessionId = null) => "refresh-token";
        public (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? ValidateToken(string token) => null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAiUsageRepository(
        FoodDiary.Application.Abstractions.Admin.Models.AiUsageSummary? response = null) : IAiUsageRepository {
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

            return Task.FromResult(response ?? new FoodDiary.Application.Abstractions.Admin.Models.AiUsageSummary(
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

    [ExcludeFromCodeCoverage]
    private sealed class SummaryUserRepository(
        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers) response) : IUserRepository {
        public int LastRecentLimit { get; private set; }

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(
            int recentLimit,
            CancellationToken cancellationToken = default) {
            LastRecentLimit = recentLimit;
            return Task.FromResult(response);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingContentReportRepository(int pendingCount, IReadOnlyList<ContentReport>? reports = null) : IContentReportRepository {
        public ReportStatus? LastStatus { get; private set; }
        public int LastPage { get; private set; }
        public int LastLimit { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult(status == ReportStatus.Pending ? pendingCount : 0);

        public Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ContentReport?> GetByIdAsync(ContentReportId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult((reports ?? []).FirstOrDefault(report => report.Id == id));

        public Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task<bool> HasUserReportedAsync(UserId userId, ReportTargetType targetType, Guid targetId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<ContentReport> Items, int Total)> GetPagedAsync(
            ReportStatus? status,
            int page,
            int limit,
            CancellationToken cancellationToken = default) {
            LastStatus = status;
            LastPage = page;
            LastLimit = limit;
            IReadOnlyList<ContentReport> filtered = reports ?? [];
            return Task.FromResult((filtered, filtered.Count));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingUserRoleAuditRepository(IReadOnlyList<AdminUserRoleAuditEventReadModel>? events = null) : IAdminUserRoleAuditRepository {
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<AdminUserRoleAuditEventReadModel>> GetRecentForUserAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken = default) {
            LastLimit = limit;
            return Task.FromResult(events ?? []);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingEmailTransport : IEmailTransport {
        public string? ToEmail { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public List<string> AlternateViewBodies { get; } = [];

        public async Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
            ToEmail = message.To.Single().Address;
            Subject = message.Subject;
            Body = message.Body;

            foreach (AlternateView view in message.AlternateViews) {
                using var reader = new StreamReader(view.ContentStream);
                AlternateViewBodies.Add(await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false));
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryAiPromptTemplateRepository(params AiPromptTemplate[] templates) : IAiPromptTemplateRepository {
        private readonly List<AiPromptTemplate> _templates = templates.ToList();

        public IReadOnlyList<AiPromptTemplate> Templates => _templates;
        public int UpdateCallCount { get; private set; }

        public Task<IReadOnlyList<AiPromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiPromptTemplate>>(_templates);

        public Task<AiPromptTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default) =>
            Task.FromResult(_templates.FirstOrDefault(template =>
                string.Equals(template.Key, key, StringComparison.Ordinal) &&
                string.Equals(template.Locale, locale, StringComparison.Ordinal)));

        public Task<AiPromptTemplate?> GetByIdAsync(
            AiPromptTemplateId id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_templates.FirstOrDefault(template => template.Id == id));

        public Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
            _templates.Add(template);
            return Task.FromResult(template);
        }

        public Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAdminMailInboxReader : IAdminMailInboxReader {
        public IReadOnlyList<AdminMailInboxMessageSummaryModel> Messages { get; init; } = [];
        public AdminMailInboxMessageDetailsModel? Message { get; init; }
        public int LastLimit { get; private set; }
        public Guid LastMessageId { get; private set; }

        public Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) {
            LastLimit = limit;
            return Task.FromResult(Messages);
        }

        public Task<AdminMailInboxMessageDetailsModel?> GetMessageAsync(
            Guid id,
            CancellationToken cancellationToken) {
            LastMessageId = id;
            return Task.FromResult(Message is not null && Message.Id == id ? Message : null);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryEmailTemplateRepository(params EmailTemplate[] templates) : IEmailTemplateRepository {
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
            Task.FromResult<IReadOnlyList<EmailTemplate>>(templates);

        public Task<EmailTemplate?> GetByKeyAsync(
            string key,
            string locale,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmailTemplate?>(null);
    }

    [ExcludeFromCodeCoverage]
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
