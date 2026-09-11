using FluentValidation;

namespace FoodDiary.Application.Ai.Commands.StartFoodRecognition;

public sealed class StartFoodRecognitionCommandValidator : AbstractValidator<StartFoodRecognitionCommand> {
    public StartFoodRecognitionCommandValidator() {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ImageAssetId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2048);
    }
}
