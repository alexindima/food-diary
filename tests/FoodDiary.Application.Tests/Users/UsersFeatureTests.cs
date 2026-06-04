using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.SetPassword;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateUserAppearance;
using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
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
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public class UsersFeatureTests {
    [Fact]
    public void UserMappings_ToModel_UsesDefaultPreferencesWhenMissing() {
        var user = User.Create("default-preferences@example.com", "hash");

        var model = user.ToModel();

        Assert.Equal(user.Id.Value, model.Id);
        Assert.Equal("default-preferences@example.com", model.Email);
        Assert.Equal("en", model.Language);
        Assert.Equal("ocean", model.Theme);
        Assert.Equal("classic", model.UiStyle);
        Assert.Null(model.DashboardLayout);
        Assert.True(model.HasPassword);
        Assert.True(model.IsActive);
    }

    [Fact]
    public void UserMappings_ToModelAndToAdminModel_MapProfilePreferencesGoalsAndRoles() {
        var user = CreateMappedUser();

        var model = user.ToModel();
        var goals = user.ToGoalsModel();
        var adminModel = user.ToAdminModel();

        Assert.Equal("mapped", model.Username);
        Assert.Equal("ru", model.Language);
        Assert.Equal("dark", model.Theme);
        Assert.Equal("modern", model.UiStyle);
        Assert.Equal(["meals", "weight"], model.DashboardLayout?.Web);
        Assert.Equal(["summary"], model.DashboardLayout?.Mobile);
        Assert.Equal(2200, goals.DailyCalorieTarget);
        Assert.Equal(2350, goals.SaturdayCalories);
        Assert.Equal(user.Id.Value, adminModel.Id);
        Assert.Equal(123456, adminModel.TelegramUserId);
        Assert.Equal(1000, adminModel.AiInputTokenLimit);
        Assert.Equal(2000, adminModel.AiOutputTokenLimit);
        Assert.NotNull(adminModel.AiConsentAcceptedAt);
        Assert.Contains(RoleNames.Admin, adminModel.Roles);
        Assert.Contains(RoleNames.Support, adminModel.Roles);
    }

    private static User CreateMappedUser() {
        var user = User.Create("mapped@example.com", "hash");
        user.UpdatePersonalInfo("mapped", "Alex", "Tester", new DateTime(1990, 1, 2), "M", 82, 181);
        user.UpdateActivity(ActivityLevel.High, stepGoal: 9000, hydrationGoal: 2.4);
        user.UpdateGoals(new UserGoalUpdate(2200, 130, 70, 240, 32, 2.5, 78, 84, true, 2100, 2150, 2200, 2250, 2300, 2350, 2050));
        var layoutJson = JsonSerializer.Serialize(new DashboardLayoutModel(["meals", "weight"], ["summary"]));
        user.UpdatePreferences(new UserPreferenceUpdate(layoutJson, "ru", "dark", "modern", true, false, false, 10, 18));
        user.ReplaceRoles([Role.Create(RoleNames.Admin), Role.Create(RoleNames.Support)]);
        user.SetEmailConfirmed(true);
        user.LinkTelegram(123456);
        user.UpdateAiTokenLimits(1000, 2000);
        user.AcceptAiConsent();
        return user;
    }

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
    public async Task ChangePasswordHandler_WithoutConfiguredPassword_ReturnsConflict() {
        var user = User.Create("google@example.com", "hash", hasPassword: false);
        var handler = new ChangePasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "old", "new"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.PasswordNotSet", result.Error.Code);
    }

    [Fact]
    public async Task SetPasswordHandler_ForGoogleOnlyAccount_SetsPassword() {
        var user = User.Create("google@example.com", "hash", hasPassword: false);
        var handler = new SetPasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        var result = await handler.Handle(
            new SetPasswordCommand(user.Id.Value, "new-password"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.HasPassword);
        Assert.Equal("new-password", user.Password);
    }

    [Fact]
    public async Task SetPasswordHandler_WhenPasswordAlreadyExists_ReturnsConflict() {
        var user = User.Create("user@example.com", "hash");
        var handler = new SetPasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        var result = await handler.Handle(
            new SetPasswordCommand(user.Id.Value, "new-password"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.PasswordAlreadySet", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(new UpdateUserAppearanceCommand(null, "dark", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("appearance-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, "dark", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithInvalidTheme_ReturnsValidationFailure() {
        var user = User.Create("appearance-theme@example.com", "hash");
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, "invalid", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("theme", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithInvalidUiStyle_ReturnsValidationFailure() {
        var user = User.Create("appearance-style@example.com", "hash");
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, null, "invalid"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("style", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithValidValues_UpdatesPreferences() {
        var user = User.Create("appearance-success@example.com", "hash");
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, "dark", "modern"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("dark", result.Value.Theme);
        Assert.Equal("modern", result.Value.UiStyle);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(User.Create("desired-weight@example.com", "hash")));

        var result = await handler.Handle(new UpdateDesiredWeightCommand(null, 75), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateDesiredWeightCommand(user.Id.Value, 75), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WithValidValue_UpdatesUser() {
        var user = User.Create("desired-weight-success@example.com", "hash");
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateDesiredWeightCommand(user.Id.Value, 72.5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(72.5, result.Value.DesiredWeight);
        Assert.Equal(72.5, user.DesiredWeight);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(User.Create("desired-waist@example.com", "hash")));

        var result = await handler.Handle(new UpdateDesiredWaistCommand(Guid.Empty, 80), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateDesiredWaistCommand(user.Id.Value, 80), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithValidValue_UpdatesUser() {
        var user = User.Create("desired-waist-success@example.com", "hash");
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new UpdateDesiredWaistCommand(user.Id.Value, 78.5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(78.5, result.Value.DesiredWaist);
        Assert.Equal(78.5, user.DesiredWaist);
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
    public async Task UpdateGoalsHandler_WithInvalidDesiredWeight_ReturnsValidationFailure() {
        var user = User.Create("goals-invalid-weight@example.com", "hash");
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(
            new UpdateGoalsCommand(
                user.Id.Value,
                DailyCalorieTarget: null,
                ProteinTarget: null,
                FatTarget: null,
                CarbTarget: null,
                FiberTarget: null,
                WaterGoal: null,
                DesiredWeight: 0,
                DesiredWaist: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithInvalidDayCalories_ReturnsValidationFailure() {
        var user = User.Create("goals-invalid-day-calories@example.com", "hash");
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(
            new UpdateGoalsCommand(
                user.Id.Value,
                DailyCalorieTarget: null,
                ProteinTarget: null,
                FatTarget: null,
                CarbTarget: null,
                FiberTarget: null,
                WaterGoal: null,
                DesiredWeight: null,
                DesiredWaist: null,
                MondayCalories: double.NaN),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("goals-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(
            new UpdateGoalsCommand(
                user.Id.Value,
                DailyCalorieTarget: 2000,
                ProteinTarget: null,
                FatTarget: null,
                CarbTarget: null,
                FiberTarget: null,
                WaterGoal: null,
                DesiredWeight: null,
                DesiredWaist: null),
            CancellationToken.None);

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
    public async Task GetDesiredWeightHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetDesiredWeightQueryHandler(new SingleUserRepository(User.Create("desired-weight-query@example.com", "hash")));

        var result = await handler.Handle(new GetDesiredWeightQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWeightHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-weight-query@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetDesiredWeightQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetDesiredWeightQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWeightHandler_ReturnsCurrentDesiredWeight() {
        var user = User.Create("desired-weight-query-success@example.com", "hash");
        user.UpdateDesiredWeight(74.5);
        var handler = new GetDesiredWeightQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetDesiredWeightQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(74.5, result.Value.DesiredWeight);
    }

    [Fact]
    public async Task GetUserGoalsQueryValidator_WithValidUserId_Passes() {
        var validator = new GetUserGoalsQueryValidator();
        var result = await validator.ValidateAsync(new GetUserGoalsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetDesiredWaistHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetDesiredWaistQueryHandler(new SingleUserRepository(User.Create("desired-waist-query@example.com", "hash")));

        var result = await handler.Handle(new GetDesiredWaistQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWaistHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-waist-query@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetDesiredWaistQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetDesiredWaistQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWaistHandler_ReturnsCurrentDesiredWaist() {
        var user = User.Create("desired-waist-query-success@example.com", "hash");
        user.UpdateDesiredWaist(79.5);
        var handler = new GetDesiredWaistQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetDesiredWaistQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(79.5, result.Value.DesiredWaist);
    }

    [Fact]
    public async Task GetUserGoalsHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetUserGoalsQueryHandler(new SingleUserRepository(User.Create("goals-query@example.com", "hash")));

        var result = await handler.Handle(new GetUserGoalsQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetUserGoalsHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-goals-query@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetUserGoalsQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetUserGoalsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetUserGoalsHandler_ReturnsCurrentGoals() {
        var user = User.Create("goals-query-success@example.com", "hash");
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2100,
            ProteinTarget: 140,
            FatTarget: 70,
            CarbTarget: 220,
            FiberTarget: 30,
            WaterGoal: 2.1,
            DesiredWeight: 73,
            DesiredWaist: 78));
        var handler = new GetUserGoalsQueryHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new GetUserGoalsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2100, result.Value.DailyCalorieTarget);
        Assert.Equal(73, result.Value.DesiredWeight);
        Assert.Equal(78, result.Value.DesiredWaist);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class PassthroughPasswordHasher : IPasswordHasher {
        public string Hash(string password) => password;
        public bool Verify(string password, string hashedPassword) => string.Equals(password, hashedPassword, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) { }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class FixedWebPushSubscriptionRepository(IReadOnlyList<WebPushSubscription> subscriptions) : IWebPushSubscriptionRepository {
        public Task<WebPushSubscription?> GetByEndpointAsync(string endpoint, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(subscriptions.FirstOrDefault(item => string.Equals(item.Endpoint, endpoint, StringComparison.Ordinal)));

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscription>>(subscriptions.Where(item => item.UserId == userId).ToList());

        public Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptionsToDelete, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
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
