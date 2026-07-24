using FluentValidation.TestHelper;
using FoodDiary.Application.Admin.Commands.CreateAdminUser;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class CreateAdminUserCommandValidatorTests {
    private readonly CreateAdminUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidGeneratedPasswordCommand_HasNoErrors() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithValidExplicitPasswordAndNoEmail_HasNoErrors() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with {
                Language = null,
                TemporaryPassword = "password",
                GeneratePassword = false,
                SendCredentialsEmail = false,
                RequirePasswordChange = false,
            });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Email")]
    [InlineData("not-an-email", "Email")]
    public async Task Validate_WithInvalidEmail_HasError(string email, string propertyName) {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { Email = email });

        result.ShouldHaveValidationErrorFor(propertyName);
    }

    [Fact]
    public async Task Validate_WithEmptyActorUserId_HasError() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { ActorUserId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(command => command.ActorUserId);
    }

    [Fact]
    public async Task Validate_WithInvalidLanguage_HasError() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { Language = "invalid" });

        result.ShouldHaveValidationErrorFor(command => command.Language);
    }

    [Fact]
    public async Task Validate_WithNullRoles_HasError() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { Roles = null! });

        result.ShouldHaveValidationErrorFor(command => command.Roles);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Unknown")]
    public async Task Validate_WithUnknownRole_HasError(string role) {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { Roles = [role] });

        result.ShouldHaveValidationErrorFor("Roles[0]");
    }

    [Fact]
    public async Task Validate_WithTrimmedAllowedRole_HasNoRoleError() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { Roles = [$" {RoleNames.Admin} "] });

        result.ShouldNotHaveValidationErrorFor("Roles[0]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public async Task Validate_WithoutGeneratedPasswordAndInvalidTemporaryPassword_HasError(string? password) {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with {
                TemporaryPassword = password,
                GeneratePassword = false,
            });

        result.ShouldHaveValidationErrorFor(command => command.TemporaryPassword);
    }

    [Fact]
    public async Task Validate_WhenCredentialsEmailDoesNotRequirePasswordChange_HasError() {
        TestValidationResult<CreateAdminUserCommand> result =
            await _validator.TestValidateAsync(CreateValidCommand() with { RequirePasswordChange = false });

        result.ShouldHaveValidationErrorFor(command => command.RequirePasswordChange);
    }

    private static CreateAdminUserCommand CreateValidCommand() =>
        new(
            "admin@example.com",
            "Admin",
            "User",
            "en",
            [RoleNames.Admin, RoleNames.Premium, RoleNames.Support, RoleNames.Dietologist],
            TemporaryPassword: null,
            GeneratePassword: true,
            IsEmailConfirmed: true,
            SendCredentialsEmail: true,
            RequirePasswordChange: true,
            ClientOrigin: null,
            ActorUserId: Guid.NewGuid());
}
