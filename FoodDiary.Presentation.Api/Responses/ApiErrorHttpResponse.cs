namespace FoodDiary.Presentation.Api.Responses;

public sealed record ApiErrorHttpResponse(
    string Error,
    string Message);
