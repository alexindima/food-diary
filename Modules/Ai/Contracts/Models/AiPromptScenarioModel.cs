namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record AiPromptScenarioModel(
    string Key, string Locale, string PromptText, string Source, string SourceLocale,
    string InheritedPromptText, string InheritedSource, AiPromptTemplateReadModel? Template,
    IReadOnlyList<string> Variables, string ResponseFormatJson);
