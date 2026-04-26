using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Validation;

public static class StringCodeParser {
    public static Result<string?> ParseOptionalLanguage(string? value, string fieldName, string message) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return LanguageCode.TryParse(value, out var language)
            ? Result.Success<string?>(language.Value)
            : Result.Failure<string?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<string?> ParseOptionalGender(string? value, string fieldName, string message) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return GenderCode.TryParse(value, out var gender)
            ? Result.Success<string?>(gender.Value)
            : Result.Failure<string?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<string?> ParseOptionalTheme(string? value, string fieldName, string message) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return ThemeCode.TryParse(value, out var theme)
            ? Result.Success<string?>(theme.Value)
            : Result.Failure<string?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<string?> ParseOptionalUiStyle(string? value, string fieldName, string message) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return UiStyleCode.TryParse(value, out var uiStyle)
            ? Result.Success<string?>(uiStyle.Value)
            : Result.Failure<string?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<string> ParseRequiredLanguage(string value, string fieldName, string message) {
        return LanguageCode.TryParse(value, out var language)
            ? Result.Success(language.Value)
            : Result.Failure<string>(Errors.Validation.Invalid(fieldName, message));
    }
}
