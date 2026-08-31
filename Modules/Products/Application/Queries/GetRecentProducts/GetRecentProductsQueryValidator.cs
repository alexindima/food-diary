using FluentValidation;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed class GetRecentProductsQueryValidator : AbstractValidator<GetRecentProductsQuery> {
    public GetRecentProductsQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithErrorCode("Validation.Invalid");
    }
}
