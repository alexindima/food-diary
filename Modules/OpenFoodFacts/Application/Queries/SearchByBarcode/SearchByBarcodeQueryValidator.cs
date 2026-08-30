using FluentValidation;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;

public sealed class SearchByBarcodeQueryValidator : AbstractValidator<SearchByBarcodeQuery> {
    public const int MaximumBarcodeLength = 128;

    public SearchByBarcodeQueryValidator() {
        RuleFor(x => x.Barcode)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Barcode is required.");

        RuleFor(x => x.Barcode)
            .MaximumLength(MaximumBarcodeLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Barcode must not exceed {MaximumBarcodeLength} characters.");
    }
}
