using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery> {
    public GetProductsQueryValidator() {
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
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"page must be between {PaginationPolicy.DefaultPage} and {PaginationPolicy.MaxPageNumber}");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, PaginationPolicy.MaxPageSize)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"limit must be between 1 and {PaginationPolicy.MaxPageSize}");
    }
}
