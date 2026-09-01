using FoodDiary.Results;

namespace FoodDiary.Application.Users.Common;

public static class UserAppearancePreferencesParser {
    public static Result<UserAppearancePreferences> ParseOptional(string? theme, string? uiStyle) {
        Result<string?> themeResult = UserPreferenceCodeParser.ParseOptionalTheme(
            theme,
            "Theme",
            "Invalid theme value.");
        if (themeResult.IsFailure) {
            return Result.Failure<UserAppearancePreferences>(themeResult.Error);
        }

        Result<string?> uiStyleResult = UserPreferenceCodeParser.ParseOptionalUiStyle(
            uiStyle,
            "UiStyle",
            "Invalid UI style value.");
        if (uiStyleResult.IsFailure) {
            return Result.Failure<UserAppearancePreferences>(uiStyleResult.Error);
        }

        return Result.Success(new UserAppearancePreferences(themeResult.Value, uiStyleResult.Value));
    }
}
