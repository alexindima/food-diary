using System.Globalization;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public static class UsdaErrors {
    public static Error DailyMicronutrientItemLimitExceeded(int limit) => new(
        "Usda.DailyMicronutrientItemLimitExceeded",
        $"Daily micronutrient summaries support at most {limit.ToString(CultureInfo.InvariantCulture)} product items.",
        Kind: ErrorKind.RateLimited);

    public static Error FoodNotFound(int fdcId) => new(
        "Usda.FoodNotFound",
        $"USDA food with FDC ID {fdcId.ToString(CultureInfo.InvariantCulture)} was not found.",
        Kind: ErrorKind.NotFound);
}
