namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminAiPromptScenarioHttpResponse(string Key, string Locale, string PromptText,
    string Source, string SourceLocale, string InheritedPromptText, string InheritedSource,
    AdminAiPromptHttpResponse? Template, IReadOnlyList<string> Variables, string ResponseFormatJson);
