using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public class UpdateShoppingListCommandValidator : AbstractValidator<UpdateShoppingListCommand>
{
    public UpdateShoppingListCommandValidator()
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

        RuleFor(x => x)
            .Must(command => !string.IsNullOrWhiteSpace(command.Name) || command.Items is not null)
            .WithErrorCode("Validation.Required")
            .WithMessage("Name or Items is required");
    }
}
