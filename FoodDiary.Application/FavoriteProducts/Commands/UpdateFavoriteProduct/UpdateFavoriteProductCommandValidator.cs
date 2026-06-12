using FluentValidation;

namespace FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;

public sealed class UpdateFavoriteProductCommandValidator : AbstractValidator<UpdateFavoriteProductCommand> {
    public UpdateFavoriteProductCommandValidator() {
        RuleFor(x => x.FavoriteProductId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Favorite product id must not be empty.");

        RuleFor(x => x.PreferredPortionAmount)
            .GreaterThan(0)
            .Must(value => !double.IsNaN(value) && !double.IsInfinity(value))
            .WithErrorCode("Validation.Range")
            .WithMessage("Preferred portion amount must be a positive finite number.");
    }
}
