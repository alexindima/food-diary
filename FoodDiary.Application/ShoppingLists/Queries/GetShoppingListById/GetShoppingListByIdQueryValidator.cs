using FluentValidation;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public class GetShoppingListByIdQueryValidator : AbstractValidator<GetShoppingListByIdQuery>
{
    public GetShoppingListByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.ShoppingListId)
            .Must(id => id != ShoppingListId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ShoppingListId is required");
    }
}
