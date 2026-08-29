using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Validators;

public static class HydrationValidators {
    public static Result ValidateAmount(int amountMl) {
        return amountMl switch {
            <= 0 => Result.Failure(Errors.Validation.Invalid(nameof(amountMl), "Amount must be positive")),
            > HydrationEntry.MaximumAmountMl => Result.Failure(Errors.Validation.Invalid(
                nameof(amountMl),
                $"Amount must be <= {HydrationEntry.MaximumAmountMl} ml")),
            _ => Result.Success(),
        };
    }
}
