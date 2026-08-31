using FluentValidation;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Commands.UpdateShoppingList;

public sealed class UpdateShoppingListCommandValidator : AbstractValidator<UpdateShoppingListCommand> {
    public UpdateShoppingListCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.ShoppingListId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ShoppingListId is required");

        RuleFor(x => x)
            .Must(command => !string.IsNullOrWhiteSpace(command.Name) || command.Items is not null)
            .WithErrorCode("Validation.Required")
            .WithMessage("Name or Items is required");

        RuleFor(x => x.Name)
            .MaximumLength(ShoppingListInputLimits.NameMaxLength)
            .When(x => x.Name is not null)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Name must be at most {ShoppingListInputLimits.NameMaxLength} characters.");

        RuleFor(x => x.Items)
            .Must(items => items is null || items.Count <= ShoppingListInputLimits.ItemsMaxCount)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"A shopping list must contain at most {ShoppingListInputLimits.ItemsMaxCount} items.");

        RuleForEach(x => x.Items)
            .SetValidator(new ShoppingListItemInputValidator());
    }
}
