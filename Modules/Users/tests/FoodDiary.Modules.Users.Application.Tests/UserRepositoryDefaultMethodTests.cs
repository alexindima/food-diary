using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserRepositoryDefaultMethodTests {
    [Theory]
    [InlineData(UserAccountStatusFilter.All, true)]
    [InlineData(UserAccountStatusFilter.Active, false)]
    [InlineData(UserAccountStatusFilter.Inactive, false)]
    [InlineData(UserAccountStatusFilter.Deleted, true)]
    public async Task UserRepository_GetPagedAsync_WithStatus_DelegatesToLegacyIncludeDeletedOverload(
        UserAccountStatusFilter status,
        bool expectedIncludeDeleted) {
        var stub = new RecordingUserRepository();
        IUserAdminReadRepository repository = stub;
        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.GetPagedAsync("search", page: 2, limit: 10, status, cancellationTokenSource.Token);

        Assert.Equal("search", stub.CapturedSearch);
        Assert.Equal(2, stub.CapturedPage);
        Assert.Equal(10, stub.CapturedLimit);
        Assert.Equal(expectedIncludeDeleted, stub.CapturedIncludeDeleted);
        Assert.Equal(cancellationTokenSource.Token, stub.CapturedPagedCancellationToken);
    }

    [Fact]
    public async Task UserRepository_UpdateAsync_WithAuditEvents_DelegatesToLegacyUpdateOverload() {
        var stub = new RecordingUserRepository();
        IUserRepository repository = stub;
        var user = User.Create("user@test.com", "hashed");
        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.UpdateAsync(user, [], cancellationTokenSource.Token);

        Assert.Same(user, stub.CapturedUpdatedUser);
        Assert.Equal(cancellationTokenSource.Token, stub.CapturedUpdateCancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingUserRepository : IUserRepository, IUserAdminReadRepository, IUserAdminReadModelRepository {
        public string? CapturedSearch { get; private set; }
        public int CapturedPage { get; private set; }
        public int CapturedLimit { get; private set; }
        public bool CapturedIncludeDeleted { get; private set; }
        public CancellationToken CapturedPagedCancellationToken { get; private set; }
        public User? CapturedUpdatedUser { get; private set; }
        public CancellationToken CapturedUpdateCancellationToken { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<UserAdminReadModel?> GetByIdIncludingDeletedReadModelAsync(UserId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            bool includeDeleted,
            CancellationToken cancellationToken = default) {
            CapturedSearch = search;
            CapturedPage = page;
            CapturedLimit = limit;
            CapturedIncludeDeleted = includeDeleted;
            CapturedPagedCancellationToken = cancellationToken;
            return Task.FromResult<(IReadOnlyList<User>, int)>(([], 0));
        }

        public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetFilteredPagedReadModelsAsync(string? search, int page, int limit, UserAccountStatusFilter status, UserAdministrationFilter filter, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedReadModelsAsync(
            string? search,
            int page,
            int limit,
            UserAccountStatusFilter status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
            GetAdminDashboardSummaryReadModelsAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            CapturedUpdatedUser = user;
            CapturedUpdateCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
