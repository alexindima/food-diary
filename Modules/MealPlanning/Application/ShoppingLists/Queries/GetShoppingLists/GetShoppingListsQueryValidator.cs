using FluentValidation;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryValidator : AbstractValidator<GetShoppingListsQuery> {
    public GetShoppingListsQueryValidator() {
        RuleFor(x => x.Page).InclusiveBetween(1, 10000).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50).WithErrorCode("Validation.Invalid").When(x => x.PageSize.HasValue);
        RuleFor(x => x.Search).MaximumLength(128).WithErrorCode("Validation.Invalid");
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");
    }
}
