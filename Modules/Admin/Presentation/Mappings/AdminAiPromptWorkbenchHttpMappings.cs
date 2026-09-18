using FoodDiary.Modules.Admin.Application.Commands.TestAdminAiPrompt;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPromptScenarios;
using FoodDiary.Modules.Admin.Application.Queries.PreviewAdminAiPrompt;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Mappings;

public static class AdminAiPromptWorkbenchHttpMappings {
    public static GetAdminAiPromptScenariosQuery ToScenariosQuery() => new();

    public static PreviewAdminAiPromptQuery ToPreviewQuery(this AdminAiPromptDraftHttpRequest request) => new(ToDraft(request));

    public static TestAdminAiPromptCommand ToTestCommand(this AdminAiPromptDraftHttpRequest request, Guid userId, string requestId) =>
        new(userId, requestId, ToDraft(request));

    public static AdminAiPromptScenarioHttpResponse ToScenarioHttpResponse(this AdminAiPromptScenarioModel item) =>
        new(item.Key, item.Locale, item.PromptText, item.Source, item.SourceLocale, item.InheritedPromptText,
            item.InheritedSource, item.Template?.ToAiPromptHttpResponse(), item.Variables, item.ResponseFormatJson);

    private static AdminAiPromptDraft ToDraft(AdminAiPromptDraftHttpRequest request) =>
        new(request.Key, request.Locale, request.PromptText, request.Text, request.ImageAssetId, request.FoodName, request.Amount, request.Unit);
}
