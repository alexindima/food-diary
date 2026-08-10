using FoodDiary.Application.Lessons.Commands.MarkLessonRead;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Application.Lessons.Queries.GetLessonById;
using FoodDiary.Application.Lessons.Queries.GetLessons;
using FoodDiary.Presentation.Api.Features.Lessons.Responses;
using FoodDiary.Presentation.Api.Features.Lessons.Requests;

namespace FoodDiary.Presentation.Api.Features.Lessons.Mappings;

public static class LessonHttpMappings {
    extension(Guid userId) {
        public GetLessonsQuery ToQuery(GetLessonsHttpQuery query) =>
            new(userId, query.Locale, query.Category, query.Difficulty, query.Search, query.Sort, query.Page, query.PageSize);
        public GetLessonByIdQuery ToGetByIdQuery(Guid lessonId) =>
            new(userId, lessonId);
        public MarkLessonReadCommand ToMarkReadCommand(Guid lessonId) =>
            new(userId, lessonId);
    }

    extension(LessonPageModel model) {
        public LessonPageHttpResponse ToHttpResponse() =>
            new(
                model.Items.Select(m => new LessonSummaryHttpResponse(
                    m.Id, m.Title, m.Summary, m.Category, m.Difficulty, m.EstimatedReadMinutes, m.IsRead)).ToList(),
                model.Page,
                model.PageSize,
                model.TotalCount,
                model.TotalPages,
                model.TotalLessonCount,
                model.ReadLessonCount);
    }

    extension(LessonDetailModel model) {
        public LessonDetailHttpResponse ToHttpResponse() =>
                new(model.Id, model.Title, model.Content, model.Summary, model.Category,
                    model.Difficulty, model.EstimatedReadMinutes, model.IsRead);
    }
}
