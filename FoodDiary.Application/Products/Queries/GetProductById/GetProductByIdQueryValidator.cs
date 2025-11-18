using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery> {
    public GetProductByIdQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.ProductId)
            .Must(id => id != ProductId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductId is required");
    }
}
