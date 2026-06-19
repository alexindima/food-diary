using FluentValidation;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand> {
    public CreateProductCommandValidator() {
        ConfigureIdentityRules();
        ConfigureEnumRules();
        ConfigureMeasurementRules();
        ConfigureNutritionRules();
    }

    private void ConfigureIdentityRules() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required");
    }

    private void ConfigureEnumRules() {
        RuleFor(x => x.BaseUnit)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("BaseUnit is required")
            .Must(BeValidUnit)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid measurement unit");

        RuleFor(x => x.Visibility)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Visibility is required")
            .Must(BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level");

        RuleFor(x => x.ProductType)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductType is required")
            .Must(BeValidProductType)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid product type");
    }

    private void ConfigureMeasurementRules() {
        RuleFor(x => x.BaseAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("BaseAmount must be greater than 0");

        RuleFor(x => x.DefaultPortionAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DefaultPortionAmount must be greater than 0")
            .Must((command, amount) => BeWithinDefaultPortionLimit(command.BaseUnit, amount))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DefaultPortionAmount exceeds the maximum for the selected unit");
    }

    private void ConfigureNutritionRules() {
        RuleFor(x => x.CaloriesPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CaloriesPerBase must be non-negative")
            .Must((command, value) => BeWithinCaloriesLimit(command.BaseUnit, value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CaloriesPerBase exceeds the maximum for the selected unit");

        RuleFor(x => x.ProteinsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ProteinsPerBase must be non-negative")
            .Must((command, value) => BeWithinNutrientLimit(command.BaseUnit, value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ProteinsPerBase exceeds the maximum for the selected unit");

        RuleFor(x => x.FatsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FatsPerBase must be non-negative")
            .Must((command, value) => BeWithinNutrientLimit(command.BaseUnit, value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FatsPerBase exceeds the maximum for the selected unit");

        RuleFor(x => x.CarbsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CarbsPerBase must be non-negative")
            .Must((command, value) => BeWithinNutrientLimit(command.BaseUnit, value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CarbsPerBase exceeds the maximum for the selected unit");

        RuleFor(x => x.FiberPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FiberPerBase must be non-negative")
            .Must((command, value) => BeWithinNutrientLimit(command.BaseUnit, value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FiberPerBase exceeds the maximum for the selected unit");

        RuleFor(x => x.AlcoholPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AlcoholPerBase must be non-negative")
            .Must((command, value) => BeWithinNutrientLimit(command.BaseUnit, value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AlcoholPerBase exceeds the maximum for the selected unit");
    }

    private static bool BeValidUnit(string unit) {
        return Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit _);
    }

    private static bool BeWithinDefaultPortionLimit(string unit, double amount) {
        return !Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit parsedUnit) ||
               amount <= Product.GetMaxDefaultPortionAmount(parsedUnit);
    }

    private static bool BeWithinCaloriesLimit(string unit, double amount) {
        return !Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit parsedUnit) ||
               amount <= Product.GetMaxCaloriesPerBase(parsedUnit);
    }

    private static bool BeWithinNutrientLimit(string unit, double amount) {
        return !Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit parsedUnit) ||
               amount <= Product.GetMaxNutrientPerBase(parsedUnit);
    }

    private static bool BeValidVisibility(string visibility) {
        return Enum.TryParse(visibility, ignoreCase: true, out Visibility _);
    }

    private static bool BeValidProductType(string? productType) {
        return productType is not null &&
               Enum.TryParse(productType, ignoreCase: true, out ProductType parsed) &&
               Enum.IsDefined(parsed);
    }

}
