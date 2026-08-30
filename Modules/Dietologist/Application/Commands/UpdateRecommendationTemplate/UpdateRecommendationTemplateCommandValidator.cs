using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.UpdateRecommendationTemplate;

public sealed class UpdateRecommendationTemplateCommandValidator : AbstractValidator<UpdateRecommendationTemplateCommand> {
    public UpdateRecommendationTemplateCommandValidator() {
        RuleFor(command => command.TemplateId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Text).NotEmpty().MaximumLength(2000);
    }
}
