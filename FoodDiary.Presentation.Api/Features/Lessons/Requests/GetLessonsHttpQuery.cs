namespace FoodDiary.Presentation.Api.Features.Lessons.Requests;

public sealed record GetLessonsHttpQuery(
    string Locale = "en",
    string? Category = null,
    string? Difficulty = null,
    string? Search = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20);
