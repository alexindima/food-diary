namespace FoodDiary.Application.Users.Common;

public sealed record UserAppearancePreferences(string? Theme, string? UiStyle, string? SurfaceStyle = null);
