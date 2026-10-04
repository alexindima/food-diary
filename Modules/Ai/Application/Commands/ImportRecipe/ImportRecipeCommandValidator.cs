using FluentValidation;

namespace FoodDiary.Modules.Ai.Application.Commands.ImportRecipe;

public sealed class ImportRecipeCommandValidator : AbstractValidator<ImportRecipeCommand> {
    public ImportRecipeCommandValidator() {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.SourceUrl) || !string.IsNullOrWhiteSpace(x.Text))
            .WithErrorCode("Validation.Required").WithMessage("A recipe URL or recipe text is required.");
        RuleFor(x => x.SourceUrl).MaximumLength(1400).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Text).MaximumLength(16000).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.RequestId).NotEmpty().Length(64).Matches("^[0-9A-F]{64}$");
    }
}
