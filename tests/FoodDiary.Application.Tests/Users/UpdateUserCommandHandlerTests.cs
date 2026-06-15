using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
        var handler = new UpdateUserCommandHandler(
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
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

        Result<UserModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.DashboardLayoutJson);
        DashboardLayoutModel? deserialized = JsonSerializer.Deserialize<DashboardLayoutModel>(user.DashboardLayoutJson!);
        Assert.NotNull(deserialized);
        Assert.Equal(layout.Web, deserialized.Web);
        Assert.Equal(layout.Mobile, deserialized.Mobile);
    }

    [Fact]
    public async Task Handle_WhenProfileImageCleanupFails_StillReturnsSuccessAndUpdatesUser() {
        var user = User.Create("user@example.com", "hash");
        var oldAssetId = ImageAssetId.New();
        user.UpdateProfileMedia(new FoodDiary.Domain.ValueObjects.UserProfileMediaUpdate(ProfileImageAssetId: oldAssetId));

        IImageAssetCleanupService cleanup = CreateImageAssetCleanupService("storage_error", out List<ImageAssetId> requestedAssetIds);
        var handler = new UpdateUserCommandHandler(
            CreateUserRepository(user),
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

        Result<UserModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newAssetId, user.ProfileImageAssetId);
        Assert.Equal([oldAssetId], requestedAssetIds);
    }

    [Fact]
    public async Task Handle_WithEmptyProfileImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new UpdateUserCommandHandler(
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<UserModel> result = await handler.Handle(
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
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<UserModel> result = await handler.Handle(CreateCommand(userId: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateUserCommandHandler(
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<UserModel> result = await handler.Handle(CreateCommand(user.Id.Value), CancellationToken.None);

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
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<UserModel> result = await handler.Handle(
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
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
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

        Result<UserModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("leaf", user.Theme);
        Assert.Equal("leaf", result.Value.Theme);
        Assert.Equal("modern", user.UiStyle);
        Assert.Equal("modern", result.Value.UiStyle);
    }

    [Fact]
    public async Task Handle_WhenProfileImageAccessFails_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        RecordingImageAssetAccessService imageAccess = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.NotFound(Guid.NewGuid()));
        var handler = new UpdateUserCommandHandler(
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            imageAccess);

        var assetId = Guid.NewGuid();
        Result<UserModel> result = await handler.Handle(
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
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<UserModel> result = await handler.Handle(
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
            CreateUserRepository(user),
            CreateImageAssetCleanupService(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<UserModel> result = await handler.Handle(
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

    private static IUserRepository CreateUserRepository(User user) {
        IUserRepository repository = Substitute.For<IUserRepository>();
        repository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult<User?>(user.Id == id ? user : null);
            });
        repository
            .GetByIdIncludingDeletedAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult<User?>(user.Id == id ? user : null);
            });
        repository
            .UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static IImageAssetCleanupService CreateImageAssetCleanupService(string? errorCode = null) =>
        CreateImageAssetCleanupService(errorCode, out _);

    private static IImageAssetCleanupService CreateImageAssetCleanupService(
        string? errorCode,
        out List<ImageAssetId> requestedAssetIds) {
        requestedAssetIds = [];
        List<ImageAssetId> capturedRequestedAssetIds = requestedAssetIds;

        IImageAssetCleanupService service = Substitute.For<IImageAssetCleanupService>();
        service
            .DeleteIfUnusedAsync(Arg.Do<ImageAssetId>(capturedRequestedAssetIds.Add), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(Deleted: true)
                : new DeleteImageAssetResult(Deleted: false, errorCode)));
        service
            .CleanupOrphansAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return service;
    }
}
