using FoodDiary.Application.Contracts.Common.Validation;
using FluentValidation;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;

public sealed class SaveCatalogMealPlanCommandValidator : AbstractValidator<SaveCatalogMealPlanCommand> {
    public SaveCatalogMealPlanCommandValidator() {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithErrorCode("Validation.Invalid").When(x => x.Id.HasValue);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("Validation.Required").MaximumLength(256).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Description).MaximumLength(2048).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.DietType).Must(value => SharedEnumValueParser.CanParseDefined<DietType>(value))
            .WithMessage("Select a valid diet type.").WithErrorCode("Validation.Invalid");
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 31).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.TargetCaloriesPerDay).Must(value => value is null || (double.IsFinite(value.Value) && value > 0))
            .WithMessage("Target calories must be a positive finite number.").WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Days).NotNull().WithErrorCode("Validation.Required").Must(days => days?.Count <= 31)
            .WithMessage("A plan can contain at most 31 days.").WithErrorCode("Validation.Invalid");
        RuleFor(x => x).Must(x => x.Days is not null && x.Days.Select(d => d.DayNumber).Distinct().Count() == x.Days.Count)
            .WithMessage("Each day number must be unique.").WithErrorCode("Validation.Invalid");
        RuleForEach(x => x.Days).ChildRules(day => {
            day.RuleFor(x => x.DayNumber).InclusiveBetween(1, 31).WithErrorCode("Validation.Invalid");
            day.RuleFor(x => x.Meals).NotNull().WithErrorCode("Validation.Required").Must(meals => meals?.Count <= 20)
                .WithMessage("A day can contain at most 20 recipes.").WithErrorCode("Validation.Invalid");
            day.RuleForEach(x => x.Meals).ChildRules(meal => {
                meal.RuleFor(x => x.RecipeId).NotEmpty().WithErrorCode("Validation.Required");
                meal.RuleFor(x => x.Servings).InclusiveBetween(1, 100).WithErrorCode("Validation.Invalid");
                meal.RuleFor(x => x.MealType).Must(value => SharedEnumValueParser.CanParseDefined<MealType>(value))
                    .WithMessage("Select a valid meal type.").WithErrorCode("Validation.Invalid");
            });
        });
        RuleFor(x => x).Must(x => x.Days?.All(d => d.DayNumber <= x.DurationDays) == true)
            .WithMessage("Day numbers must fit within the plan duration.").WithErrorCode("Validation.Invalid");
        RuleFor(x => x).Must(x => !x.IsPublished || (x.Days is not null && x.Days.Count == x.DurationDays && x.Days.All(d => d.Meals is { Count: > 0 })))
            .WithMessage("Published plans need every day filled with at least one recipe.").WithErrorCode("Validation.Invalid");
    }
}
