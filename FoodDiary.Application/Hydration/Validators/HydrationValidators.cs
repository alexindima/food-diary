using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Hydration.Validators;

public static class HydrationValidators
{
    private const int MaxSingleEntryMl = 10000;

    public static Result ValidateAmount(int amountMl)
    {
        if (amountMl <= 0)
        {
            return Result.Failure(Errors.Validation.Invalid(nameof(amountMl), "Amount must be positive"));
        }

        if (amountMl > MaxSingleEntryMl)
        {
            return Result.Failure(Errors.Validation.Invalid(nameof(amountMl), $"Amount must be <= {MaxSingleEntryMl} ml"));
        }

        return Result.Success();
    }
}
