using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Products.Queries.GetProductsOverview;

public sealed class GetProductsOverviewQueryValidator : AbstractValidator<GetProductsOverviewQuery> {
    public GetProductsOverviewQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Page)
            .InclusiveBetween(PaginationPolicy.DefaultPage, PaginationPolicy.MaxPageNumber)
            .WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, PaginationPolicy.MaxPageSize)
            .WithErrorCode("Validation.Invalid");
        RuleFor(x => x.RecentLimit)
            .InclusiveBetween(1, 50)
            .WithErrorCode("Validation.Invalid");
        RuleFor(x => x.FavoriteLimit)
            .InclusiveBetween(1, 50)
            .WithErrorCode("Validation.Invalid");
    }
}
