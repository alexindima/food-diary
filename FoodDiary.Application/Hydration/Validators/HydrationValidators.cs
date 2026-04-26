using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Hydration.Validators;

public static class HydrationValidators {
    private const int MaxSingleEntryMl = 10000;

    public static Result ValidateAmount(int amountMl) {
        return amountMl switch {
            <= 0 => Result.Failure(Errors.Validation.Invalid(nameof(amountMl), "Amount must be positive")),
            > MaxSingleEntryMl => Result.Failure(Errors.Validation.Invalid(nameof(amountMl), $"Amount must be <= {MaxSingleEntryMl} ml")),
            _ => Result.Success()
        };
    }
}
