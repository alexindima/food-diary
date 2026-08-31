using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Admin.Common;

public static class AdminLocaleParser {
    public static Result<string> ParseRequiredLanguage(string value, string fieldName, string message) =>
        LanguageCode.TryParse(value, out LanguageCode language)
            ? Result.Success(language.Value)
            : Result.Failure<string>(Errors.Validation.Invalid(fieldName, message));
}
