using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand> {
    private const string ProductContextKey = "__product";
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandValidator(IProductRepository productRepository) {
        _productRepository = productRepository;

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

        RuleFor(x => x.BaseAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("BaseAmount must be greater than 0")
            .When(x => x.BaseAmount.HasValue);

        RuleFor(x => x.DefaultPortionAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DefaultPortionAmount must be greater than 0")
            .When(x => x.DefaultPortionAmount.HasValue);

        RuleFor(x => x.CaloriesPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CaloriesPerBase must be non-negative")
            .When(x => x.CaloriesPerBase.HasValue);

        RuleFor(x => x.ProteinsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ProteinsPerBase must be non-negative")
            .When(x => x.ProteinsPerBase.HasValue);

        RuleFor(x => x.FatsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FatsPerBase must be non-negative")
            .When(x => x.FatsPerBase.HasValue);

        RuleFor(x => x.CarbsPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CarbsPerBase must be non-negative")
            .When(x => x.CarbsPerBase.HasValue);

        RuleFor(x => x.FiberPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FiberPerBase must be non-negative")
            .When(x => x.FiberPerBase.HasValue);

        RuleFor(x => x.AlcoholPerBase)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AlcoholPerBase must be non-negative")
            .When(x => x.AlcoholPerBase.HasValue);

        RuleFor(x => x.BaseUnit)
            .Must(BeValidUnit)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid measurement unit")
            .When(x => !string.IsNullOrWhiteSpace(x.BaseUnit));

        RuleFor(x => x.Visibility)
            .Must(BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level")
            .When(x => !string.IsNullOrWhiteSpace(x.Visibility));

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

        RuleFor(x => x)
            .CustomAsync(EnsureProductEditableAsync);
    }

    private bool BeValidUnit(string? unit) =>
        unit != null && Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit _);

    private bool BeValidVisibility(string? visibility) =>
        visibility != null && Enum.TryParse(visibility, ignoreCase: true, out Visibility _);

    private async Task EnsureProductEditableAsync(
        UpdateProductCommand command,
        ValidationContext<UpdateProductCommand> context,
        CancellationToken cancellationToken) {
        context.RootContextData.TryGetValue(ProductContextKey, out var cached);
        var product = cached as Product;

        if (product is null) {
            if (command.UserId is null || command.UserId.Value == Guid.Empty) {
                return;
            }

            product = await _productRepository.GetByIdAsync(new ProductId(command.ProductId), new UserId(command.UserId.Value), includePublic: false, cancellationToken: cancellationToken);
            if (product is not null) {
                context.RootContextData[ProductContextKey] = product;
            }
        }

        if (product is null) {
            context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product not found or you do not have permission to modify it") {
                ErrorCode = "Product.NotFound"
            });
            return;
        }

        var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        if (usageCount > 0) {
            context.AddFailure(new ValidationFailure(nameof(command.ProductId),
                "Product is already used in consumptions or recipes and cannot be updated") {
                ErrorCode = "Validation.Invalid"
            });
        }
    }
}
