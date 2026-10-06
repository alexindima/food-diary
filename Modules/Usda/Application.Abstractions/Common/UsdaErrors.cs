using System.Globalization;
using FoodDiary.Results;

namespace FoodDiary.Modules.Usda.Application.Abstractions.Common;

public static class UsdaErrors {
    public static Error ProviderLookupLimitExceeded(int limit) => new(
        "Usda.ProviderLookupLimitExceeded",
        $"Daily micronutrient summaries support at most {limit.ToString(CultureInfo.InvariantCulture)} distinct provider-only foods per request.",
        Kind: ErrorKind.RateLimited);

    public static Error ProviderLookupTimedOut() => new(
        "Usda.ProviderLookupTimedOut",
        "The USDA provider lookup budget expired. Retry the daily micronutrient request.",
        Kind: ErrorKind.ExternalFailure);

    public static Error DailyMicronutrientItemLimitExceeded(int limit) => new(
        "Usda.DailyMicronutrientItemLimitExceeded",
        $"Daily micronutrient summaries support at most {limit.ToString(CultureInfo.InvariantCulture)} product items.",
        Kind: ErrorKind.RateLimited);

    public static Error FoodNotFound(int fdcId) => new(
        "Usda.FoodNotFound",
        $"USDA food with FDC ID {fdcId.ToString(CultureInfo.InvariantCulture)} was not found.",
        Kind: ErrorKind.NotFound);
}
