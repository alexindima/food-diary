using FluentValidation;

namespace FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;

public sealed class CreateRecipeCommentCommandValidator : AbstractValidator<CreateRecipeCommentCommand> {
    public CreateRecipeCommentCommandValidator() {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Comment text is required.")
            .MaximumLength(2000)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Comment text must be at most 2000 characters.");
    }
}
