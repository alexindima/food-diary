using FluentValidation;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public class GetCurrentShoppingListQueryValidator : AbstractValidator<GetCurrentShoppingListQuery> {
    public GetCurrentShoppingListQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");
    }
}
