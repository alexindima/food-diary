using System.Text.Json;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Assets;
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
            new StubImageAssetRepository(),
            new StubImageStorageService());

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

    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate) => Task.CompletedTask;
    }

    private sealed class StubImageAssetRepository : IImageAssetRepository {
        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) => Task.FromResult<ImageAsset?>(null);
        public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> IsAssetInUse(ImageAssetId id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ImageAsset>>([]);
    }

    private sealed class StubImageStorageService : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(UserId userId, string fileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
