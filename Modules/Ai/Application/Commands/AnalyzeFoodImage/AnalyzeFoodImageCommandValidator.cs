using FoodDiary.Modules.Ai.Application.Common.Validation;
using FluentValidation;

namespace FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;

public sealed class AnalyzeFoodImageCommandValidator : AbstractValidator<AnalyzeFoodImageCommand> {
    public AnalyzeFoodImageCommandValidator() {
        RuleFor(x => x).Must(x => RecognitionImagesValidation.IsValid(x.ImageAssetId, x.IsProductLabel, x.AdditionalImageAssetIds))
            .WithMessage("Provide up to five distinct images of one product; additional images require product label recognition.");
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleFor(x => x.ImageAssetId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleFor(x => x.Description)
            .MaximumLength(2048)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.RequestId)
            .NotEmpty()
            .Length(64)
            .Matches("^[0-9A-F]{64}$");
    }
}
