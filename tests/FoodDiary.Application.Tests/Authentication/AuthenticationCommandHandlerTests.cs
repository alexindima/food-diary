using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed partial class AuthenticationCommandHandlerTests {






























































    [ExcludeFromCodeCoverage]
    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) { }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubAdminSsoService(UserId? exchangeUserId = null) : IAdminSsoService {
        public int CreateCodeCallCount { get; private set; }

        public Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default) {
            CreateCodeCallCount++;
            return Task.FromResult(new AdminSsoCode("admin-sso-code", DateTime.UtcNow.AddMinutes(5)));
        }

        public Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Equals(code, "admin-sso-code", StringComparison.Ordinal) ? exchangeUserId : null);
    }

    private static LinkTelegramCommandHandler CreateLinkTelegramHandler(
        StubUserRepository userRepository,
        StubTelegramAuthValidator? telegramAuthValidator = null) =>
        new(userRepository, userRepository, telegramAuthValidator ?? new StubTelegramAuthValidator());

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User? user = null, params User[] otherUsers)
        : IUserRepository,
            IAuthenticationUserLookupService,
            IAuthenticationUserMutationService,
            IAuthenticationUserRegistrationService,
            IUserContextService {
        private readonly List<User> _users = user is null ? [.. otherUsers] : [user, .. otherUsers];

        public int AddCallCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate => string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate => candidate is { IsActive: true, DeletedAt: null } && candidate.Id == id));
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate =>
                candidate is { IsActive: true, DeletedAt: null, TelegramUserId: not null } &&
                candidate.TelegramUserId == telegramUserId));
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate =>
                candidate.TelegramUserId.HasValue &&
                candidate.TelegramUserId == telegramUserId));
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>([.. names.Select(Role.Create)]);

        public Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
            GetRolesByNamesAsync(names, cancellationToken);

        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) {
            AddCallCount++;
            _users.Add(userToAdd);
            return Task.FromResult(userToAdd);
        }

        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? accessibleUser = _users.FirstOrDefault(candidate => candidate is { IsActive: true, DeletedAt: null } && candidate.Id == userId);
            Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(accessibleUser);
            return Task.FromResult(accessError is not null
                ? Result.Failure<User>(accessError)
                : Result.Success(accessibleUser!));
        }

        public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? result.Error : null;
        }

        public Task UpdateUserAsync(User userToUpdate, CancellationToken cancellationToken) => UpdateAsync(userToUpdate, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class DirectUserByIdRepository(User? user = null)
        : IUserRepository, IAuthenticationUserLookupService, IAuthenticationUserMutationService {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user?.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubPasswordHasher : IPasswordHasher {
        public string Hash(string password) => password;

        public bool Verify(string password, string hashedPassword) => string.Equals(password, hashedPassword, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubAuthenticationTokenService : IAuthenticationTokenService {
        public User? LastUser { get; private set; }

        public Task<IssuedAuthenticationTokens> IssueAndStoreAsync(
            User user,
            CancellationToken cancellationToken,
            AuthenticationClientContext? clientContext = null,
            bool rememberMe = false,
            Guid? refreshSessionId = null) {
            LastUser = user;
            return Task.FromResult(new IssuedAuthenticationTokens("access", "refresh"));
        }

        public string IssueAccessToken(User user) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubNotificationRepository(params Notification[] notifications)
        : INotificationLookupRepository, INotificationReadModelRepository, INotificationWriteRepository {
        public List<Notification> Notifications { get; } = [.. notifications];

        public Task<IReadOnlyList<NotificationReadModel>> GetByUserReadModelsAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationReadModel>>([.. Notifications
                .Where(notification => notification.UserId == userId)
                .Take(limit)
                .Select(notification => new NotificationReadModel(
                    notification.Id.Value,
                    notification.Type,
                    notification.ReferenceId,
                    notification.PayloadJson,
                    notification.IsRead,
                    notification.CreatedOnUtc))]);

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Notification?>(Notifications.FirstOrDefault(x => x.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default) {
            Notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Any(x => x.UserId == userId && string.Equals(x.Type, type, StringComparison.Ordinal) && string.Equals(x.ReferenceId, referenceId, StringComparison.Ordinal)));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead && string.Equals(x.Type, type, StringComparison.Ordinal)));

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

    [ExcludeFromCodeCoverage]
    private sealed class StubNotificationWriter(INotificationWriteRepository notificationRepository) : INotificationWriter {
        public async Task AddAsync(
            Notification notification,
            bool sendWebPush = false,
            CancellationToken cancellationToken = default) =>
            await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubEmailVerificationNotifier(bool throwOnNotify = false) : IEmailVerificationNotifier {
        public Guid? LastUserId { get; private set; }

        public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) {
            if (throwOnNotify) {
                throw new InvalidOperationException("notification failed");
            }

            LastUserId = userId;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubEmailSender(bool throwOnEmailVerification = false, bool throwOnPasswordReset = false) : IEmailSender {
        public EmailVerificationMessage? LastEmailVerification { get; private set; }
        public PasswordResetMessage? LastPasswordReset { get; private set; }

        public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) {
            if (throwOnEmailVerification) {
                throw new InvalidOperationException("smtp failed");
            }

            LastEmailVerification = message;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken) {
            if (throwOnPasswordReset) {
                throw new InvalidOperationException("smtp failed");
            }

            LastPasswordReset = message;
            return Task.CompletedTask;
        }

        public Task SendTestEmailAsync(TestEmailMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubTelegramAuthValidator(bool validateFailure = false) : ITelegramAuthValidator {
        public FoodDiary.Results.Result<TelegramInitData> ValidateInitData(string initData) =>
            validateFailure
                ? FoodDiary.Results.Result.Failure<TelegramInitData>(
                    Errors.Validation.Invalid("initData", "Invalid Telegram init data."))
                : FoodDiary.Results.Result.Success(
                    new TelegramInitData(123456, "alex", "Alex", "User", PhotoUrl: null, "en", DateTime.UtcNow));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubTelegramLoginWidgetValidator(bool validateFailure = false) : ITelegramLoginWidgetValidator {
        public FoodDiary.Results.Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data) =>
            validateFailure
                ? FoodDiary.Results.Result.Failure<TelegramInitData>(
                    Errors.Validation.Invalid("hash", "Invalid Telegram login widget hash."))
                : FoodDiary.Results.Result.Success(
                    new TelegramInitData(data.Id, data.Username, data.FirstName, data.LastName, data.PhotoUrl, LanguageCode: null, DateTime.UtcNow));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubGoogleTokenValidator(GoogleIdentityPayload payload, bool validateFailure = false) : IGoogleTokenValidator {
        public Task<FoodDiary.Results.Result<GoogleIdentityPayload>> ValidateCredentialAsync(
            string credential,
            CancellationToken cancellationToken) =>
            Task.FromResult(validateFailure
                ? FoodDiary.Results.Result.Failure<GoogleIdentityPayload>(
                    Errors.Validation.Invalid("credential", "Invalid Google credential."))
                : FoodDiary.Results.Result.Success(payload));
    }

    private static ResendEmailVerificationCommandHandler CreateResendEmailVerificationHandler(
        User user,
        StubEmailSender sender) =>
        new(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider());

    [ExcludeFromCodeCoverage]
    private sealed class ImmediatePostCommitActionQueue : IPostCommitActionQueue {
        public bool HasActions => false;

        public void Enqueue(string actionName, Func<CancellationToken, Task> action) {
            action(CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
