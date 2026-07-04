using FluentValidation.TestHelper;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class RegisterCommandValidatorTests {
    [Fact]
    public async Task Register_WithEmptyEmail_HasError() {
        var validator = new RegisterCommandValidator(CreateUserRepository());
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("", "password1", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_HasError() {
        var validator = new RegisterCommandValidator(CreateUserRepository());
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("not-email", "password1", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Register_WithEmptyPassword_HasError() {
        var validator = new RegisterCommandValidator(CreateUserRepository());
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Register_WithShortPassword_HasError() {
        var validator = new RegisterCommandValidator(CreateUserRepository());
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "12345", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Register_WithValidData_NoErrors() {
        var validator = new RegisterCommandValidator(CreateUserRepository());
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "password1", "en"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_HasConflictError() {
        var existingUser = User.Create("taken@test.com", "hashed");
        IAuthenticationUserRegistrationService userRegistrationService = CreateUserRegistrationService(existingUser);

        var validator = new RegisterCommandValidator(userRegistrationService);
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("taken@test.com", "password1", Language: null));

        result.ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorCode("Validation.Conflict");
    }

    [Fact]
    public async Task Register_WhenEmailBelongsToDeletedAccount_HasDeletedError() {
        var deletedUser = User.Create("deleted@test.com", "hashed");
        deletedUser.MarkDeleted(DateTime.UtcNow);
        IAuthenticationUserRegistrationService userRegistrationService = CreateUserRegistrationService(deletedUser);

        var validator = new RegisterCommandValidator(userRegistrationService);
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("deleted@test.com", "password1", Language: null));

        result.ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorCode("Authentication.AccountDeleted");
    }

    private static IAuthenticationUserRegistrationService CreateUserRepository(User? includingDeletedUser = null) =>
        CreateUserRegistrationService(includingDeletedUser);

    private static IAuthenticationUserRegistrationService CreateUserRegistrationService(User? includingDeletedUser = null) {
        IAuthenticationUserRegistrationService userRegistrationService = Substitute.For<IAuthenticationUserRegistrationService>();
        userRegistrationService.GetByEmailIncludingDeletedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string email = call.ArgAt<string>(0);
                return Task.FromResult(
                    includingDeletedUser is not null &&
                    string.Equals(includingDeletedUser.Email, email, StringComparison.OrdinalIgnoreCase)
                        ? includingDeletedUser
                        : null);
            });
        return userRegistrationService;
    }
}
