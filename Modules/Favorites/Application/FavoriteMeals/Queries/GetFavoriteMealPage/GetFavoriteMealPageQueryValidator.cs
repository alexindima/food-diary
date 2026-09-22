using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMealPage;

public sealed class GetFavoriteMealPageQueryValidator : AbstractValidator<GetFavoriteMealPageQuery> {
    public GetFavoriteMealPageQueryValidator() {
        RuleFor(x => x.UserId).NotNull().Must(id => id != Guid.Empty);
        RuleFor(x => x.Page).InclusiveBetween(1, PaginationPolicy.MaxPageNumber);
        RuleFor(x => x.Limit).InclusiveBetween(1, PaginationPolicy.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
