using FluentValidation;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public class DeleteShoppingListCommandValidator : AbstractValidator<DeleteShoppingListCommand> {
    public DeleteShoppingListCommandValidator() {
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
