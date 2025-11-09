using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand> {
    private const string ProductContextKey = "__product";
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandValidator(IProductRepository productRepository) {
        _productRepository = productRepository;
        _productRepository = productRepository;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Не удалось определить пользователя")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Не удалось определить пользователя");

        RuleFor(x => x.ProductId)
            .Must(id => id != ProductId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductId is required");

        RuleFor(x => x.BaseAmount)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("BaseAmount must be greater than 0")
            .When(x => x.BaseAmount.HasValue);

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

        RuleFor(x => x.BaseUnit)
            .Must(BeValidUnit)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Неверная единица измерения")
            .When(x => !string.IsNullOrWhiteSpace(x.BaseUnit));

        RuleFor(x => x.Visibility)
            .Must(BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Неверный уровень видимости")
            .When(x => !string.IsNullOrWhiteSpace(x.Visibility));

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
        if (!context.RootContextData.TryGetValue(ProductContextKey, out var cached) ||
            cached is not Domain.Entities.Product product) {
            if (command.UserId is null || command.UserId.Value == UserId.Empty) {
                return;
            }

            product = await _productRepository.GetByIdAsync(command.ProductId, command.UserId.Value, includePublic: false, cancellationToken: cancellationToken);
            context.RootContextData[ProductContextKey] = product!;
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
                "Продукт используется в рецептах или потреблениях и не может быть изменён") {
                ErrorCode = "Validation.Invalid"
            });
        }
    }
}

