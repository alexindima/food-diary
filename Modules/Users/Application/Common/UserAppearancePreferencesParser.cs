using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Common;

public static class UserAppearancePreferencesParser {
    public static Result<UserAppearancePreferences> ParseOptional(string? theme, string? uiStyle, string? surfaceStyle = null) {
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

        string? normalizedSurface = null;
        if (surfaceStyle is not null) {
            if (!SurfaceStyleCode.TryParse(surfaceStyle, out SurfaceStyleCode parsedSurface)) {
                return Result.Failure<UserAppearancePreferences>(Errors.Validation.Invalid("SurfaceStyle", "Invalid surface style value."));
            }

            normalizedSurface = parsedSurface.Value;
        }

        return Result.Success(new UserAppearancePreferences(themeResult.Value, uiStyleResult.Value, normalizedSurface));
    }
}
