using FluentValidation.TestHelper;
using FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;

namespace FoodDiary.Application.Tests.RecipeComments;

public class RecipeCommentsValidatorTests {
    private readonly CreateRecipeCommentCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyText_HasError() {
        var command = new CreateRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public async Task Validate_WithTooLongText_HasError() {
        var command = new CreateRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('c', 2001));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public async Task Validate_WithValidCommand_NoErrors() {
        var command = new CreateRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Great recipe!");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
