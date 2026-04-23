using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Commands.VerifyEmail;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Authentication;

public sealed class AuthenticationCommandHandlerTests {
    [Fact]
    public async Task AdminSsoStartHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AdminSsoStartCommandHandler(new StubAdminSsoService());

        var result = await handler.Handle(new AdminSsoStartCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            new NullAuditLogger());

        var result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.Empty, "token", "StrongPass123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmailHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubEmailVerificationNotifier());

        var result = await handler.Handle(
            new VerifyEmailCommand(Guid.Empty, "token"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ResendEmailVerificationCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ResendEmailVerificationCommandHandler>.Instance);

        var result = await handler.Handle(
            new ResendEmailVerificationCommand(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkTelegramHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator());

        var result = await handler.Handle(
            new LinkTelegramCommand(Guid.Empty, "init-data"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithDeletedUser_RestoresAndIssuesTokens() {
        var user = User.Create("deleted@example.com", "secret");
        user.DeleteAccount(DateTime.UtcNow.AddDays(-2));
        var handler = new RestoreAccountCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService(),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new RestoreAccountCommand(user.Email, "secret"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public async Task GoogleLoginHandler_ForGoogleOnlyAccount_CreatesPasswordSetupNotification() {
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(),
            notificationRepository,
            new StubGoogleTokenValidator(new GoogleIdentityPayload("google@example.com", "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        var result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var notification = Assert.Single(notificationRepository.Notifications);
        Assert.Equal(NotificationTypes.PasswordSetupSuggested, notification.Type);
        Assert.StartsWith("password-setup:", notification.ReferenceId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GoogleLoginHandler_DoesNotDuplicatePasswordSetupNotification() {
        var user = User.Create("google@example.com", "secret", hasPassword: false);
        var existingNotification = NotificationFactory.CreatePasswordSetupSuggested(user.Id, $"password-setup:{user.Id.Value}");
        var notificationRepository = new StubNotificationRepository(existingNotification);
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubGoogleTokenValidator(new GoogleIdentityPayload("google@example.com", "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        var result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(notificationRepository.Notifications);
    }

    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType, string? targetId, string? details) { }
    }

    private sealed class StubAdminSsoService : IAdminSsoService {
        public Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubUserRepository(User? user = null) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user is not null && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) ? user : null);
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => Task.FromResult(userToAdd);
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubPasswordHasher : IPasswordHasher {
        public string Hash(string password) => password;

        public bool Verify(string password, string hashedPassword) => password == hashedPassword;
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => new(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class StubAuthenticationTokenService : IAuthenticationTokenService {
        public Task<IssuedAuthenticationTokens> IssueAndStoreAsync(User user, CancellationToken cancellationToken) =>
            Task.FromResult(new IssuedAuthenticationTokens("access", "refresh"));

        public string IssueAccessToken(User user) => throw new NotSupportedException();
    }

    private sealed class StubNotificationRepository(params Notification[] notifications) : INotificationRepository {
        public List<Notification> Notifications { get; } = notifications.ToList();

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Notifications.Where(x => x.UserId == userId).Take(limit).ToList());

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Notification?>(Notifications.FirstOrDefault(x => x.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default) {
            Notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Any(x => x.UserId == userId && x.Type == type && x.ReferenceId == referenceId));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead && x.Type == type));

        public Task MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class StubEmailVerificationNotifier : IEmailVerificationNotifier {
        public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubEmailSender : IEmailSender {
        public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubTelegramAuthValidator : ITelegramAuthValidator {
        public FoodDiary.Application.Common.Abstractions.Result.Result<TelegramInitData> ValidateInitData(string initData) =>
            throw new NotSupportedException();
    }

    private sealed class StubGoogleTokenValidator(GoogleIdentityPayload payload) : IGoogleTokenValidator {
        public Task<FoodDiary.Application.Common.Abstractions.Result.Result<GoogleIdentityPayload>> ValidateCredentialAsync(
            string credential,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FoodDiary.Application.Common.Abstractions.Result.Result.Success(payload));
    }
}
