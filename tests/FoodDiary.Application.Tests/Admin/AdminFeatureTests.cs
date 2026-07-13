using FoodDiary.Application.Admin.Commands.DismissContentReport;
using FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;
using FoodDiary.Application.Admin.Commands.ReviewContentReport;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.ContentReports.Services;
using FoodDiary.Application.Email.Services;
using FoodDiary.Application.Lessons.Services;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Results;
using FluentValidation.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public partial class AdminFeatureTests {
    [Fact]
    public async Task AdminImpersonationUserService_ForwardsGetById() {
        var user = User.Create("admin-impersonation-service@example.com", "hash");
        IUserLookupRepository repository = Substitute.For<IUserLookupRepository>();
        using var cts = new CancellationTokenSource();
        UserId? capturedUserId = null;
        CancellationToken capturedCancellationToken = default;
        repository
            .GetByIdAsync(
                Arg.Do<UserId>(userId => capturedUserId = userId),
                Arg.Do<CancellationToken>(cancellationToken => capturedCancellationToken = cancellationToken))
            .Returns(Task.FromResult<User?>(user));
        var service = new AdminImpersonationUserService(repository);

        User? result = await service.GetByIdAsync(user.Id, cts.Token);

        Assert.Same(user, result);
        Assert.Equal(user.Id, capturedUserId);
        Assert.Equal(cts.Token, capturedCancellationToken);
    }

    [Fact]
    public async Task AdminUserManagementService_ForwardsLookupRolesAndUpdate() {
        var user = User.Create("admin-management-service@example.com", "hash");
        var role = Role.Create("Premium");
        IUserAdministrationService userAdministrationService = Substitute.For<IUserAdministrationService>();
        using var cts = new CancellationTokenSource();
        userAdministrationService
            .GetByIdIncludingDeletedAsync(user.Id, cts.Token)
            .Returns(Task.FromResult<User?>(user));
        userAdministrationService
            .GetRolesByNamesAsync(Arg.Is<IReadOnlyList<string>>(names => names!.Count == 1 && names[0] == "Premium"), cts.Token)
            .Returns(Task.FromResult<IReadOnlyList<Role>>([role]));
        var service = new AdminUserManagementService(userAdministrationService);

        User? loadedUser = await service.GetByIdIncludingDeletedAsync(user.Id, cts.Token);
        IReadOnlyList<Role> roles = await service.GetRolesByNamesAsync(["Premium"], cts.Token);
        await service.UpdateAsync(user, [], cts.Token);

        Assert.Same(user, loadedUser);
        Assert.Equal(role, Assert.Single(roles));
        await userAdministrationService.Received(1).UpdateAsync(
            user,
            Arg.Is<IReadOnlyCollection<UserRoleAuditEvent>>(events => events!.Count == 0),
            cts.Token);
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
        StartAdminImpersonationCommandHandler handler = CreateStartImpersonationHandler(actor, target, sessionRepository);

        Result<AdminImpersonationStartModel> result = await handler.Handle(
            new StartAdminImpersonationCommand(
                actor.Id.Value,
                target.Id.Value,
                "  Support case with billing issue  ",
                "127.0.0.1",
                "Test"),
            CancellationToken.None);

        ResultAssert.Success(result);
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
        var handler = new UpsertAdminEmailTemplateCommandHandler(
            new EmailTemplateAdministrationService(new InMemoryEmailTemplateRepository()));

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
            new EmailTemplateAdministrationService(new InMemoryEmailTemplateRepository()));

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
        Assert.Equal("<a href=\"https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo\">FoodDiary</a>", transport.Body);
        Assert.Contains(
            "Alex Johnson on FoodDiary: https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo",
            transport.AlternateViewBodies);
    }

    [Fact]
    public async Task UpsertAdminAiPromptHandler_WhenPromptMissing_CreatesTemplate() {
        var repository = new InMemoryAiPromptTemplateRepository();
        var handler = new UpsertAdminAiPromptCommandHandler(new AiPromptAdministrationService(repository));

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
        var handler = new UpsertAdminAiPromptCommandHandler(new AiPromptAdministrationService(repository));

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
    public async Task UpsertAdminAiPromptHandler_WhenTrackedPromptDisappears_ReturnsNotFound() {
        var existing = AiPromptTemplate.Create("meal_summary", "en", "Old prompt", isActive: true);
        IAiPromptTemplateWriteRepository repository = Substitute.For<IAiPromptTemplateWriteRepository>();
        repository.GetByKeyAsync("meal_summary", "en", Arg.Any<CancellationToken>()).Returns(existing);
        repository
            .GetByIdAsync(existing.Id, asTracking: true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AiPromptTemplate?>(null));
        var handler = new UpsertAdminAiPromptCommandHandler(new AiPromptAdministrationService(repository));

        Result<AdminAiPromptModel> result = await handler.Handle(
            new UpsertAdminAiPromptCommand("meal_summary", "en", "New prompt", IsActive: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Ai.PromptTemplateNotFound", result.Error.Code);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<AiPromptTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertAdminAiPromptHandler_WithInvalidLocale_ReturnsValidationFailure() {
        var repository = new InMemoryAiPromptTemplateRepository();
        var handler = new UpsertAdminAiPromptCommandHandler(new AiPromptAdministrationService(repository));

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
            new ContentReportAdministrationService(new CountingContentReportRepository(0)));
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new ReviewContentReportCommand(reportId, "handled"), CancellationToken.None);

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
        var handler = new ReviewContentReportCommandHandler(new ContentReportAdministrationService(repository));

        Result result = await handler.Handle(new ReviewContentReportCommand(report.Id.Value, "  verified  "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(ReportStatus.Reviewed, report.Status);
        Assert.Equal("verified", report.AdminNote);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DismissContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new DismissContentReportCommandHandler(
            new ContentReportAdministrationService(new CountingContentReportRepository(0)));
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new DismissContentReportCommand(reportId, "duplicate"), CancellationToken.None);

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
        var handler = new DismissContentReportCommandHandler(new ContentReportAdministrationService(repository));

        Result result = await handler.Handle(new DismissContentReportCommand(report.Id.Value, "  duplicate  "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal("duplicate", report.AdminNote);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    private static User CreateUserWithRoles(string email, IReadOnlyList<string> roleNames) {
        var user = User.Create(email, "hash");
        Role[] roles = [.. roleNames.Select(name => Role.Create(name))];
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
        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) {
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserRepository(User user, IEnumerable<string> availableRoles) : IUserRepository, IAdminUserReadService, IAdminUserManagementService {
        private readonly User _user = user;

        private readonly Dictionary<string, Role> _roles = availableRoles.ToDictionary(
            name => name,
            name => user.UserRoles
                        .Select(userRole => userRole.Role)
                        .FirstOrDefault(role => string.Equals(role.Name, name, StringComparison.Ordinal))
                    ?? Role.Create(name),
            StringComparer.Ordinal);

        public List<UserRoleAuditEvent> RoleAuditEvents { get; } = [];
        public int UpdateCallCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(_user.Id == id ? _user : null);

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(_user.Id == id ? _user : null);

        async Task<AdminUserModel?> IAdminUserReadService.GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
            (await GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false))?.ToAdminModel();

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

        Task<(IReadOnlyList<AdminUserModel> Items, int TotalItems)> IAdminUserReadService.GetPagedAsync(
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

        Task<AdminDashboardSummaryModel> IAdminUserReadService.GetDashboardSummaryAsync(
            int recentLimit,
            int pendingReportsCount,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        async Task<bool> IAdminUserReadService.ExistsIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
            await GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false) is not null;

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
            var found = names.Where(name => _roles.ContainsKey(name)).Select(name => _roles[name]).ToList();
            return Task.FromResult<IReadOnlyList<Role>>(found);
        }

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
    }

    [ExcludeFromCodeCoverage]
    private sealed class PrefixPasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hashedPassword) =>
            string.Equals(Hash(password), hashedPassword, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class MultipleUserRepository(IReadOnlyList<User> users) : IUserRepository, IAdminImpersonationUserService {
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
        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers) response) : IUserRepository, IAdminUserReadService {
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

        public async Task<AdminDashboardSummaryModel> GetDashboardSummaryAsync(
            int recentLimit,
            int pendingReportsCount,
            CancellationToken cancellationToken = default) {
            (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<User> recentUsers) =
                await GetAdminDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

            return new AdminDashboardSummaryModel(
                totalUsers,
                activeUsers,
                premiumUsers,
                deletedUsers,
                pendingReportsCount,
                [.. recentUsers.Select(AdminUserMappings.ToAdminModel)]);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        async Task<AdminUserModel?> IAdminUserReadService.GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
            (await GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false))?.ToAdminModel();
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
        Task<(IReadOnlyList<AdminUserModel> Items, int TotalItems)> IAdminUserReadService.GetPagedAsync(
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

        public Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
            ReportStatus? status,
            int page,
            int limit,
            CancellationToken cancellationToken = default) {
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
    private sealed class InMemoryAiPromptTemplateRepository(params AiPromptTemplate[] templates) : IAiPromptTemplateRepository {
        private readonly List<AiPromptTemplate> _templates = [.. templates];

        public IReadOnlyList<AiPromptTemplate> Templates => _templates;
        public int UpdateCallCount { get; private set; }

        public Task<IReadOnlyList<AiPromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiPromptTemplate>>(_templates);

        public Task<IReadOnlyList<AiPromptTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiPromptTemplateReadModel>>([.. _templates.Select(ToReadModel)]);

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

    private static IAdminContentReadService CreateAdminContentReadService(
        INutritionLessonReadModelRepository? lessonRepository = null,
        IEmailTemplateReadModelRepository? emailTemplateRepository = null,
        IAiPromptTemplateReadModelRepository? aiPromptTemplateRepository = null,
        IContentReportReadModelRepository? contentReportRepository = null) =>
        new AdminContentReadService(
            new LessonAdministrationReadService(lessonRepository ?? Substitute.For<INutritionLessonReadModelRepository>()),
            new EmailTemplateAdministrationReadService(emailTemplateRepository ?? Substitute.For<IEmailTemplateReadModelRepository>()),
            new AiAdministrationReadService(
                Substitute.For<IAiUsageReadRepository>(),
                aiPromptTemplateRepository ?? Substitute.For<IAiPromptTemplateReadModelRepository>()),
            new ContentReportAdministrationReadService(
                contentReportRepository ?? Substitute.For<IContentReportReadModelRepository>(),
                Substitute.For<IContentReportReadRepository>()));

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
    }
}
