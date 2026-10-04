using FluentValidation;

namespace FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;

public sealed class ImportRecipeVideoCommandValidator : AbstractValidator<ImportRecipeVideoCommand> {
    public ImportRecipeVideoCommandValidator() {
        RuleFor(x => x).Must(x => x.Video is not null || !string.IsNullOrWhiteSpace(x.SourceUrl))
            .WithErrorCode("Validation.Required").WithMessage("A public video URL or uploaded video is required.");
        RuleFor(x => x.SourceUrl).MaximumLength(1400).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Text).MaximumLength(8000).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.RequestId).NotEmpty().Length(64).Matches("^[0-9A-F]{64}$");
    }
}
