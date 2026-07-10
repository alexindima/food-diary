using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.SetAdminUserPassword;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FluentValidation.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Tests.Admin;

public partial class AdminFeatureTests {
    [Fact]
    public async Task SetAdminUserPasswordValidator_WithInvalidPayload_Fails() {
        var validator = new SetAdminUserPasswordCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new SetAdminUserPasswordCommand(Guid.Empty, "123"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "UserId", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "NewPassword", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SetAdminUserPasswordHandler_WithExistingPassword_ReplacesPassword() {
        User user = CreateUserWithRoles("password-user@example.com", []);
        var userRepository = new InMemoryUserRepository(user, availableRoles: []);
        var handler = new SetAdminUserPasswordCommandHandler(userRepository, new PrefixPasswordHasher());

        Result result = await handler.Handle(
            new SetAdminUserPasswordCommand(user.Id.Value, "NewPassword123!"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.HasPassword);
        Assert.Equal("hashed:NewPassword123!", user.Password);
        Assert.Equal(1, userRepository.UpdateCallCount);
    }

    [Fact]
    public async Task SetAdminUserPasswordHandler_WithGoogleOnlyUser_SetsFirstPassword() {
        var user = User.Create("google-user@example.com", "placeholder-hash", hasPassword: false);
        var userRepository = new InMemoryUserRepository(user, availableRoles: []);
        var handler = new SetAdminUserPasswordCommandHandler(userRepository, new PrefixPasswordHasher());

        Result result = await handler.Handle(
            new SetAdminUserPasswordCommand(user.Id.Value, "FirstPassword123!"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.HasPassword);
        Assert.Equal("hashed:FirstPassword123!", user.Password);
        Assert.Equal(1, userRepository.UpdateCallCount);
    }

    [Fact]
    public async Task SetAdminUserPasswordHandler_WithEmptyUserId_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("password-empty-user@example.com", []);
        var userRepository = new InMemoryUserRepository(user, availableRoles: []);
        var handler = new SetAdminUserPasswordCommandHandler(userRepository, new PrefixPasswordHasher());

        Result result = await handler.Handle(
            new SetAdminUserPasswordCommand(Guid.Empty, "NewPassword123!"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal(0, userRepository.UpdateCallCount);
    }

    [Fact]
    public async Task SetAdminUserPasswordHandler_WhenUserMissing_ReturnsNotFound() {
        User user = CreateUserWithRoles("password-missing-user@example.com", []);
        var userRepository = new InMemoryUserRepository(user, availableRoles: []);
        var handler = new SetAdminUserPasswordCommandHandler(userRepository, new PrefixPasswordHasher());

        Result result = await handler.Handle(
            new SetAdminUserPasswordCommand(Guid.NewGuid(), "NewPassword123!"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal(0, userRepository.UpdateCallCount);
    }

    [Fact]
    public async Task UpdateAdminUserValidator_WithInvalidRole_Fails() {
        var validator = new UpdateAdminUserCommandValidator();
        var command = new UpdateAdminUserCommand(
            Guid.NewGuid(),
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: ["Unknown"],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Unknown role.", StringComparison.Ordinal));
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithUnknownRoleFromRepository_Fails() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);
        var command = new UpdateAdminUserCommand(
            user.Id.Value,
            IsActive: null,
            IsEmailConfirmed: null,
            Roles: [RoleNames.Admin, RoleNames.Support],
            Language: null,
            AiInputTokenLimit: null,
            AiOutputTokenLimit: null);

        Result<AdminUserModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("roles", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyUserId_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                Guid.Empty,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WhenUserMissing_ReturnsNotFound() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                Guid.NewGuid(),
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithInvalidLanguage_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: "klingon",
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("language", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithUnknownRequestedRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: ["MysteryRole"],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown role", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithNullRoles_DoesNotChangeRoles() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);
        string[] beforeRoles = [.. user.UserRoles.Select(r => r.Role.Name).Order(StringComparer.Ordinal)];

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        string[] afterRoles = [.. user.UserRoles.Select(r => r.Role.Name).Order(StringComparer.Ordinal)];

        ResultAssert.Success(result);
        Assert.Equal(beforeRoles, afterRoles);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyRoles_ClearsRoles() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: Array.Empty<string>(),
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(user.UserRoles);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerRoleForNonOwner_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Owner, RoleNames.Admin],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner role", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin], [.. user.GetRoleNames().Order(StringComparer.Ordinal)]);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserWithoutOwnerRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Admin],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner and Admin", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin, RoleNames.Owner], [.. user.GetRoleNames().Order(StringComparer.Ordinal)]);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserWithoutAdminRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Owner],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner and Admin", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin, RoleNames.Owner], [.. user.GetRoleNames().Order(StringComparer.Ordinal)]);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserKeepingOwnerAndAdmin_UpdatesRoles() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Support],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(
            [RoleNames.Admin, RoleNames.Owner, RoleNames.Support],
            [.. user.GetRoleNames().Order(StringComparer.Ordinal)]);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithOwnerUserDeactivation_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@fooddiary.club", [RoleNames.Owner, RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: false,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Owner user", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(user.IsActive);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WhenActorRemovesOwnAdminRole_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Premium],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: user.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("own Admin role", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([RoleNames.Admin, RoleNames.Premium], [.. user.GetRoleNames().Order(StringComparer.Ordinal)]);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WhenActorDeactivatesOwnAccount_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: false,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: user.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("own account", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(user.IsActive);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithSameRoles_DoesNotSetModifiedOnUtc() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);
        DateTime? modifiedBefore = user.ModifiedOnUtc;

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Premium, RoleNames.Admin],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(modifiedBefore, user.ModifiedOnUtc);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithRoleChanges_StoresRoleAuditEvents() {
        var actorUserId = UserId.New();
        var timestamp = new DateTime(2026, 3, 26, 11, 0, 0, DateTimeKind.Utc);
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin, RoleNames.Premium]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            new NullAuditLogger(),
            new FixedDateTimeProvider(timestamp));

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Admin, RoleNames.Support],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: actorUserId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            userRepository.RoleAuditEvents.OrderBy(auditEvent => auditEvent.RoleName, StringComparer.Ordinal),
            auditEvent => {
                Assert.Equal(UserRoleAuditAction.Removed, auditEvent.Action);
                Assert.Equal(RoleNames.Premium, auditEvent.RoleName);
                Assert.Equal(actorUserId, auditEvent.ActorUserId);
                Assert.Equal(timestamp, auditEvent.OccurredAtUtc);
            },
            auditEvent => {
                Assert.Equal(UserRoleAuditAction.Added, auditEvent.Action);
                Assert.Equal(RoleNames.Support, auditEvent.RoleName);
                Assert.Equal(actorUserId, auditEvent.ActorUserId);
                Assert.Equal(timestamp, auditEvent.OccurredAtUtc);
            });
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithEmptyActorUserId_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("admin-empty-actor@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: null,
                Roles: [RoleNames.Admin, RoleNames.Premium],
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ActorUserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithUnchangedAdminAccountFields_DoesNotSetModifiedOnUtc() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        user.SetEmailConfirmed(isConfirmed: true);
        user.SetLanguage("en");
        user.UpdateAiTokenLimits(new FoodDiary.Domain.ValueObjects.UserAiTokenLimitUpdate(
            InputLimit: 123,
            OutputLimit: 456));
        DateTime? modifiedBefore = user.ModifiedOnUtc;
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: null,
                IsEmailConfirmed: true,
                Roles: null,
                Language: "en",
                AiInputTokenLimit: 123,
                AiOutputTokenLimit: 456),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(modifiedBefore, user.ModifiedOnUtc);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithDeletedUserAndActiveToggle_ReturnsValidationFailure() {
        User user = CreateUserWithRoles("deleted-admin@example.com", [RoleNames.Admin]);
        user.DeleteAccount(DateTime.UtcNow);
        var userRepository = new InMemoryUserRepository(
            user,
            availableRoles: [RoleNames.Admin, RoleNames.Premium, RoleNames.Support]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: true,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("restore flow", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithActiveToggleTrue_ActivatesUser() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        user.Deactivate();
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: true,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.IsActive);
    }


    [Fact]
    public async Task UpdateAdminUserHandler_WithActiveToggleFalse_DeactivatesUser() {
        User user = CreateUserWithRoles("admin@example.com", [RoleNames.Admin]);
        var userRepository = new InMemoryUserRepository(user, availableRoles: [RoleNames.Admin]);
        UpdateAdminUserCommandHandler handler = CreateUpdateAdminUserHandler(userRepository);

        Result<AdminUserModel> result = await handler.Handle(
            new UpdateAdminUserCommand(
                user.Id.Value,
                IsActive: false,
                IsEmailConfirmed: null,
                Roles: null,
                Language: null,
                AiInputTokenLimit: null,
                AiOutputTokenLimit: null,
                ActorUserId: Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(user.IsActive);
    }

}
