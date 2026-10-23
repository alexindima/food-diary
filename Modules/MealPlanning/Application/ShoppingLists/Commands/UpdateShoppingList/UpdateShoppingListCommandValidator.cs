using FluentValidation;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.UpdateShoppingList;

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

        RuleFor(x => x.ExpectedRevision)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("ExpectedRevision is required.")
            .Must(ShoppingListRevision.IsValid)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ExpectedRevision must be a 64-character lowercase hexadecimal revision.");

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
