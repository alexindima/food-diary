using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
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
            UserId: user.Id.Value,
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            Weight: null,
            Height: null,
            ActivityLevel: null,
            StepGoal: null,
            HydrationGoal: null,
            Language: null,
            Theme: null,
            UiStyle: null,
            PushNotificationsEnabled: null,
            FastingPushNotificationsEnabled: null,
            SocialPushNotificationsEnabled: null,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: layout,
            IsActive: null);

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
        user.UpdateProfileMedia(new FoodDiary.Domain.ValueObjects.UserProfileMediaUpdate(ProfileImageAssetId: oldAssetId));

        var cleanup = new StubImageAssetCleanupService("storage_error");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            cleanup);

        var newAssetId = ImageAssetId.New();
        var command = new UpdateUserCommand(
            UserId: user.Id.Value,
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            Weight: null,
            Height: null,
            ActivityLevel: null,
            StepGoal: null,
            HydrationGoal: null,
            Language: null,
            Theme: null,
            UiStyle: null,
            PushNotificationsEnabled: null,
            FastingPushNotificationsEnabled: null,
            SocialPushNotificationsEnabled: null,
            ProfileImage: null,
            ProfileImageAssetId: newAssetId.Value,
            DashboardLayout: null,
            IsActive: null);

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
                UserId: user.Id.Value,
                Username: null,
                FirstName: null,
                LastName: null,
                BirthDate: null,
                Gender: null,
                Weight: null,
                Height: null,
                ActivityLevel: null,
                StepGoal: null,
                HydrationGoal: null,
                Language: null,
                Theme: null,
                UiStyle: null,
                PushNotificationsEnabled: null,
                FastingPushNotificationsEnabled: null,
                SocialPushNotificationsEnabled: null,
                ProfileImage: null,
                ProfileImageAssetId: Guid.Empty,
                DashboardLayout: null,
                IsActive: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProfileImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithTheme_UpdatesUserTheme() {
        var user = User.Create("user@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService());

        var command = new UpdateUserCommand(
            UserId: user.Id.Value,
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            Weight: null,
            Height: null,
            ActivityLevel: null,
            StepGoal: null,
            HydrationGoal: null,
            Language: null,
            Theme: "leaf",
            UiStyle: "modern",
            PushNotificationsEnabled: null,
            FastingPushNotificationsEnabled: null,
            SocialPushNotificationsEnabled: null,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: null,
            IsActive: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("leaf", user.Theme);
        Assert.Equal("leaf", result.Value.Theme);
        Assert.Equal("modern", user.UiStyle);
        Assert.Equal("modern", result.Value.UiStyle);
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
