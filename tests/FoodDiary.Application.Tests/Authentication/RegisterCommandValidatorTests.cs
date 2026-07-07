using FluentValidation.TestHelper;
using FoodDiary.Application.Authentication.Commands.Register;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class RegisterCommandValidatorTests {
    [Fact]
    public async Task Register_WithEmptyEmail_HasError() {
        var validator = new RegisterCommandValidator();
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("", "password1", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_HasError() {
        var validator = new RegisterCommandValidator();
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("not-email", "password1", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Register_WithEmptyPassword_HasError() {
        var validator = new RegisterCommandValidator();
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Register_WithShortPassword_HasError() {
        var validator = new RegisterCommandValidator();
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "12345", Language: null));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Register_WithValidData_NoErrors() {
        var validator = new RegisterCommandValidator();
        TestValidationResult<RegisterCommand> result = await validator.TestValidateAsync(new RegisterCommand("user@test.com", "password1", "en"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
