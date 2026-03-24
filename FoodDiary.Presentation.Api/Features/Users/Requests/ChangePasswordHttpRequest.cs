namespace FoodDiary.Presentation.Api.Features.Users.Requests;

public sealed record ChangePasswordHttpRequest(
    string CurrentPassword,
    string NewPassword);
