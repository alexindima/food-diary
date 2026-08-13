using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Enums;
using FluentValidation;
using FoodDiary.Application.Admin.Internal.Validation;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminAchievementDefinition;

public sealed class UpdateAdminAchievementDefinitionCommandValidator : AbstractValidator<UpdateAdminAchievementDefinitionCommand> {
    public UpdateAdminAchievementDefinitionCommandValidator() {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Input).NotNull().DependentRules(() => {
            RuleFor(command => command.Input.Category).NotEmpty().MaximumLength(AchievementDefinition.CategoryMaxLength)
                .Matches("^[a-zA-Z0-9_-]+$");
            RuleFor(command => command.Input.Metric).NotEmpty()
                .Must(EnumValueParser.CanParseDefined<AchievementMetric>)
                .WithMessage("Unsupported achievement metric.");
            RuleFor(command => command.Input.Threshold).GreaterThan(0);
            RuleFor(command => command.Input.TitleRu).NotEmpty().MaximumLength(AchievementDefinition.TitleMaxLength);
            RuleFor(command => command.Input.TitleEn).NotEmpty().MaximumLength(AchievementDefinition.TitleMaxLength);
            RuleFor(command => command.Input.DescriptionRu).NotEmpty().MaximumLength(AchievementDefinition.DescriptionMaxLength);
            RuleFor(command => command.Input.DescriptionEn).NotEmpty().MaximumLength(AchievementDefinition.DescriptionMaxLength);
            RuleFor(command => command.Input.Icon).NotEmpty().MaximumLength(AchievementDefinition.IconMaxLength)
                .Matches("^[a-zA-Z0-9_-]+$");
            RuleFor(command => command.Input.SortOrder).GreaterThanOrEqualTo(0);
            RuleFor(command => command.Input.Version).GreaterThan(0);
        });
    }
}
