using FluentValidation;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Common;

internal sealed class ShoppingListItemInputValidator : AbstractValidator<ShoppingListItemInput> {
    public ShoppingListItemInputValidator() {
        RuleFor(item => item.Name)
            .NotEmpty()
            .When(item => !item.ProductId.HasValue)
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required for a custom shopping-list item.");

        RuleFor(item => item.Name)
            .MaximumLength(ShoppingListInputLimits.ItemNameMaxLength)
            .When(item => !item.ProductId.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Name must be at most {ShoppingListInputLimits.ItemNameMaxLength} characters.");

        RuleFor(item => item.Category)
            .MaximumLength(ShoppingListInputLimits.CategoryMaxLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Category must be at most {ShoppingListInputLimits.CategoryMaxLength} characters.");

        RuleFor(item => item.Aisle)
            .MaximumLength(ShoppingListInputLimits.CategoryMaxLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Aisle must be at most {ShoppingListInputLimits.CategoryMaxLength} characters.");

        RuleFor(item => item.Note)
            .MaximumLength(ShoppingListInputLimits.NoteMaxLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Note must be at most {ShoppingListInputLimits.NoteMaxLength} characters.");

        RuleFor(item => item.Amount)
            .Must(amount => !amount.HasValue ||
                (!double.IsNaN(amount.Value) &&
                 !double.IsInfinity(amount.Value) &&
                 amount.Value is > 0 and <= ShoppingListInputLimits.AmountMaxValue))
            .WithErrorCode("Validation.Invalid")
            .WithMessage(ShoppingListInputLimits.AmountRangeErrorMessage);

        RuleFor(item => item.CheckedOnUtc)
            .Must(timestamp => !timestamp.HasValue || timestamp.Value.Kind != DateTimeKind.Unspecified)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CheckedOnUtc must include timezone information.");
    }
}
