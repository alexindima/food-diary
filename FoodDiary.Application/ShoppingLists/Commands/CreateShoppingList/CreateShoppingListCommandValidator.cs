using FluentValidation;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public class CreateShoppingListCommandValidator : AbstractValidator<CreateShoppingListCommand> {
    public CreateShoppingListCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required");
    }
}
