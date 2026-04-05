using FoodDiary.Application.Lessons.Commands.MarkLessonRead;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Application.Lessons.Queries.GetLessonById;
using FoodDiary.Application.Lessons.Queries.GetLessons;
using FoodDiary.Presentation.Api.Features.Lessons.Responses;

namespace FoodDiary.Presentation.Api.Features.Lessons.Mappings;

public static class LessonHttpMappings {
    public static GetLessonsQuery ToQuery(this Guid userId, string locale, string? category) =>
        new(userId, locale, category);

    public static GetLessonByIdQuery ToGetByIdQuery(this Guid userId, Guid lessonId) =>
        new(userId, lessonId);

    public static MarkLessonReadCommand ToMarkReadCommand(this Guid userId, Guid lessonId) =>
        new(userId, lessonId);

    public static IReadOnlyList<LessonSummaryHttpResponse> ToHttpResponse(
        this IReadOnlyList<LessonSummaryModel> models) =>
        models.Select(m => new LessonSummaryHttpResponse(
            m.Id, m.Title, m.Summary, m.Category, m.Difficulty, m.EstimatedReadMinutes, m.IsRead)).ToList();

    public static LessonDetailHttpResponse ToHttpResponse(this LessonDetailModel model) =>
        new(model.Id, model.Title, model.Content, model.Summary, model.Category,
            model.Difficulty, model.EstimatedReadMinutes, model.IsRead);
}
