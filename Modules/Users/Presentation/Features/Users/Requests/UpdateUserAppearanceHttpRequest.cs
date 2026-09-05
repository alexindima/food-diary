namespace FoodDiary.Presentation.Api.Features.Users.Requests;

public sealed record UpdateUserAppearanceHttpRequest(
    string? Theme,
    string? UiStyle);
