using OwnerDismissContentReportCommandHandler = FoodDiary.Application.ContentReports.Commands.DismissContentReport.DismissContentReportCommandHandler;
using OwnerReviewContentReportCommandHandler = FoodDiary.Application.ContentReports.Commands.ReviewContentReport.ReviewContentReportCommandHandler;
using FoodDiary.Application.Identity.Email.Commands.UpsertEmailTemplate;
using FoodDiary.Application.Users.Commands.CreateUserByAdministrator;
using FoodDiary.Application.Users.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Application.Users.Commands.UpdateUserByAdministrator;
using FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt;
using FoodDiary.Application.Abstractions.Queries.GetFilteredUsersForAdministration;
using FoodDiary.Application.Abstractions.Queries.GetUserAdministrationSummary;
using FoodDiary.Application.Abstractions.Queries.GetUserForAdministration;
using FoodDiary.Application.Abstractions.Queries.GetUsersForAdministration;
using FoodDiary.Testing;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Admin.Application.Commands.DismissContentReport;
using FoodDiary.Modules.Admin.Application.Commands.MarkAdminMailInboxMessageRead;
using FoodDiary.Modules.Admin.Application.Commands.ReviewContentReport;
using FoodDiary.Modules.Admin.Application.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Modules.Admin.Application.Commands.StartAdminImpersonation;
using FoodDiary.Modules.Admin.Application.Commands.UpdateAdminUser;
using FoodDiary.Modules.Admin.Application.Commands.UpsertAdminAiPrompt;
using FoodDiary.Modules.Admin.Application.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Results;
using FluentValidation.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public partial class AdminFeatureTests {
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.ImpersonationForbidden", result.Error.Code);
        Assert.Equal(0, sessionRepository.AddCallCount);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithDeletedTarget_ReturnsForbiddenWithoutSession() {
        User actor = CreateUserWithRoles("admin-deleted-target@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("deleted-target@example.com", []);
        target.DeleteAccount(DateTime.UtcNow);
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

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.ImpersonationForbidden", result.Error.Code);
        Assert.Equal(0, sessionRepository.AddCallCount);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithAdminTarget_ReturnsForbiddenWithoutSession() {
        User actor = CreateUserWithRoles("admin-actor@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("admin-target@example.com", [RoleNames.Admin]);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task StartAdminImpersonationHandler_WithValidRequest_CreatesSessionAndReturnsToken() {
        User actor = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        User target = CreateUserWithRoles("client@example.com", [RoleNames.Premium]);
        var sessionRepository = new RecordingImpersonationSessionRepository();
        var issuer = new StubImpersonationTokenIssuer();
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target, sessionRepository, issuer);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                target.Id.Value,
                "  Support case with billing issue  ",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("one-time-code", result.Value.Code);
        Assert.Equal(target.Id.Value, result.Value.TargetUserId);
        Assert.Equal(actor.Id.Value, result.Value.ActorUserId);
        Assert.Equal("Support case with billing issue", result.Value.Reason);
        Assert.Equal(1, sessionRepository.AddCallCount);
        Assert.Equal(target.Id, sessionRepository.LastSession?.TargetUserId);
        Assert.Equal(actor.Id, sessionRepository.LastSession?.ActorUserId);
        Assert.NotNull(issuer.LastRequest);
        Assert.Multiple(
            () => Assert.Equal(target.Id, issuer.LastRequest.SubjectId),
            () => Assert.Equal(actor.Id, issuer.LastRequest.ActorId),
            () => Assert.Equal(target.Email, issuer.LastRequest.Email),
            () => Assert.Equal("Support case with billing issue", issuer.LastRequest.Reason),
            () => Assert.Equal([RoleNames.Premium], issuer.LastRequest.Roles, StringComparer.Ordinal));
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
        var handler = new UpsertAdminEmailTemplateCommandHandler(
            RequestTestSender.Create(new UpsertEmailTemplateCommandHandler(new InMemoryEmailTemplateRepository())));

        Result<AdminEmailTemplateModel> result = await handler.Handle(
            new UpsertAdminEmailTemplateCommand(
                Key: "verify_email",
                Locale: "de",
                Subject: "Subject",
                HtmlBody: "<b>Body</b>",
                TextBody: "Body",
                IsActive: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("supported codes", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpsertAdminEmailTemplateHandler_WithValidCommand_UpsertsTemplate() {
        var handler = new UpsertAdminEmailTemplateCommandHandler(
            RequestTestSender.Create(new UpsertEmailTemplateCommandHandler(new InMemoryEmailTemplateRepository())));

        Result<AdminEmailTemplateModel> result = await handler.Handle(
            new UpsertAdminEmailTemplateCommand(
                Key: " Verify_Email ",
                Locale: " EN ",
                Subject: "Subject",
                HtmlBody: "<b>Body</b>",
                TextBody: "Body",
                IsActive: true),
            CancellationToken.None);

        ResultAssert.Success(result);
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

        ResultAssert.Success(result);
        Assert.Equal("admin@example.com", transport.ToEmail);
        Assert.Equal("Hello Alex Johnson", transport.Subject);
        Assert.Equal("<a href=\"https://fooddiary.club/dietologist-invitations/demo\">FoodDiary</a>", transport.Body);
        Assert.Contains(
            "Alex Johnson on FoodDiary: https://fooddiary.club/dietologist-invitations/demo",
            transport.AlternateViewBodies, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SendAdminEmailTemplateTestHandler_UsesDemoAccountVariablesWithoutCreatingCredentials() {
        var transport = new RecordingEmailTransport();
        var handler = new SendAdminEmailTemplateTestCommandHandler(new EmailOptions {
            FromAddress = "noreply@example.com",
            FromName = "FoodDiary",
        }, transport);
        Result result = await handler.Handle(new SendAdminEmailTemplateTestCommand(
            "admin@example.com", "account_created", "{{brand}} account",
            "{{email}} {{temporaryPassword}} {{loginLink}} {{link}}", "{{email}}"), CancellationToken.None);
        ResultAssert.Success(result);
        Assert.Equal("demo@example.com Demo-only-password https://fooddiary.club/login https://fooddiary.club/login", transport.Body);
        Assert.Equal("FoodDiary account", transport.Subject);
    }

    [Fact]
    public async Task UpsertAdminAiPromptHandler_WhenPromptMissing_CreatesTemplate() {
        var repository = new InMemoryAiPromptTemplateRepository();
        var handler = new UpsertAdminAiPromptCommandHandler(RequestTestSender.Create(new UpsertAiPromptCommandHandler(repository)));

        Result<AdminAiPromptModel> result = await handler.Handle(
            new UpsertAdminAiPromptCommand(" Meal_Summary ", " EN ", " Prompt text ", IsActive: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("meal_summary", result.Value.Key);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal("Prompt text", result.Value.PromptText);
        Assert.True(result.Value.IsActive);
        Assert.Single(repository.Templates);
    }

    [Fact]
    public async Task UpsertAdminAiPromptHandler_WhenPromptExists_UpdatesTrackedTemplate() {
        var template = AiPromptTemplate.Create("meal_summary", "en", "Old prompt", isActive: true);
        var repository = new InMemoryAiPromptTemplateRepository(template);
        var handler = new UpsertAdminAiPromptCommandHandler(RequestTestSender.Create(new UpsertAiPromptCommandHandler(repository)));

        Result<AdminAiPromptModel> result = await handler.Handle(
            new UpsertAdminAiPromptCommand("MEAL_SUMMARY", "EN", "New prompt", IsActive: false),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(template.Id.Value, result.Value.Id);
        Assert.Equal("New prompt", result.Value.PromptText);
        Assert.False(result.Value.IsActive);
        Assert.Equal(2, result.Value.Version);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task UpsertAdminAiPromptHandler_WithInvalidLocale_ReturnsValidationFailure() {
        var repository = new InMemoryAiPromptTemplateRepository();
        var handler = new UpsertAdminAiPromptCommandHandler(RequestTestSender.Create(new UpsertAiPromptCommandHandler(repository)));

        Result<AdminAiPromptModel> result = await handler.Handle(
            new UpsertAdminAiPromptCommand("meal_summary", "xx", "Prompt text", IsActive: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(repository.Templates);
    }

    [Fact]
    public async Task MarkAdminMailInboxMessageReadCommandHandler_WhenMessageExists_ReturnsSuccess() {
        var messageId = Guid.NewGuid();
        var reader = new RecordingAdminMailInboxReader {
            MarkReadResult = true,
        };
        var handler = new MarkAdminMailInboxMessageReadCommandHandler(reader);

        Result result = await handler.Handle(new MarkAdminMailInboxMessageReadCommand(messageId), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(messageId, reader.LastReadMessageId);
    }

    [Fact]
    public async Task MarkAdminMailInboxMessageReadCommandHandler_WhenMessageMissing_ReturnsNotFound() {
        var messageId = Guid.NewGuid();
        var handler = new MarkAdminMailInboxMessageReadCommandHandler(new RecordingAdminMailInboxReader());

        Result result = await handler.Handle(new MarkAdminMailInboxMessageReadCommand(messageId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("MailInbox.MessageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ReviewContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new ReviewContentReportCommandHandler(
            RequestTestSender.Create(new OwnerReviewContentReportCommandHandler(new CountingContentReportRepository(0)), new OwnerDismissContentReportCommandHandler(new CountingContentReportRepository(0))));
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new ReviewContentReportCommand(reportId, Guid.NewGuid(), "handled"), CancellationToken.None);

        ResultAssert.Failure(result);
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
        var handler = new ReviewContentReportCommandHandler(RequestTestSender.Create(new OwnerReviewContentReportCommandHandler(repository), new OwnerDismissContentReportCommandHandler(repository)));

        var reviewerUserId = UserId.New();
        Result result = await handler.Handle(new ReviewContentReportCommand(report.Id.Value, reviewerUserId.Value, "  verified  "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(ReportStatus.Reviewed, report.Status);
        Assert.Equal("verified", report.AdminNote);
        Assert.Equal(reviewerUserId, report.ReviewedByUserId);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DismissContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new DismissContentReportCommandHandler(
            RequestTestSender.Create(new OwnerReviewContentReportCommandHandler(new CountingContentReportRepository(0)), new OwnerDismissContentReportCommandHandler(new CountingContentReportRepository(0))));
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new DismissContentReportCommand(reportId, Guid.NewGuid(), "duplicate"), CancellationToken.None);

        ResultAssert.Failure(result);
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
        var handler = new DismissContentReportCommandHandler(RequestTestSender.Create(new OwnerReviewContentReportCommandHandler(repository), new OwnerDismissContentReportCommandHandler(repository)));

        var reviewerUserId = UserId.New();
        Result result = await handler.Handle(new DismissContentReportCommand(report.Id.Value, reviewerUserId.Value, "  duplicate  "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal("duplicate", report.AdminNote);
        Assert.Equal(reviewerUserId, report.ReviewedByUserId);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DismissContentReportHandler_WhenReportAlreadyResolved_ReturnsConflictWithoutUpdate() {
        var report = ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "Incorrect content");
        report.MarkReviewed(UserId.New(), "verified");
        var repository = new CountingContentReportRepository(0, [report]);
        var handler = new DismissContentReportCommandHandler(RequestTestSender.Create(new OwnerReviewContentReportCommandHandler(repository), new OwnerDismissContentReportCommandHandler(repository)));

        Result result = await handler.Handle(
            new DismissContentReportCommand(report.Id.Value, Guid.NewGuid(), "overwrite"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("ContentReport.AlreadyResolved", result.Error.Code),
            () => Assert.Equal(ReportStatus.Reviewed, report.Status),
            () => Assert.Equal(0, repository.UpdateCallCount));
    }

    private static User CreateUserWithRoles(string email, IReadOnlyList<string> roleNames) {
        var user = User.Create(email, "hash");
        Role[] roles = [.. roleNames.Select(name => Role.Create(name))];
        user.ReplaceRoles(roles);
        return user;
    }

    private static UpdateAdminUserCommandHandler CreateUpdateAdminUserHandler(InMemoryUserRepository userRepository) =>
        new(
            RequestTestSender.Create(new CreateUserByAdministratorCommandHandler(userRepository, userRepository, userRepository, new PrefixPasswordHasher()), new UpdateUserByAdministratorCommandHandler(userRepository, userRepository, userRepository), new SetUserPasswordByAdministratorCommandHandler(userRepository, userRepository, new PrefixPasswordHasher())),
            new NullAuditLogger(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)));

    private static StartAdminImpersonationCommandHandler CreateStartImpersonationHandler(
        User actor,
        User target,
        RecordingImpersonationSessionRepository? sessionRepository = null,
        StubImpersonationTokenIssuer? issuer = null) =>
        CreateStartImpersonationHandler(
            new MultipleUserRepository([actor, target]),
            sessionRepository ?? new RecordingImpersonationSessionRepository(),
            new FixedDateTimeProvider(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc)), issuer);

    private static StartAdminImpersonationCommandHandler CreateStartImpersonationHandler(
        MultipleUserRepository repository,
        RecordingImpersonationSessionRepository sessionRepository,
        FixedDateTimeProvider dateTimeProvider,
        StubImpersonationTokenIssuer? issuer = null) =>
        new(
            new UserAuthenticationIdentityService(repository, repository, repository, new PrefixPasswordHasher()),
            sessionRepository,
            new StubImpersonationHandoffService(),
            issuer ?? new StubImpersonationTokenIssuer(),
            dateTimeProvider,
            new NullAuditLogger());

    [ExcludeFromCodeCoverage]
    private sealed class StubImpersonationHandoffService : IAdminImpersonationHandoffService {
        public Task<string> CreateCodeAsync(string accessToken, CancellationToken cancellationToken = default) =>
            Task.FromResult("one-time-code");

        public Task<string?> ConsumeCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) {
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserRepository(User user, IEnumerable<string> availableRoles)
: RequestTestSender, IUserRepository, IUserRoleCatalogService {
        private readonly Dictionary<string, Role> _roles = availableRoles.ToDictionary(
            name => name,
            name => user.UserRoles
                        .Select(userRole => userRole.Role)
                        .FirstOrDefault(role => string.Equals(role.Name, name, StringComparison.Ordinal))
                    ?? Role.Create(name),
            StringComparer.Ordinal);

        public List<UserRoleAuditEvent> RoleAuditEvents { get; } = [];
        public int UpdateCallCount { get; private set; }

        public Task<User?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByEmailIncludingDeletedAsync(string? email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == userId ? user : null);

        public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == userId ? user : null);

        private async Task<UserAdminReadModel?> GetByIdIncludingDeletedForRequestAsync(UserId userId, CancellationToken cancellationToken) =>
            (await GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false))?.ToAdminReadModel();

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            UserAccountStatusFilter status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetFilteredPagedAsync(string? search, int page, int limit, UserAccountStatusFilter status, UserAdministrationFilter filter, CancellationToken cancellationToken) => throw new NotSupportedException();

        private Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedForRequestAsync(
            string? search,
            int page,
            int limit,
            UserAccountStatusFilter status,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            GetAdminDashboardSummaryAsync(recentLimit, cancellationToken);

        private Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)> GetDashboardSummaryForRequestAsync(
            int recentLimit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
            var found = names.Where(name => _roles.ContainsKey(name)).Select(name => _roles[name]).ToList();
            return Task.FromResult<IReadOnlyList<Role>>(found);
        }

        public Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
            GetRolesByNamesAsync(names, cancellationToken);

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            User user,
            IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
            CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            RoleAuditEvents.AddRange(roleAuditEvents);
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(global::FoodDiary.Mediator.IRequest<TResponse> request, CancellationToken cancellationToken = default) => request switch {
            GetFilteredUsersForAdministrationQuery r => (Task<TResponse>)(object)GetFilteredPagedAsync(r.Search, r.Page, r.Limit, r.Status, r.Filter, cancellationToken),
            GetUserForAdministrationQuery r => (Task<TResponse>)(object)GetByIdIncludingDeletedForRequestAsync(r.UserId, cancellationToken),
            GetUsersForAdministrationQuery r => (Task<TResponse>)(object)GetPagedForRequestAsync(r.Search, r.Page, r.Limit, r.Status, cancellationToken),
            GetUserAdministrationSummaryQuery r => (Task<TResponse>)(object)GetDashboardSummaryForRequestAsync(r.RecentLimit, cancellationToken),
            _ => throw new InvalidOperationException(request.GetType().Name),
        };
    }

    [ExcludeFromCodeCoverage]
    private sealed class PrefixPasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hashedPassword) =>
            string.Equals(Hash(password), hashedPassword, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class MultipleUserRepository(IReadOnlyList<User> users) : IUserRepository, IUserGoogleIdentityRepository {
        public Task<User?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string? email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(users.FirstOrDefault(user => user.Id == userId));
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(users.FirstOrDefault(user => user.Id == id));
        public Task<User?> GetByGoogleIdentityIncludingDeletedAsync(string issuer, string subject, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
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
    private sealed class RecordingImpersonationSessionRepository : IAdminImpersonationSessionWriteRepository, IAdminImpersonationSessionQuery {
        public int AddCallCount { get; private set; }
        public FoodDiary.Modules.Admin.Domain.Entities.AdminImpersonationSession? LastSession { get; private set; }
        public (IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems) PagedResponse { get; set; } = ([], 0);
        public int LastPage { get; private set; }
        public int LastLimit { get; private set; }
        public string? LastSearch { get; private set; }

        public Task AddAsync(FoodDiary.Modules.Admin.Domain.Entities.AdminImpersonationSession session, CancellationToken cancellationToken = default) {
            AddCallCount++;
            LastSession = session;
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
            int page,
            int limit,
            string? search,
            CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? actorId = null, Guid? targetId = null) {
            LastPage = page;
            LastLimit = limit;
            LastSearch = search;
            return Task.FromResult(PagedResponse);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubImpersonationTokenIssuer : IImpersonationTokenIssuer {
        public ImpersonationTokenRequest? LastRequest { get; private set; }

        public string IssueAccessToken(ImpersonationTokenRequest request) {
            LastRequest = request;
            return "impersonation-token";
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAiUsageRepository(
        FoodDiary.Modules.Ai.Contracts.Models.AiUsageSummary? response = null) : IAiUsageQuery {
        public Task<FoodDiary.Modules.Ai.Contracts.Models.AiUsageSummary> GetSummaryForUserAsync(
            DateTime fromUtc, DateTime toUtc, UserId userId, CancellationToken cancellationToken) => GetSummaryAsync(fromUtc, toUtc, cancellationToken);

        public DateTime LastFromUtc { get; private set; }
        public DateTime LastToUtc { get; private set; }

        public Task AddAsync(FoodDiary.Modules.Ai.Domain.Entities.AiUsage usage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FoodDiary.Modules.Ai.Contracts.Models.AiUsageSummary> GetSummaryAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) {
            LastFromUtc = fromUtc;
            LastToUtc = toUtc;

            return Task.FromResult(response ?? new FoodDiary.Modules.Ai.Contracts.Models.AiUsageSummary(
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
        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers) response) : RequestTestSender, IUserRepository {
        public int LastRecentLimit { get; private set; }

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(
            int recentLimit,
            CancellationToken cancellationToken = default) {
            LastRecentLimit = recentLimit;
            return Task.FromResult(response);
        }

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetDashboardSummaryAsync(
            int recentLimit,
            CancellationToken cancellationToken = default) =>
            GetAdminDashboardSummaryAsync(recentLimit, cancellationToken);

        private async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)> GetDashboardSummaryForRequestAsync(
            int recentLimit,
            CancellationToken cancellationToken) {
            (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<User> recentUsers) =
                await GetAdminDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

            return (totalUsers, activeUsers, premiumUsers, deletedUsers, [.. recentUsers.Select(user => user.ToAdminReadModel())]);
        }

        public Task<User?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string? email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        private async Task<UserAdminReadModel?> GetByIdIncludingDeletedForRequestAsync(UserId userId, CancellationToken cancellationToken) =>
            (await GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false))?.ToAdminReadModel();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            UserAccountStatusFilter status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetFilteredPagedAsync(string? search, int page, int limit, UserAccountStatusFilter status, UserAdministrationFilter filter, CancellationToken cancellationToken) => throw new NotSupportedException();

        private Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedForRequestAsync(
            string? search,
            int page,
            int limit,
            UserAccountStatusFilter status,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> ExistsIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public override Task<TResponse> Send<TResponse>(global::FoodDiary.Mediator.IRequest<TResponse> request, CancellationToken cancellationToken = default) => request switch {
            GetFilteredUsersForAdministrationQuery r => (Task<TResponse>)(object)GetFilteredPagedAsync(r.Search, r.Page, r.Limit, r.Status, r.Filter, cancellationToken),
            GetUserForAdministrationQuery r => (Task<TResponse>)(object)GetByIdIncludingDeletedForRequestAsync(r.UserId, cancellationToken),
            GetUsersForAdministrationQuery r => (Task<TResponse>)(object)GetPagedForRequestAsync(r.Search, r.Page, r.Limit, r.Status, cancellationToken),
            GetUserAdministrationSummaryQuery r => (Task<TResponse>)(object)GetDashboardSummaryForRequestAsync(r.RecentLimit, cancellationToken),
            _ => throw new InvalidOperationException(request.GetType().Name),
        };
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingContentReportRepository(int pendingCount, IReadOnlyList<ContentReport>? reports = null)
        : IContentReportReadModelRepository, IContentReportWriteRepository {
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

        public Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
            ReportStatus? status,
            int page,
            int limit,
            CancellationToken cancellationToken = default, ContentReportAdminFilter? filter = null) {
            LastStatus = status;
            LastPage = page;
            LastLimit = limit;
            IReadOnlyList<ContentReport> filtered = reports ?? [];
            IReadOnlyList<ContentReportAdminReadModel> models = [
                .. filtered.Select(static report => new ContentReportAdminReadModel(
                    report.Id.Value,
                    report.UserId.Value,
                    report.TargetType.ToString(),
                    report.TargetId,
                    report.Reason,
                    report.Status.ToString(),
                    report.AdminNote,
                    report.CreatedOnUtc,
                    report.ReviewedAtUtc)),
            ];
            return Task.FromResult((models, models.Count));
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

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) {
            ToEmail = message.ToAddresses.Single();
            Subject = message.Subject;
            Body = message.HtmlBody;

            if (message.TextBody is not null) {
                AlternateViewBodies.Add(message.TextBody);
            }

            AlternateViewBodies.Add(message.HtmlBody);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryAiPromptTemplateRepository(params AiPromptTemplate[] templates) : IAiPromptTemplateWriteRepository, IAiPromptTemplateReadModelRepository {
        public Task<IReadOnlyList<AiPromptRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AiPromptRevisionReadModel>>([]);
        private readonly List<AiPromptTemplate> _templates = [.. templates];

        public IReadOnlyList<AiPromptTemplate> Templates => _templates;
        public int UpdateCallCount { get; private set; }

        public Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiPromptTemplateReadModel>>([.. _templates.Select(ToReadModel)]);

        public Task<AiPromptTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default) =>
            Task.FromResult(_templates.FirstOrDefault(template =>
                string.Equals(template.Key, key, StringComparison.Ordinal) &&
                string.Equals(template.Locale, locale, StringComparison.Ordinal)));

        public Task<AiPromptTemplate> AddAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
            _templates.Add(template);
            return Task.FromResult(template);
        }

        public Task UpdateAsync(AiPromptTemplate template, CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            return Task.CompletedTask;
        }
        private static AiPromptTemplateReadModel ToReadModel(AiPromptTemplate template) =>
            new(
                template.Id.Value,
                template.Key,
                template.Locale,
                template.PromptText,
                template.Version,
                template.IsActive,
                template.CreatedOnUtc,
                template.ModifiedOnUtc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAdminMailInboxReader : IAdminMailInboxReader {
        public IReadOnlyList<AdminMailInboxMessageSummaryModel> Messages { get; init; } = [];
        public AdminMailInboxMessageDetailsModel? Message { get; init; }
        public bool MarkReadResult { get; init; }
        public int LastLimit { get; private set; }
        public Guid LastMessageId { get; private set; }
        public Guid LastReadMessageId { get; private set; }

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

        public Task<bool> MarkMessageReadAsync(
            Guid id,
            CancellationToken cancellationToken) {
            LastReadMessageId = id;
            return Task.FromResult(MarkReadResult);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryEmailTemplateRepository(params EmailTemplate[] templates) : IEmailTemplateRepository {
        public Task<IReadOnlyList<EmailTemplateRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmailTemplateRevisionReadModel>>([]);
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

        public Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmailTemplateReadModel>>([
                .. templates.Select(static template => new EmailTemplateReadModel(
                    template.Id,
                    template.Key,
                    template.Locale,
                    template.Subject,
                    template.HtmlBody,
                    template.TextBody,
                    template.IsActive,
                    template.CreatedOnUtc,
                    template.ModifiedOnUtc)),
            ]);

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

        public Task<AdminBillingRevenueSummaryReadModel> GetRevenueSummaryAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdminBillingRevenueSummaryReadModel(fromUtc, toUtc, []));
    }
}
