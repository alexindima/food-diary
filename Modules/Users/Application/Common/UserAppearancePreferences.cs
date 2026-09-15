namespace FoodDiary.Modules.Users.Application.Common;

public sealed record UserAppearancePreferences(string? Theme, string? UiStyle, string? SurfaceStyle = null);
