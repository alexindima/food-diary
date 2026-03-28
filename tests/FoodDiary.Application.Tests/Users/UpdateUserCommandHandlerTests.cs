using System.Text.Json;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

public sealed class UpdateUserCommandHandlerTests {
    [Fact]
    public async Task Handle_WithDashboardLayout_SerializesInApplicationLayer() {
        var user = User.Create("user@example.com", "hash");
        var userRepository = new SingleUserRepository(user);
        var handler = new UpdateUserCommandHandler(
            userRepository,
            new StubImageAssetCleanupService());

        var layout = new DashboardLayoutModel(["summary", "goals"], ["water", "weight"]);
        var command = new UpdateUserCommand(
            user.Id.Value,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            layout,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.DashboardLayoutJson);
        var deserialized = JsonSerializer.Deserialize<DashboardLayoutModel>(user.DashboardLayoutJson!);
        Assert.NotNull(deserialized);
        Assert.Equal(layout.Web, deserialized.Web);
        Assert.Equal(layout.Mobile, deserialized.Mobile);
    }

    [Fact]
    public async Task Handle_WhenProfileImageCleanupFails_StillReturnsSuccessAndUpdatesUser() {
        var user = User.Create("user@example.com", "hash");
        var oldAssetId = ImageAssetId.New();
        user.UpdateProfile(new FoodDiary.Domain.ValueObjects.UserProfileUpdate(ProfileImageAssetId: oldAssetId));

        var cleanup = new StubImageAssetCleanupService("storage_error");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            cleanup);

        var newAssetId = ImageAssetId.New();
        var command = new UpdateUserCommand(
            user.Id.Value,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            newAssetId.Value,
            null,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newAssetId, user.ProfileImageAssetId);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task Handle_WithEmptyProfileImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService());

        var result = await handler.Handle(
            new UpdateUserCommand(
                user.Id.Value,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                Guid.Empty,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProfileImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubImageAssetCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(true)
                : new DeleteImageAssetResult(false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
