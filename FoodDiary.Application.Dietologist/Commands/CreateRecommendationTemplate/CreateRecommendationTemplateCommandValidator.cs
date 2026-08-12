using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendationTemplate;

public sealed class CreateRecommendationTemplateCommandValidator : AbstractValidator<CreateRecommendationTemplateCommand> {
    public CreateRecommendationTemplateCommandValidator() {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Text).NotEmpty().MaximumLength(2000);
    }
}
