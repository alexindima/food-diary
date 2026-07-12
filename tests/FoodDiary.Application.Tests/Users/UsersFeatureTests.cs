using System.Text.Json;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.SetPassword;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateUserAppearance;
using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Queries.GetProfileOverview;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;
using FluentValidation.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public partial class UsersFeatureTests {

    [Fact]
    public void UserMappings_ToModel_UsesDefaultPreferencesWhenMissing() {
        var user = User.Create("default-preferences@example.com", "hash");

        UserModel model = user.ToModel();

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
    public void UserMappings_ToModel_WithInvalidDashboardLayoutJson_IgnoresLayout() {
        var user = User.Create("invalid-layout@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: "{invalid-json"));

        UserModel model = user.ToModel();

        Assert.Null(model.DashboardLayout);
    }

    [Fact]
    public void UserMappings_ToModelAndToAdminModel_MapProfilePreferencesGoalsAndRoles() {
        User user = CreateMappedUser();

        UserModel model = user.ToModel();
        var goals = user.ToGoalsModel();
        AdminUserModel adminModel = user.ToAdminModel();

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
        user.UpdateGoals(new UserGoalUpdate(2200, 130, 70, 240, 32, 2.5, 78, 84, CalorieCyclingEnabled: true, 2100, 2150, 2200, 2250, 2300, 2350, 2050));
        string layoutJson = JsonSerializer.Serialize(new DashboardLayoutModel(["meals", "weight"], ["summary"]));
        user.UpdatePreferences(new UserPreferenceUpdate(layoutJson, "ru", "dark", "modern", PushNotificationsEnabled: true, FastingPushNotificationsEnabled: false, SocialPushNotificationsEnabled: false, 10, 18));
        user.ReplaceRoles([Role.Create(RoleNames.Admin), Role.Create(RoleNames.Support)]);
        user.SetEmailConfirmed(isConfirmed: true);
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

        Result result = await handler.Handle(
            new ChangePasswordCommand(Guid.Empty, "old", "new"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ChangePasswordHandler_WithWrongCurrentPassword_ReturnsInvalidPassword() {
        var user = User.Create("wrong-password@example.com", "old-hash");
        var handler = new ChangePasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "wrong-password", "new-password"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.InvalidPassword", result.Error.Code);
        Assert.Equal("old-hash", user.Password);
    }

    [Fact]
    public async Task ChangePasswordHandler_WithValidCurrentPassword_UpdatesPassword() {
        var user = User.Create("change-password@example.com", "old-password");
        var repository = new SingleUserRepository(user);
        var handler = new ChangePasswordCommandHandler(
            repository,
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "old-password", "new-password"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("new-password", user.Password);
        Assert.Same(user, repository.UpdatedUser);
    }

    [Fact]
    public async Task DeleteUserHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new DeleteUserCommandHandler(
            new SingleUserRepository(user),
            new FixedDateTimeProvider(DateTime.UtcNow),
            new NullAuditLogger());

        Result result = await handler.Handle(new DeleteUserCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
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

        Result result = await handler.Handle(new DeleteUserCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(deletedAtUtc, user.DeletedAt);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task DeleteUserHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("delete-access@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteUserCommandHandler(
            new SingleUserRepository(user),
            new FixedDateTimeProvider(DateTime.UtcNow),
            new NullAuditLogger());

        Result result = await handler.Handle(new DeleteUserCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteUserHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new DeleteUserCommandHandler(
            CreateAccessCheckedFailingUserContext(userId),
            new FixedDateTimeProvider(DateTime.UtcNow),
            new NullAuditLogger());

        Result result = await handler.Handle(new DeleteUserCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ChangePasswordHandler_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive@example.com", "hash");
        user.Deactivate();
        var handler = new ChangePasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "hash", "new"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ChangePasswordHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var handler = new ChangePasswordCommandHandler(
            userContextService,
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new ChangePasswordCommand(userId.Value, "old", "new"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ChangePasswordHandler_WithoutConfiguredPassword_ReturnsConflict() {
        var user = User.Create("google@example.com", "hash", hasPassword: false);
        var handler = new ChangePasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "old", "new"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.PasswordNotSet", result.Error.Code);
    }

    [Fact]
    public async Task SetPasswordHandler_ForGoogleOnlyAccount_SetsPassword() {
        var user = User.Create("google@example.com", "hash", hasPassword: false);
        var handler = new SetPasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new SetPasswordCommand(user.Id.Value, "new-password"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.HasPassword);
        Assert.Equal("new-password", user.Password);
    }

    [Fact]
    public async Task SetPasswordHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("set-password-empty@example.com", "hash", hasPassword: false);
        var handler = new SetPasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new SetPasswordCommand(Guid.Empty, "new-password"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task SetPasswordHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("set-password-deleted@example.com", "hash", hasPassword: false);
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new SetPasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new SetPasswordCommand(user.Id.Value, "new-password"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task SetPasswordHandler_WhenPasswordAlreadyExists_ReturnsConflict() {
        var user = User.Create("user@example.com", "hash");
        var handler = new SetPasswordCommandHandler(
            new SingleUserRepository(user),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new SetPasswordCommand(user.Id.Value, "new-password"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.PasswordAlreadySet", result.Error.Code);
    }

    [Fact]
    public async Task SetPasswordHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new SetPasswordCommandHandler(
            CreateAccessCheckedFailingUserContext(userId),
            new PassthroughPasswordHasher());

        Result result = await handler.Handle(
            new SetPasswordCommand(userId.Value, "new-password"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(User.Create("user@example.com", "hash")));

        Result<UserModel> result = await handler.Handle(new UpdateUserAppearanceCommand(UserId: null, "dark", UiStyle: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("appearance-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        Result<UserModel> result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, "dark", UiStyle: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new UpdateUserAppearanceCommandHandler(CreateAccessCheckedFailingUserContext(userId));

        Result<UserModel> result = await handler.Handle(new UpdateUserAppearanceCommand(userId.Value, "dark", UiStyle: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithInvalidTheme_ReturnsValidationFailure() {
        var user = User.Create("appearance-theme@example.com", "hash");
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        Result<UserModel> result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, "invalid", UiStyle: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("theme", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithInvalidUiStyle_ReturnsValidationFailure() {
        var user = User.Create("appearance-style@example.com", "hash");
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        Result<UserModel> result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, Theme: null, "invalid"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("style", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserAppearanceHandler_WithValidValues_UpdatesPreferences() {
        var user = User.Create("appearance-success@example.com", "hash");
        var handler = new UpdateUserAppearanceCommandHandler(new SingleUserRepository(user));

        Result<UserModel> result = await handler.Handle(new UpdateUserAppearanceCommand(user.Id.Value, "dark", "modern"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("dark", result.Value.Theme);
        Assert.Equal("modern", result.Value.UiStyle);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(User.Create("desired-weight@example.com", "hash")));

        Result<UserDesiredWeightModel> result = await handler.Handle(new UpdateDesiredWeightCommand(UserId: null, 75), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(user));

        Result<UserDesiredWeightModel> result = await handler.Handle(new UpdateDesiredWeightCommand(user.Id.Value, 75), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new UpdateDesiredWeightCommandHandler(CreateAccessCheckedFailingUserContext(userId));

        Result<UserDesiredWeightModel> result = await handler.Handle(new UpdateDesiredWeightCommand(userId.Value, 75), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWeightHandler_WithValidValue_UpdatesUser() {
        var user = User.Create("desired-weight-success@example.com", "hash");
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(user));

        Result<UserDesiredWeightModel> result = await handler.Handle(new UpdateDesiredWeightCommand(user.Id.Value, 72.5), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(72.5, result.Value.DesiredWeight);
        Assert.Equal(72.5, user.DesiredWeight);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(User.Create("desired-waist@example.com", "hash")));

        Result<UserDesiredWaistModel> result = await handler.Handle(new UpdateDesiredWaistCommand(Guid.Empty, 80), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(user));

        Result<UserDesiredWaistModel> result = await handler.Handle(new UpdateDesiredWaistCommand(user.Id.Value, 80), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new UpdateDesiredWaistCommandHandler(CreateAccessCheckedFailingUserContext(userId));

        Result<UserDesiredWaistModel> result = await handler.Handle(new UpdateDesiredWaistCommand(userId.Value, 80), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithValidValue_UpdatesUser() {
        var user = User.Create("desired-waist-success@example.com", "hash");
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(user));

        Result<UserDesiredWaistModel> result = await handler.Handle(new UpdateDesiredWaistCommand(user.Id.Value, 78.5), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(78.5, result.Value.DesiredWaist);
        Assert.Equal(78.5, user.DesiredWaist);
    }

    [Fact]
    public async Task GetUserByIdHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetUserByIdQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<UserModel> result = await handler.Handle(new GetUserByIdQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetUserByIdHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetUserByIdQueryHandler(
            new SingleUserRepository(User.Create("query-empty@example.com", "hash")),
            new SingleUserRepository(User.Create("query-empty@example.com", "hash")));

        Result<UserModel> result = await handler.Handle(new GetUserByIdQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(User.Create("goals-missing@example.com", "hash")));

        Result<GoalsModel> result = await handler.Handle(
            new UpdateGoalsCommand(
                UserId: null,
                DailyCalorieTarget: 2000,
                ProteinTarget: null,
                FatTarget: null,
                CarbTarget: null,
                FiberTarget: null,
                WaterGoal: null,
                DesiredWeight: null,
                DesiredWaist: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithValidValues_UpdatesUserAndReturnsGoals() {
        var user = User.Create("goals-success@example.com", "hash");
        var repository = new SingleUserRepository(user);
        var handler = new UpdateGoalsCommandHandler(repository);

        Result<GoalsModel> result = await handler.Handle(
            new UpdateGoalsCommand(
                user.Id.Value,
                DailyCalorieTarget: 2000,
                ProteinTarget: 120,
                FatTarget: 60,
                CarbTarget: 220,
                FiberTarget: 25,
                WaterGoal: 2.2,
                DesiredWeight: 72,
                DesiredWaist: 78),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2000, result.Value.DailyCalorieTarget);
        Assert.Equal(120, result.Value.ProteinTarget);
        Assert.Equal(72, result.Value.DesiredWeight);
        Assert.Same(user, repository.UpdatedUser);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithInvalidDesiredWeight_ReturnsValidationFailure() {
        var user = User.Create("goals-invalid-weight@example.com", "hash");
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(user));

        Result<GoalsModel> result = await handler.Handle(
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

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithInvalidDayCalories_ReturnsValidationFailure() {
        var user = User.Create("goals-invalid-day-calories@example.com", "hash");
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(user));

        Result<GoalsModel> result = await handler.Handle(
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

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("goals-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateGoalsCommandHandler(new SingleUserRepository(user));

        Result<GoalsModel> result = await handler.Handle(
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

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateGoalsHandler_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var handler = new UpdateGoalsCommandHandler(userContextService);

        Result<GoalsModel> result = await handler.Handle(
            new UpdateGoalsCommand(
                userId.Value,
                DailyCalorieTarget: 2000,
                ProteinTarget: null,
                FatTarget: null,
                CarbTarget: null,
                FiberTarget: null,
                WaterGoal: null,
                DesiredWeight: null,
                DesiredWaist: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProfileOverviewHandler_ReturnsAggregatedProfileState() {
        var user = User.Create("user@example.com", "hash");
        var invitation = DietologistInvitation.Create(
            user.Id,
            "dietologist@example.com",
            "token-hash",
            DateTime.UtcNow.AddDays(7),
            new DietologistPermissions(ShareMeals: true, ShareStatistics: false, ShareWeight: true, ShareWaist: false, ShareGoals: true, ShareHydration: false, ShareProfile: true, ShareFasting: true));
        var subscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/current",
            "p256dh",
            "auth",
            locale: "en",
            userAgent: "Chrome");

        var handler = new GetProfileOverviewQueryHandler(
            new ProfileOverviewReadService(
                new SingleUserRepository(user),
                new WebPushSubscriptionReadService(new FixedWebPushSubscriptionRepository([subscription])),
                new FixedDietologistInvitationRepository(invitation)),
            new SingleUserRepository(user));

        Result<ProfileOverviewModel> result = await handler.Handle(new GetProfileOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
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
    public async Task GetProfileOverviewHandler_WithMissingUserId_ReturnsInvalidToken() {
        var user = User.Create("overview-missing@example.com", "hash");
        var handler = new GetProfileOverviewQueryHandler(
            new ProfileOverviewReadService(
                new SingleUserRepository(user),
                new WebPushSubscriptionReadService(new FixedWebPushSubscriptionRepository([])),
                new FixedDietologistInvitationRepository(invitation: null)),
            new SingleUserRepository(user));

        Result<ProfileOverviewModel> result = await handler.Handle(new GetProfileOverviewQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProfileOverviewHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("overview-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetProfileOverviewQueryHandler(
            new ProfileOverviewReadService(
                new SingleUserRepository(user),
                new WebPushSubscriptionReadService(new FixedWebPushSubscriptionRepository([])),
                new FixedDietologistInvitationRepository(invitation: null)),
            new SingleUserRepository(user));

        Result<ProfileOverviewModel> result = await handler.Handle(new GetProfileOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task ProfileOverviewReadService_WhenUserReadFails_ReturnsFailure() {
        var userId = UserId.New();
        IUserProfileReadService userProfileReadService = Substitute.For<IUserProfileReadService>();
        userProfileReadService
            .GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<UserModel>(Errors.Authentication.InvalidToken)));
        var service = new ProfileOverviewReadService(
            userProfileReadService,
            new WebPushSubscriptionReadService(new FixedWebPushSubscriptionRepository([])),
            new FixedDietologistInvitationRepository(invitation: null));

        Result<ProfileOverviewModel> result = await service.GetAsync(userId, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ProfileOverviewReadService_WhenPreferencesReadFails_ReturnsFailure() {
        var user = User.Create("overview-preferences-failure@example.com", "hash");
        IUserProfileReadService userProfileReadService = Substitute.For<IUserProfileReadService>();
        userProfileReadService
            .GetUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(user.ToModel())));
        userProfileReadService
            .GetNotificationPreferencesAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<NotificationPreferencesModel>(Errors.Authentication.InvalidToken)));
        var service = new ProfileOverviewReadService(
            userProfileReadService,
            new WebPushSubscriptionReadService(new FixedWebPushSubscriptionRepository([])),
            new FixedDietologistInvitationRepository(invitation: null));

        Result<ProfileOverviewModel> result = await service.GetAsync(user.Id, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWaistQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetDesiredWaistQueryValidator();
        ValidationResult result = await validator.ValidateAsync(new GetDesiredWaistQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDesiredWeightQueryValidator_WithNullUserId_Fails() {
        var validator = new GetDesiredWeightQueryValidator();
        ValidationResult result = await validator.ValidateAsync(new GetDesiredWeightQuery(UserId: null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDesiredWeightHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetDesiredWeightQueryHandler(
            new SingleUserRepository(User.Create("desired-weight-query@example.com", "hash")),
            new SingleUserRepository(User.Create("desired-weight-query@example.com", "hash")));

        Result<UserDesiredWeightModel> result = await handler.Handle(new GetDesiredWeightQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWeightHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-weight-query@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetDesiredWeightQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<UserDesiredWeightModel> result = await handler.Handle(new GetDesiredWeightQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWeightHandler_ReturnsCurrentDesiredWeight() {
        var user = User.Create("desired-weight-query-success@example.com", "hash");
        user.UpdateDesiredWeight(74.5);
        var handler = new GetDesiredWeightQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<UserDesiredWeightModel> result = await handler.Handle(new GetDesiredWeightQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(74.5, result.Value.DesiredWeight);
    }

    [Fact]
    public async Task GetUserGoalsQueryValidator_WithValidUserId_Passes() {
        var validator = new GetUserGoalsQueryValidator();
        ValidationResult result = await validator.ValidateAsync(new GetUserGoalsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetDesiredWaistHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetDesiredWaistQueryHandler(
            new SingleUserRepository(User.Create("desired-waist-query@example.com", "hash")),
            new SingleUserRepository(User.Create("desired-waist-query@example.com", "hash")));

        Result<UserDesiredWaistModel> result = await handler.Handle(new GetDesiredWaistQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWaistHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-desired-waist-query@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetDesiredWaistQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<UserDesiredWaistModel> result = await handler.Handle(new GetDesiredWaistQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetDesiredWaistHandler_ReturnsCurrentDesiredWaist() {
        var user = User.Create("desired-waist-query-success@example.com", "hash");
        user.UpdateDesiredWaist(79.5);
        var handler = new GetDesiredWaistQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<UserDesiredWaistModel> result = await handler.Handle(new GetDesiredWaistQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(79.5, result.Value.DesiredWaist);
    }

    [Fact]
    public async Task GetUserGoalsHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetUserGoalsQueryHandler(
            new SingleUserRepository(User.Create("goals-query@example.com", "hash")),
            new SingleUserRepository(User.Create("goals-query@example.com", "hash")));

        Result<GoalsModel> result = await handler.Handle(new GetUserGoalsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetUserGoalsHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-goals-query@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetUserGoalsQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<GoalsModel> result = await handler.Handle(new GetUserGoalsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
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
        var handler = new GetUserGoalsQueryHandler(new SingleUserRepository(user), new SingleUserRepository(user));

        Result<GoalsModel> result = await handler.Handle(new GetUserGoalsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2100, result.Value.DailyCalorieTarget);
        Assert.Equal(73, result.Value.DesiredWeight);
        Assert.Equal(78, result.Value.DesiredWaist);
    }

    [Fact]
    public async Task UserContextService_GetGoalsAsync_WhenUserMissing_ReturnsFailure() {
        var userId = UserId.New();
        IUserLookupRepository userLookupRepository = Substitute.For<IUserLookupRepository>();
        userLookupRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));
        var service = new UserContextService(userLookupRepository, Substitute.For<IUserWriteRepository>());

        Result<GoalsModel> result = await service.GetGoalsAsync(userId, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UserContextService_GetNotificationPreferencesAsync_WhenUserMissing_ReturnsFailure() {
        var userId = UserId.New();
        IUserLookupRepository userLookupRepository = Substitute.For<IUserLookupRepository>();
        userLookupRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));
        var service = new UserContextService(userLookupRepository, Substitute.For<IUserWriteRepository>());

        Result<NotificationPreferencesModel> result = await service.GetNotificationPreferencesAsync(userId, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UserContextService_GetGoalsAsync_WhenUserAccessible_ReturnsGoals() {
        var user = User.Create("goals-context@example.com", "hash");
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2150,
            ProteinTarget: 125,
            FatTarget: 65,
            CarbTarget: 250,
            FiberTarget: 28,
            WaterGoal: 2.2,
            DesiredWeight: 72,
            DesiredWaist: 77));
        IUserLookupRepository userLookupRepository = Substitute.For<IUserLookupRepository>();
        userLookupRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        var service = new UserContextService(userLookupRepository, Substitute.For<IUserWriteRepository>());

        Result<GoalsModel> result = await service.GetGoalsAsync(user.Id, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2150, result.Value.DailyCalorieTarget);
        Assert.Equal(72, result.Value.DesiredWeight);
        Assert.Equal(77, result.Value.DesiredWaist);
    }

    private static IUserContextService CreateAccessCheckedFailingUserContext(UserId userId) {
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        return userContextService;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
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
    private sealed class SingleUserRepository(User user) : IUserRepository, IUserContextService, IUserProfileReadService {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? foundUser = user.Id == userId ? user : null;
            Error? error = CurrentUserAccessPolicy.EnsureCanAccess(foundUser);
            return Task.FromResult(error is not null ? Result.Failure<User>(error) : Result.Success(foundUser!));
        }

        public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? result.Error : null;
        }

        public async Task<Result<UserModel>> GetUserAsync(UserId userId, CancellationToken cancellationToken) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? Result.Failure<UserModel>(result.Error) : Result.Success(result.Value.ToModel());
        }

        public async Task<Result<GoalsModel>> GetGoalsAsync(UserId userId, CancellationToken cancellationToken) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? Result.Failure<GoalsModel>(result.Error) : Result.Success(result.Value.ToGoalsModel());
        }

        public async Task<Result<UserDesiredWeightModel>> GetDesiredWeightAsync(UserId userId, CancellationToken cancellationToken) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure
                ? Result.Failure<UserDesiredWeightModel>(result.Error)
                : Result.Success(new UserDesiredWeightModel(result.Value.DesiredWeight));
        }

        public async Task<Result<UserDesiredWaistModel>> GetDesiredWaistAsync(UserId userId, CancellationToken cancellationToken) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure
                ? Result.Failure<UserDesiredWaistModel>(result.Error)
                : Result.Success(new UserDesiredWaistModel(result.Value.DesiredWaist));
        }

        public async Task<Result<NotificationPreferencesModel>> GetNotificationPreferencesAsync(UserId userId, CancellationToken cancellationToken) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure) {
                return Result.Failure<NotificationPreferencesModel>(result.Error);
            }

            User currentUser = result.Value;
            return Result.Success(new NotificationPreferencesModel(
                currentUser.PushNotificationsEnabled,
                currentUser.FastingPushNotificationsEnabled,
                currentUser.SocialPushNotificationsEnabled,
                currentUser.FastingCheckInReminderHours,
                currentUser.FastingCheckInFollowUpReminderHours));
        }

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

        public User? UpdatedUser { get; private set; }

        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) {
            UpdatedUser = userToUpdate;
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User userToUpdate, CancellationToken cancellationToken) =>
            UpdateAsync(userToUpdate, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedWebPushSubscriptionRepository(IReadOnlyList<WebPushSubscription> subscriptions) : IWebPushSubscriptionRepository {
        public Task<WebPushSubscription?> GetByEndpointAsync(string endpoint, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(subscriptions.FirstOrDefault(item => string.Equals(item.Endpoint, endpoint, StringComparison.Ordinal)));

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscription>>(subscriptions.Where(item => item.UserId == userId).ToList());

        public Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscriptionReadModel>>([.. subscriptions
                .Where(subscription => subscription.UserId == userId)
                .Select(subscription => new WebPushSubscriptionReadModel(
                    subscription.Endpoint,
                    subscription.ExpirationTimeUtc,
                    subscription.Locale,
                    subscription.UserAgent,
                    subscription.CreatedOnUtc,
                    subscription.ModifiedOnUtc))]);

        public Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptionsToDelete, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDietologistInvitationRepository(DietologistInvitation? invitation)
        : IDietologistInvitationRepository, IDietologistInvitationReadService, IProfileDietologistReadService {
        public async Task<Result<ProfileDietologistRelationshipModel?>> GetRelationshipAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            Result<DietologistRelationshipModel?> result = await GetMyRelationshipAsync(userId, cancellationToken).ConfigureAwait(false);
            DietologistRelationshipModel? relationship = result.Value;
            return Result.Success(relationship is null
                ? null
                : new ProfileDietologistRelationshipModel(
                    relationship.InvitationId,
                    relationship.Status,
                    relationship.Email,
                    relationship.FirstName,
                    relationship.LastName,
                    relationship.DietologistUserId,
                    new ProfileDietologistPermissionsModel(
                        relationship.Permissions.ShareMeals,
                        relationship.Permissions.ShareStatistics,
                        relationship.Permissions.ShareWeight,
                        relationship.Permissions.ShareWaist,
                        relationship.Permissions.ShareGoals,
                        relationship.Permissions.ShareHydration,
                        relationship.Permissions.ShareProfile,
                        relationship.Permissions.ShareFasting),
                    relationship.CreatedAtUtc,
                    relationship.ExpiresAtUtc,
                    relationship.AcceptedAtUtc));
        }

        public Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            DietologistRelationshipModel? relationship = invitation is null
                ? null
                : ToReadModel(invitation).ToRelationshipModel();
            return Task.FromResult(Result.Success<DietologistRelationshipModel?>(relationship));
        }

        public Task<Result<DietologistInvitationForCurrentUserModel>> GetForCurrentUserAsync(
            UserId userId,
            Guid invitationId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<InvitationModel>> GetByTokenAsync(
            UserId userId,
            Guid invitationId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<DietologistInfoModel?>> GetMyDietologistAsync(
            UserId userId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
            UserId userId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DietologistInvitationReadModel?> GetByIdReadModelAsync(
            DietologistInvitationId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(invitation is not null && invitation.Id == id ? ToReadModel(invitation) : null);

        public Task<DietologistInvitation?> GetByIdAsync(DietologistInvitationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(invitation?.Id == id ? invitation : null);

        public Task<DietologistInvitationReadModel?> GetByClientAndStatusReadModelAsync(
            UserId clientUserId,
            DietologistInvitationStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                invitation is not null && invitation.ClientUserId == clientUserId && invitation.Status == status
                    ? ToReadModel(invitation)
                    : null);

        public Task<DietologistInvitation?> GetByClientAndStatusAsync(
            UserId clientUserId,
            DietologistInvitationStatus status,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                invitation is not null && invitation.ClientUserId == clientUserId && invitation.Status == status
                    ? invitation
                    : null);

        public Task<DietologistInvitationReadModel?> GetActiveByClientReadModelAsync(
            UserId clientUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                invitation is not null && invitation.ClientUserId == clientUserId && invitation.Status == DietologistInvitationStatus.Accepted
                    ? ToReadModel(invitation)
                    : null);

        public Task<DietologistInvitation?> GetActiveByClientAsync(
            UserId clientUserId,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                invitation is not null && invitation.ClientUserId == clientUserId && invitation.Status == DietologistInvitationStatus.Accepted
                    ? invitation
                    : null);

        public Task<DietologistInvitationReadModel?> GetActiveByClientAndDietologistReadModelAsync(
            UserId clientUserId,
            UserId dietologistUserId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<DietologistInvitation?> GetActiveByClientAndDietologistAsync(
            UserId clientUserId,
            UserId dietologistUserId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<DietologistInvitationReadModel>> GetActiveByDietologistReadModelsAsync(
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

        private static DietologistInvitationReadModel ToReadModel(DietologistInvitation invitation) =>
            new(
                invitation.Id.Value,
                invitation.ClientUserId.Value,
                invitation.DietologistUserId?.Value,
                invitation.DietologistEmail,
                invitation.ClientUser?.Email ?? "client@example.com",
                invitation.ClientUser?.FirstName,
                invitation.ClientUser?.LastName,
                invitation.ClientUser?.ProfileImage,
                invitation.ClientUser?.BirthDate,
                invitation.ClientUser?.Gender,
                invitation.ClientUser?.Height,
                invitation.ClientUser?.ActivityLevel ?? ActivityLevel.Moderate,
                invitation.DietologistUser?.Email,
                invitation.DietologistUser?.FirstName,
                invitation.DietologistUser?.LastName,
                invitation.Status,
                new DietologistPermissionsReadModel(
                    invitation.ShareMeals,
                    invitation.ShareStatistics,
                    invitation.ShareWeight,
                    invitation.ShareWaist,
                    invitation.ShareGoals,
                    invitation.ShareHydration,
                    invitation.ShareProfile,
                    invitation.ShareFasting),
                invitation.CreatedOnUtc,
                invitation.ExpiresAtUtc,
                invitation.AcceptedAtUtc);
    }
}
