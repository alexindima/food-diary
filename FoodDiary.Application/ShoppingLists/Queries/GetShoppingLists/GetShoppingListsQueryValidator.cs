using FluentValidation;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public class GetShoppingListsQueryValidator : AbstractValidator<GetShoppingListsQuery> {
    public GetShoppingListsQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");
    }
}
