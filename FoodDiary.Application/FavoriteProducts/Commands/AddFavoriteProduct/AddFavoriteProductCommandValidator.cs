using FluentValidation;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public sealed class AddFavoriteProductCommandValidator : AbstractValidator<AddFavoriteProductCommand> {
    public AddFavoriteProductCommandValidator() {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Product id must not be empty.");
    }
}
