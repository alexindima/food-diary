using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipePage;

public sealed class GetFavoriteRecipePageQueryValidator : AbstractValidator<GetFavoriteRecipePageQuery> {
    public GetFavoriteRecipePageQueryValidator() {
        RuleFor(x => x.UserId).NotNull().Must(id => id != Guid.Empty);
        RuleFor(x => x.Page).InclusiveBetween(1, PaginationPolicy.MaxPageNumber);
        RuleFor(x => x.Limit).InclusiveBetween(1, PaginationPolicy.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
