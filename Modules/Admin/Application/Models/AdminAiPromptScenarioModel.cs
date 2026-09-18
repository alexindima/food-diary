namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAiPromptScenarioModel(string Key, string Locale, string PromptText,
    string Source, string SourceLocale, string InheritedPromptText, string InheritedSource,
    AdminAiPromptModel? Template, IReadOnlyList<string> Variables, string ResponseFormatJson);
