using FoodDiary.Modules.Ai.Application.Common.Validation;
using FluentValidation;

namespace FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;

public sealed class StartFoodRecognitionCommandValidator : AbstractValidator<StartFoodRecognitionCommand> {
    public StartFoodRecognitionCommandValidator() {
        RuleFor(x => x).Must(x => RecognitionImagesValidation.IsValid(x.ImageAssetId, x.IsProductLabel, x.AdditionalImageAssetIds))
            .WithMessage("Provide up to five distinct images of one product; additional images require product label recognition.");
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ImageAssetId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2048);
    }
}
