using FoodDiary.Application.Admin.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminContentManagementHttpResponseMappings {
    public static AdminLessonHttpResponse ToLessonHttpResponse(this AdminLessonModel model) {
        return new AdminLessonHttpResponse(
            model.Id,
            model.Title,
            model.Content,
            model.Summary,
            model.Locale,
            model.Category,
            model.Difficulty,
            model.EstimatedReadMinutes,
            model.SortOrder,
            model.CreatedOnUtc,
            model.ModifiedOnUtc);
    }

    public static AdminLessonsImportHttpResponse ToLessonsImportHttpResponse(this AdminLessonsImportModel model) {
        return new AdminLessonsImportHttpResponse(
            model.ImportedCount,
            model.Lessons.Select(static item => item.ToLessonHttpResponse()).ToList());
    }

    public static AdminEmailTemplateHttpResponse ToHttpResponse(this AdminEmailTemplateModel model) {
        return new AdminEmailTemplateHttpResponse(
            model.Id,
            model.Key,
            model.Locale,
            model.Subject,
            model.HtmlBody,
            model.TextBody,
            model.IsActive,
            model.CreatedOnUtc,
            model.UpdatedOnUtc);
    }
}
