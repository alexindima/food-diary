namespace FoodDiary.Modules.Users.Presentation.Requests;

public sealed record UpdateUserAppearanceHttpRequest(
    string? Theme,
    string? UiStyle,
    string? SurfaceStyle = null);
