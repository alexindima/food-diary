using FluentValidation;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public sealed class AddFavoriteProductCommandValidator : AbstractValidator<AddFavoriteProductCommand> {
    public AddFavoriteProductCommandValidator() {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Product id must not be empty.");

        RuleFor(x => x.PreferredPortionAmount)
            .GreaterThan(0)
            .Must(value => !double.IsNaN(value!.Value) && !double.IsInfinity(value.Value))
            .When(x => x.PreferredPortionAmount.HasValue)
            .WithErrorCode("Validation.Range")
            .WithMessage("Preferred portion amount must be a positive finite number.");
    }
}
