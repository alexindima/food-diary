using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Modules.Hydration.Application.Validators;

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
