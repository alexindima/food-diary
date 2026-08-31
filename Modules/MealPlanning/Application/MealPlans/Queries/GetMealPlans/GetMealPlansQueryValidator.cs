using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryValidator : AbstractValidator<GetMealPlansQuery> {
    private const int MaximumDietTypeLength = 32;
    private static readonly HashSet<string> ValidDietTypes = new(
        Enum.GetNames<DietType>(),
        StringComparer.OrdinalIgnoreCase);

    public GetMealPlansQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.DietType)
            .MaximumLength(MaximumDietTypeLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"DietType must be at most {MaximumDietTypeLength} characters.")
            .Must(IsValidDietType)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DietType is invalid.");
    }

    private static bool IsValidDietType(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        ValidDietTypes.Contains(value.Trim());
}
