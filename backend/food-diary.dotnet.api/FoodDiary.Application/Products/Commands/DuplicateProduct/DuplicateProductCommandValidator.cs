using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public class DuplicateProductCommandValidator : AbstractValidator<DuplicateProductCommand>
{
    public DuplicateProductCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.ProductId)
            .Must(id => id != ProductId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductId is required");
    }
}
