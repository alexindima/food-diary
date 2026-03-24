using FluentValidation;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public class GetShoppingListByIdQueryValidator : AbstractValidator<GetShoppingListByIdQuery> {
    public GetShoppingListByIdQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.ShoppingListId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ShoppingListId is required");
    }
}
