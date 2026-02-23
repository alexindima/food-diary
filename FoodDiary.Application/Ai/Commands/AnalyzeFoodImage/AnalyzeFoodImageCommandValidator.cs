using FluentValidation;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandValidator : AbstractValidator<AnalyzeFoodImageCommand> {
    public AnalyzeFoodImageCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleFor(x => x.ImageAssetId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleFor(x => x.Description)
            .MaximumLength(2048)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
