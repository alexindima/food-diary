using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminTemplateRevisions;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminTemplateRevisionHttpMappings {
    public static GetAdminTemplateRevisionsQuery ToTemplateRevisionsQuery(string key, string locale, bool isAiPrompt) => new(key, locale, isAiPrompt);
    public static AdminTemplateRevisionHttpResponse ToRevisionHttpResponse(this AdminTemplateRevisionModel model) =>
        new(model.Id, model.Subject, model.HtmlBody, model.TextBody, model.IsActive, model.Version, model.SavedOnUtc, model.ArchivedOnUtc);
}
