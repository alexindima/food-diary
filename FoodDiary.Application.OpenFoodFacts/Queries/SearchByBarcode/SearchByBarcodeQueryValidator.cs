using FluentValidation;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;

public sealed class SearchByBarcodeQueryValidator : AbstractValidator<SearchByBarcodeQuery> {
    public SearchByBarcodeQueryValidator() {
        RuleFor(x => x.Barcode)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Barcode is required.");

        RuleFor(x => x.Barcode)
            .MaximumLength(128)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Barcode must not exceed 128 characters.");
    }
}
