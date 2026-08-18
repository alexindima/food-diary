using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Recipes.Recipes.Queries.GetRecipes;

public sealed class GetRecipesQueryValidator : AbstractValidator<GetRecipesQuery> {
    public GetRecipesQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.Page)
            .InclusiveBetween(PaginationPolicy.DefaultPage, PaginationPolicy.MaxPageNumber)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Page must be between {PaginationPolicy.DefaultPage} and {PaginationPolicy.MaxPageNumber}");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, PaginationPolicy.MaxPageSize)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Limit must be between 1 and {PaginationPolicy.MaxPageSize}");
    }
}
