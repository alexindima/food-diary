using FluentValidation;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandValidator : AbstractValidator<CreateShoppingListCommand> {
    public CreateShoppingListCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required")
            .MaximumLength(ShoppingListInputLimits.NameMaxLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Name must be at most {ShoppingListInputLimits.NameMaxLength} characters.");

        RuleFor(x => x.Items)
            .Must(items => items.Count <= ShoppingListInputLimits.ItemsMaxCount)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"A shopping list must contain at most {ShoppingListInputLimits.ItemsMaxCount} items.");

        RuleForEach(x => x.Items)
            .SetValidator(new ShoppingListItemInputValidator());
    }
}
