using FluentValidation;

namespace FoodDiary.Modules.Usda.Application.Queries.GetMicronutrients;

public sealed class GetMicronutrientsQueryValidator : AbstractValidator<GetMicronutrientsQuery> {
    public GetMicronutrientsQueryValidator() {
        RuleFor(static query => query.FdcId)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("FDC ID must be greater than zero.");
    }
}
