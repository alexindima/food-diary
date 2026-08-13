using FluentValidation;
using FoodDiary.Application.Products.Common;
using System.Linq.Expressions;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand> {
    public UpdateProductCommandValidator() {
        ConfigureIdentityRules();
        ConfigureNutritionRules();
        ConfigureEnumRules();
        ConfigureClearRules();
    }

    private void ConfigureIdentityRules() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductId is required");
    }

    private void ConfigureNutritionRules() {
        ConfigureMeasurementRules();
        ConfigureCaloriesRule();
        ConfigureNutrientRule(
            x => x.ProteinsPerBase,
            x => x.ProteinsPerBase.HasValue,
            "ProteinsPerBase must be non-negative",
            "ProteinsPerBase exceeds the maximum for the selected unit");
        ConfigureNutrientRule(
            x => x.FatsPerBase,
            x => x.FatsPerBase.HasValue,
            "FatsPerBase must be non-negative",
            "FatsPerBase exceeds the maximum for the selected unit");
        ConfigureNutrientRule(
            x => x.CarbsPerBase,
            x => x.CarbsPerBase.HasValue,
            "CarbsPerBase must be non-negative",
            "CarbsPerBase exceeds the maximum for the selected unit");
        ConfigureNutrientRule(
            x => x.FiberPerBase,
            x => x.FiberPerBase.HasValue,
            "FiberPerBase must be non-negative",
            "FiberPerBase exceeds the maximum for the selected unit");
        ConfigureNutrientRule(
            x => x.AlcoholPerBase,
            x => x.AlcoholPerBase.HasValue,
            "AlcoholPerBase must be non-negative",
            "AlcoholPerBase exceeds the maximum for the selected unit");
    }

    private void ConfigureMeasurementRules() {
        RuleFor(x => x.BaseAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("BaseAmount must be greater than 0")
            .When(x => x.BaseAmount.HasValue);

        RuleFor(x => x.DefaultPortionAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DefaultPortionAmount must be greater than 0")
            .Must((command, amount) => ProductCommandValidation.BeWithinDefaultPortionLimit(command.BaseUnit, amount!.Value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DefaultPortionAmount exceeds the maximum for the selected unit")
            .When(x => x.DefaultPortionAmount.HasValue);
    }

    private void ConfigureCaloriesRule() {
        RuleFor(x => x.CaloriesPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CaloriesPerBase must be non-negative")
            .Must((command, value) => ProductCommandValidation.BeWithinCaloriesLimit(command.BaseUnit, value!.Value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CaloriesPerBase exceeds the maximum for the selected unit")
            .When(x => x.CaloriesPerBase.HasValue);
    }

    private void ConfigureNutrientRule(
        Expression<Func<UpdateProductCommand, double?>> property,
        Func<UpdateProductCommand, bool> condition,
        string nonNegativeMessage,
        string limitMessage) {
        RuleFor(property)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage(nonNegativeMessage)
            .Must((command, value) => ProductCommandValidation.BeWithinNutrientLimit(command.BaseUnit, value!.Value))
            .WithErrorCode("Validation.Invalid")
            .WithMessage(limitMessage)
            .When(condition);
    }

    private void ConfigureEnumRules() {
        RuleFor(x => x.BaseUnit)
            .Must(ProductCommandValidation.BeValidUnit)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid measurement unit")
            .When(x => !string.IsNullOrWhiteSpace(x.BaseUnit));

        RuleFor(x => x.Visibility)
            .Must(ProductCommandValidation.BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level")
            .When(x => !string.IsNullOrWhiteSpace(x.Visibility));

        RuleFor(x => x.ProductType)
            .Must(ProductCommandValidation.BeValidProductType)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid product type")
            .When(x => !string.IsNullOrWhiteSpace(x.ProductType));
    }

    private void ConfigureClearRules() {
        RuleFor(x => x)
            .Must(x => !(x.ClearBarcode && !string.IsNullOrWhiteSpace(x.Barcode)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Barcode cannot be provided when ClearBarcode is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearBrand && !string.IsNullOrWhiteSpace(x.Brand)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Brand cannot be provided when ClearBrand is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearCategory && !string.IsNullOrWhiteSpace(x.Category)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Category cannot be provided when ClearCategory is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearDescription && !string.IsNullOrWhiteSpace(x.Description)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Description cannot be provided when ClearDescription is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearComment && !string.IsNullOrWhiteSpace(x.Comment)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Comment cannot be provided when ClearComment is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearImageUrl && !string.IsNullOrWhiteSpace(x.ImageUrl)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ImageUrl cannot be provided when ClearImageUrl is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearImageAssetId && x.ImageAssetId.HasValue))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ImageAssetId cannot be provided when ClearImageAssetId is true");
    }
}
