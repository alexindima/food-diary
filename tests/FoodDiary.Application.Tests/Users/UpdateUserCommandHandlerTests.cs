using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UpdateUserCommandHandlerTests {
    [Fact]
    public async Task Handle_WithDashboardLayout_SerializesInApplicationLayer() {
        var user = User.Create("user@example.com", "hash");
        var userRepository = new SingleUserRepository(user);
        var handler = new UpdateUserCommandHandler(
            userRepository,
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

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
            cleanup,
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

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
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

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
    public async Task Handle_WithMissingUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var result = await handler.Handle(CreateCommand(null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var result = await handler.Handle(CreateCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Theory]
    [InlineData("not-activity", null, null, null, null, "ActivityLevel")]
    [InlineData(null, "not-language", null, null, null, "Language")]
    [InlineData(null, null, "not-theme", null, null, "Theme")]
    [InlineData(null, null, null, "not-ui-style", null, "UiStyle")]
    [InlineData(null, null, null, null, "not-gender", "Gender")]
    public async Task Handle_WithInvalidPreferences_ReturnsValidationFailure(
        string? activityLevel,
        string? language,
        string? theme,
        string? uiStyle,
        string? gender,
        string expectedField) {
        var user = User.Create("preferences-user@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var result = await handler.Handle(
            CreateCommand(
                user.Id.Value,
                activityLevel: activityLevel,
                language: language,
                theme: theme,
                uiStyle: uiStyle,
                gender: gender),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains(expectedField, result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithTheme_UpdatesUserTheme() {
        var user = User.Create("user@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

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

    [Fact]
    public async Task Handle_WhenProfileImageAccessFails_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var imageAccess = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.NotFound(Guid.NewGuid()));
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            imageAccess);

        var assetId = Guid.NewGuid();
        var result = await handler.Handle(
            CreateCommand(user.Id.Value, profileImageAssetId: assetId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.NotFound", result.Error.Code);
        Assert.Equal([new ImageAssetId(assetId)], imageAccess.RequestedAssetIds);
    }

    [Fact]
    public async Task Handle_WithIsActiveFalse_DeactivatesUser() {
        var user = User.Create("active@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var result = await handler.Handle(
            CreateCommand(user.Id.Value, isActive: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task Handle_WithIsActiveTrue_KeepsUserActive() {
        var user = User.Create("active-again@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            new SingleUserRepository(user),
            new StubImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var result = await handler.Handle(
            CreateCommand(user.Id.Value, isActive: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.True(result.Value.IsActive);
    }

    private static UpdateUserCommand CreateCommand(
        Guid? userId,
        Guid? profileImageAssetId = null,
        bool? isActive = null,
        string? activityLevel = null,
        string? language = null,
        string? theme = null,
        string? uiStyle = null,
        string? gender = null) =>
        new(
            UserId: userId,
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: gender,
            Weight: null,
            Height: null,
            ActivityLevel: activityLevel,
            StepGoal: null,
            HydrationGoal: null,
            Language: language,
            Theme: theme,
            UiStyle: uiStyle,
            PushNotificationsEnabled: null,
            FastingPushNotificationsEnabled: null,
            SocialPushNotificationsEnabled: null,
            ProfileImage: null,
            ProfileImageAssetId: profileImageAssetId,
            DashboardLayout: null,
            IsActive: isActive);

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
