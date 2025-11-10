using System;
using FoodDiary.Application.Recipes.Commands.Common;
using FoodDiary.Application.Recipes.Commands.Common.Validators;
using FluentValidation;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required");

        RuleFor(x => x.Servings)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Servings must be greater than zero");

        RuleFor(x => x.PrepTime)
            .GreaterThan(0)
            .When(x => x.PrepTime.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("PrepTime must be greater than zero");

        RuleFor(x => x.CookTime)
            .GreaterThan(0)
            .When(x => x.CookTime.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CookTime must be greater than zero");

        RuleFor(x => x.Visibility)
            .NotEmpty()
            .Must(BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level");

        RuleFor(x => x.Steps)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Steps are required")
            .Must(steps => steps.Count > 0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Recipe must contain at least one step");

        RuleForEach(x => x.Steps)
            .SetValidator(new RecipeStepInputValidator());
    }

    private static bool BeValidVisibility(string visibility) =>
        Enum.TryParse(visibility, ignoreCase: true, out Visibility _);
}
