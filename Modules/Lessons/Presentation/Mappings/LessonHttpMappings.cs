using FoodDiary.Modules.Lessons.Application.Commands.MarkLessonRead;
using FoodDiary.Modules.Lessons.Application.Models;
using FoodDiary.Modules.Lessons.Application.Queries.GetLessonById;
using FoodDiary.Modules.Lessons.Application.Queries.GetLessons;
using FoodDiary.Modules.Lessons.Presentation.Responses;
using FoodDiary.Modules.Lessons.Presentation.Requests;

namespace FoodDiary.Modules.Lessons.Presentation.Mappings;

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
                model.ReadLessonCount,
                model.AvailableCategories);
    }

    extension(LessonDetailModel model) {
        public LessonDetailHttpResponse ToHttpResponse() =>
                new(model.Id, model.Title, model.Content, model.Summary, model.Category,
                    model.Difficulty, model.EstimatedReadMinutes, model.IsRead);
    }
}
