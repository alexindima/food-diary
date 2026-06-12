using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Consumptions.Common.Validators;

public sealed class ConsumptionAiItemInputValidator : AbstractValidator<ConsumptionAiItemInput> {
    private const int NameMaxLength = 256;
    private const int UnitMaxLength = 32;

    public ConsumptionAiItemInputValidator() {
        RuleFor(x => x.NameEn)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("NameEn is required.")
            .MaximumLength(NameMaxLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"NameEn must be at most {NameMaxLength} characters.");

        RuleFor(x => x.NameLocal)
            .MaximumLength(NameMaxLength)
            .When(x => x.NameLocal is not null)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"NameLocal must be at most {NameMaxLength} characters.");

        RuleFor(x => x.Amount)
            .Must(BePositiveFinite)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Unit is required.")
            .MaximumLength(UnitMaxLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Unit must be at most {UnitMaxLength} characters.");

        RuleFor(x => x.Calories).Must(BeNonNegativeFinite).WithErrorCode("Validation.Invalid").WithMessage("Calories must be non-negative.");
        RuleFor(x => x.Proteins).Must(BeNonNegativeFinite).WithErrorCode("Validation.Invalid").WithMessage("Proteins must be non-negative.");
        RuleFor(x => x.Fats).Must(BeNonNegativeFinite).WithErrorCode("Validation.Invalid").WithMessage("Fats must be non-negative.");
        RuleFor(x => x.Carbs).Must(BeNonNegativeFinite).WithErrorCode("Validation.Invalid").WithMessage("Carbs must be non-negative.");
        RuleFor(x => x.Fiber).Must(BeNonNegativeFinite).WithErrorCode("Validation.Invalid").WithMessage("Fiber must be non-negative.");
        RuleFor(x => x.Alcohol).Must(BeNonNegativeFinite).WithErrorCode("Validation.Invalid").WithMessage("Alcohol must be non-negative.");
        RuleFor(x => x.Confidence)
            .Must(value => value is null || BeConfidence(value.Value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Confidence must be in range [0, 1].");
        RuleFor(x => x.Resolution)
            .Must(resolution => string.IsNullOrWhiteSpace(resolution) || Enum.TryParse<MealAiItemResolution>(resolution, ignoreCase: true, out _))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unknown AI item resolution value.");
    }

    private static bool BePositiveFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;

    private static bool BeNonNegativeFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;

    private static bool BeConfidence(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value is >= 0 and <= 1;
}
