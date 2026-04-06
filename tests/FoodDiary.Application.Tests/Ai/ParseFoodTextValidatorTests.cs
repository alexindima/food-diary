using FluentValidation.TestHelper;
using FoodDiary.Application.Ai.Commands.ParseFoodText;

namespace FoodDiary.Application.Tests.Ai;

public class ParseFoodTextValidatorTests {
    private readonly ParseFoodTextCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyText_HasError() {
        var command = new ParseFoodTextCommand(Guid.NewGuid(), "");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public async Task Validate_WithTooLongText_HasError() {
        var command = new ParseFoodTextCommand(Guid.NewGuid(), new string('a', 2049));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public async Task Validate_WithValidCommand_NoErrors() {
        var command = new ParseFoodTextCommand(Guid.NewGuid(), "chicken breast 200g");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
