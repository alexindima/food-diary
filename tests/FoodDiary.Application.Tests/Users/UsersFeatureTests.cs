using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Domain.Entities.Users;
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
}
