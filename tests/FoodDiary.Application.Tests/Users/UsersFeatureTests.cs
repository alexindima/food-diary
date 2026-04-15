using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Queries.GetProfileOverview;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

public class UsersFeatureTests {
    [Fact]
    public async Task ChangePasswordHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ChangePasswordCommandHandler(
            new SingleUserRepository(User.Create("user@example.com", "hash")),
            new PassthroughPasswordHasher());

        var result = await handler.Handle(
            new ChangePasswordCommand(Guid.Empty, "old", "new"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteUserHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new DeleteUserCommandHandler(
            new SingleUserRepository(user),
            new FixedDateTimeProvider(DateTime.UtcNow),
            new NullAuditLogger());

        var result = await handler.Handle(new DeleteUserCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteUserHandler_UsesDateTimeProvider() {
        var user = User.Create("user@example.com", "hash");
        var deletedAtUtc = new DateTime(2026, 2, 23, 10, 30, 0, DateTimeKind.Utc);
        var handler = new DeleteUserCommandHandler(
            new SingleUserRepository(user),
            new FixedDateTimeProvider(deletedAtUtc),
            new NullAuditLogger());

        var result = await handler.Handle(new DeleteUserCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(deletedAtUtc, user.DeletedAt);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task ChangePasswordHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive@example.com", "hash");
        user.Deactivate();
        var handler = new ChangePasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "hash", "new"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetUserByIdHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetUserByIdQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetUserByIdQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetProfileOverviewHandler_ReturnsAggregatedProfileState() {
        var user = User.Create("user@example.com", "hash");
        var invitation = DietologistInvitation.Create(
            user.Id,
            "dietologist@example.com",
            "token-hash",
            DateTime.UtcNow.AddDays(7),
            new DietologistPermissions(true, false, true, false, true, false, true, true));
        var subscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/current",
            "p256dh",
            "auth",
            locale: "en",
            userAgent: "Chrome");

        var handler = new GetProfileOverviewQueryHandler(
            new SingleUserRepository(user),
            new FixedWebPushSubscriptionRepository([subscription]),
            new FixedDietologistInvitationRepository(invitation));

        var result = await handler.Handle(new GetProfileOverviewQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Email, result.Value.User.Email);
        Assert.Equal(user.PushNotificationsEnabled, result.Value.NotificationPreferences.PushNotificationsEnabled);
        Assert.Single(result.Value.WebPushSubscriptions);
        Assert.Equal("push.example.com", result.Value.WebPushSubscriptions[0].EndpointHost);
        Assert.NotNull(result.Value.DietologistRelationship);
        Assert.Equal("dietologist@example.com", result.Value.DietologistRelationship!.Email);
        Assert.Equal("Pending", result.Value.DietologistRelationship.Status);
        Assert.True(result.Value.DietologistRelationship.Permissions.ShareProfile);
        Assert.True(result.Value.DietologistRelationship.Permissions.ShareFasting);
    }

    [Fact]
    public async Task GetDesiredWaistQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetDesiredWaistQueryValidator();
        var result = await validator.ValidateAsync(new GetDesiredWaistQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDesiredWeightQueryValidator_WithNullUserId_Fails() {
        var validator = new GetDesiredWeightQueryValidator();
        var result = await validator.ValidateAsync(new GetDesiredWeightQuery(null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetUserGoalsQueryValidator_WithValidUserId_Passes() {
        var validator = new GetUserGoalsQueryValidator();
        var result = await validator.ValidateAsync(new GetUserGoalsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class PassthroughPasswordHasher : IPasswordHasher {
        public string Hash(string password) => password;
        public bool Verify(string password, string hashedPassword) => password == hashedPassword;
    }

    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType, string? targetId, string? details) { }
    }

    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            bool includeDeleted,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedWebPushSubscriptionRepository(IReadOnlyList<WebPushSubscription> subscriptions) : IWebPushSubscriptionRepository {
        public Task<WebPushSubscription?> GetByEndpointAsync(string endpoint, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(subscriptions.FirstOrDefault(item => item.Endpoint == endpoint));

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscription>>(subscriptions.Where(item => item.UserId == userId).ToList());

        public Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptionsToDelete, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedDietologistInvitationRepository(DietologistInvitation? invitation) : IDietologistInvitationRepository {
        public Task<DietologistInvitation?> GetByIdAsync(DietologistInvitationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(invitation?.Id == id ? invitation : null);

        public Task<DietologistInvitation?> GetByClientAndStatusAsync(
            UserId clientUserId,
            DietologistInvitationStatus status,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                invitation is not null && invitation.ClientUserId == clientUserId && invitation.Status == status
                    ? invitation
                    : null);

        public Task<DietologistInvitation?> GetActiveByClientAsync(
            UserId clientUserId,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                invitation is not null && invitation.ClientUserId == clientUserId && invitation.Status == DietologistInvitationStatus.Accepted
                    ? invitation
                    : null);

        public Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
            UserId clientUserId,
            UserId dietologistUserId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<DietologistInvitation>> GetActiveByDietologistAsync(
            UserId dietologistUserId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasActiveRelationshipAsync(
            UserId clientUserId,
            UserId dietologistUserId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<DietologistInvitation> AddAsync(DietologistInvitation invitationToAdd, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(DietologistInvitation invitationToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
