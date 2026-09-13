using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminTemplateRevisions;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

public static class AdminTemplateRevisionHttpMappings {
    public static GetAdminTemplateRevisionsQuery ToTemplateRevisionsQuery(string key, string locale, bool isAiPrompt) => new(key, locale, isAiPrompt);
    public static AdminTemplateRevisionHttpResponse ToRevisionHttpResponse(this AdminTemplateRevisionModel model) =>
        new(model.Id, model.Subject, model.HtmlBody, model.TextBody, model.IsActive, model.Version, model.SavedOnUtc, model.ArchivedOnUtc);
}
