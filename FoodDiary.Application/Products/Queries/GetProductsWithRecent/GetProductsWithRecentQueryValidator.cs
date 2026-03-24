using FluentValidation;

namespace FoodDiary.Application.Products.Queries.GetProductsWithRecent;

public sealed class GetProductsWithRecentQueryValidator : AbstractValidator<GetProductsWithRecentQuery> {
    public GetProductsWithRecentQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
